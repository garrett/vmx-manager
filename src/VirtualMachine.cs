using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Unix;

namespace VmxManager {

    public interface IVirtualDevice {
        VirtualDeviceType DeviceType { get; }
        string DisplayName { get; }
    }

    public delegate void VirtualMachineHandler (object o, VirtualMachineArgs args);

    public class VirtualMachineArgs : EventArgs {

        private VirtualMachine machine;

        public VirtualMachine Machine {
            get { return machine; }
        }

        public VirtualMachineArgs (VirtualMachine machine) {
            this.machine = machine;
        }
    }

    public class VirtualMachine {

        private string file;
        private Dictionary<string, string> dict = new Dictionary<string, string> ();
        private bool running;
        private FileSystemWatcher watcher;
        
        private List<VirtualHardDisk> hardDisks = new List<VirtualHardDisk> ();
        private List<VirtualCdDrive> cds = new List<VirtualCdDrive> ();
        private List<VirtualEthernet> ethernetDevices = new List<VirtualEthernet> ();

        public event VirtualHardDiskHandler HardDiskAdded;
        public event VirtualHardDiskHandler HardDiskRemoved;
        public event VirtualCdDriveHandler CdDriveAdded;
        public event VirtualCdDriveHandler CdDriveRemoved;
        public event VirtualEthernetHandler EthernetDeviceAdded;
        public event VirtualEthernetHandler EthernetDeviceRemoved;

        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler NameChanged;
        public event EventHandler FileNameChanged;
        
        public string FileName {
            get { return file; }
            set {
                file = Path.GetFullPath (value);

                EventHandler handler = FileNameChanged;
                if (handler != null) {
                    handler (this, new EventArgs ());
                }
                
                CreateWatcher ();
            }
        }

        public string Name {
            get {
                if (dict.ContainsKey ("displayName")) {
                    return dict["displayName"];
                } else {
                    return Catalog.GetString ("Unknown");
                }
            } set {
                dict["displayName"] = value;

                EventHandler handler = NameChanged;
                if (handler != null) {
                    handler (this, new EventArgs ());
                }
            }
        }

        public int MemorySize {
            get {
                string val = dict["memsize"];
                if (val != null) {
                    return Int32.Parse (val);
                } else {
                    return 0;
                }
            } set {
                dict["memsize"] = value.ToString ();
            }
        }

        public GuestOperatingSystem OperatingSystem {
            get {
                return GuestOperatingSystem.Lookup (dict["guestOS"]);
            } set {
                dict["guestOS"] = value.Name;
            }
        }

        public bool SoundEnabled {
            get { return dict.ContainsKey ("sound.present") && dict["sound.present"] == "TRUE"; }
            set { dict["sound.present"] = value ? "TRUE" : "FALSE"; }
        }

        public bool UsbEnabled {
            get { return dict.ContainsKey ("usb.present") && dict["usb.present"] == "TRUE"; }
            set { dict["usb.present"] = value ? "TRUE" : "FALSE"; }
        }

        public ReadOnlyCollection<VirtualHardDisk> HardDisks {
            get { return new ReadOnlyCollection<VirtualHardDisk> (hardDisks); }
        }

        public ReadOnlyCollection<VirtualCdDrive> CdDrives {
            get { return new ReadOnlyCollection<VirtualCdDrive> (cds); }
        }

        public ReadOnlyCollection<VirtualEthernet> EthernetDevices {
            get { return new ReadOnlyCollection<VirtualEthernet> (ethernetDevices); }
        }

        public bool IsRunning {
            get { return running; }
        }

        public string this[string key] {
            get {
                if (dict.ContainsKey (key)) {
                    return dict[key];
                } else {
                    return null;
                }
            } set {
                dict[key] = value;
            }
        }

        private VirtualMachine () {
        }

        public VirtualMachine (string file) {
            this.FileName = file;
            LoadFromFile ();
            CheckRunning ();
        }

        public static VirtualMachine Create (string file, string name) {
            VirtualMachine machine = new VirtualMachine ();
            machine.LoadDefaults ();
            machine.FileName = file;
            machine.Name = name;
            machine["nvram"] = name + ".nvram";
            machine["checkpoint.vmState"] = name + ".vmss";

            return machine;
        }

        private string GetDiskBaseKey (VirtualDisk disk) {
            return GetDiskBaseKey (disk.BusType, disk.BusNumber, disk.DeviceNumber);
        }

        private string GetDiskBaseKey (DiskBusType busType, int busnum, int devnum) {
            if (busType == DiskBusType.Ide) {
                return String.Format ("ide{0}:{1}.", busnum, devnum);
            } else {
                return String.Format ("scsi{0}:{1}.", busnum, devnum);
            }
        }

        public void AddHardDisk (VirtualHardDisk disk) {
            hardDisks.Add (disk);

            VirtualHardDiskHandler handler = HardDiskAdded;
            if (handler != null) {
                handler (this, new VirtualHardDiskArgs (disk));
            }
        }

        public void RemoveHardDisk (VirtualHardDisk disk) {
            if (hardDisks.Contains (disk)) {
                hardDisks.Remove (disk);

                RemoveDiskValues (disk);

                VirtualHardDiskHandler handler = HardDiskRemoved;
                if (handler != null) {
                    handler (this, new VirtualHardDiskArgs (disk));
                }
            }
        }

        private void RemoveDiskValues (VirtualDisk disk) {
            string basekey = GetDiskBaseKey (disk);
            foreach (string key in new List<string> (dict.Keys)) {
                if (key.StartsWith (basekey)) {
                    dict.Remove (key);
                }
            }
        }

        public void AddCdDrive (VirtualCdDrive drive) {
            cds.Add (drive);

            VirtualCdDriveHandler handler = CdDriveAdded;
            if (handler != null) {
                handler (this, new VirtualCdDriveArgs (drive));
            }
        }

        public void RemoveCdDrive (VirtualCdDrive drive) {
            if (cds.Contains (drive)) {
                cds.Remove (drive);

                RemoveDiskValues (drive);

                VirtualCdDriveHandler handler = CdDriveRemoved;
                if (handler != null) {
                    handler (this, new VirtualCdDriveArgs (drive));
                }
            }
        }

        public void AddEthernetDevice (VirtualEthernet eth) {
            ethernetDevices.Add (eth);

            VirtualEthernetHandler handler = EthernetDeviceAdded;
            if (handler != null) {
                handler (this, new VirtualEthernetArgs (eth));
            }
        }

        public void RemoveEthernetDevice (VirtualEthernet eth) {
            int index = ethernetDevices.IndexOf (eth);
            
            if (index >= 0) {
                string basekey = String.Format ("ethernet{0}", index);
                                
                ethernetDevices.Remove (eth);

                foreach (string key in new List<string> (dict.Keys)) {
                    if (key.StartsWith (basekey)) {
                        dict.Remove (key);
                    }
                }
                
                VirtualEthernetHandler handler = EthernetDeviceRemoved;
                if (handler != null) {
                    handler (this, new VirtualEthernetArgs (eth));
                }
            }
        }

        private void LoadDefaults () {
            LoadFromStream (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("defaults.vmx"));
        }

        private void LoadFromFile () {
            LoadFromStream (File.OpenRead (file));
        }

        private void LoadFromStream (Stream stream) {
            dict.Clear ();
            hardDisks.Clear ();
            cds.Clear ();
            
            using (StreamReader reader = new StreamReader (stream)) {
                string line;

                while ((line = reader.ReadLine ()) != null) {
                    string key, value;
                    if (Utility.ReadConfigLine (line, out key, out value)) {
                        dict[key] = value;
                    }
                }
            }

            LoadDisks (DiskBusType.Ide);
            LoadDisks (DiskBusType.Scsi);
            LoadEthernetDevices ();
        }

        private void LoadDisks (DiskBusType busType) {
            for (ushort i = 0; i < 2; i++) {
                for (ushort j = 0; j < 6; j++) {
                    string basekey = String.Format ("{0}{1}:{2}.", busType == DiskBusType.Ide ? "ide" : "scsi",
                                                    i, j);
                    
                    if (this[basekey + "present"] != null && this[basekey + "present"] == "TRUE") {
                        string diskFile = this[basekey + "fileName"];
                        if (diskFile != null && diskFile != "auto detect" && !Path.IsPathRooted (diskFile)) {
                            diskFile = Path.Combine (Path.GetDirectoryName (file), diskFile);
                        }

                        string devtype = this[basekey + "deviceType"];
                        if (devtype == null || devtype == "disk") {
                            VirtualHardDisk disk = new VirtualHardDisk (diskFile,
                                                                        i, j, busType);
                            hardDisks.Add (disk);
                        } else {
                            VirtualCdDrive drive = new VirtualCdDrive (diskFile, i, j, busType,
                                                                       Utility.ParseCdDeviceType (devtype));
                            cds.Add (drive);
                        }
                    }
                }
            }
        }

        private void LoadEthernetDevices () {
            for (int i = 0; i < 10; i++) {
                string basekey = String.Format ("ethernet{0}.", i);

                if (this[basekey + "present"] != null) {
                    string connType = this[basekey + "connectionType"];
                    string address = this[basekey + "address"];
                    string dev = this[basekey + "virtualDev"];

                    VirtualEthernet eth = new VirtualEthernet (Utility.ParseNetworkType (connType),
                                                               address,
                                                               OperatingSystem.SuggestedEthernetDeviceType);
                    ethernetDevices.Add (eth);
                }
            }
        }

        private void CheckConflictingDisk (VirtualDisk disk, List<int> list) {
            int hash = String.Format ("{0}{1}{2}", disk.BusNumber, disk.DeviceNumber, disk.BusType).GetHashCode ();
            if (list.Contains (hash)) {
                throw new ApplicationException (Catalog.GetString ("There are two conflicting disks.  Please change the position of one of the disks and try again."));
            }

            list.Add (hash);
        }

        private void CheckConflictingDisks () {
            // this is clumsy
            List<int> list = new List<int> ();

            foreach (VirtualDisk disk in hardDisks) {
                CheckConflictingDisk (disk, list);
            }

            foreach (VirtualDisk disk in cds) {
                CheckConflictingDisk (disk, list);
            }
        }

        public void Save () {
            CheckConflictingDisks ();
            
            string vmdir = Path.GetDirectoryName (file);
            if (!Directory.Exists (vmdir)) {
                Directory.CreateDirectory (vmdir);
            }

            if (!File.Exists (file)) {
                dict["nvram"] = Name + ".nvram";
                dict["checkpoint.vmState"] = Name + ".vmss";
            }

            // remove all the existing ide/scsi related values
            foreach (string key in new List<string> (dict.Keys)) {
                if (key.StartsWith ("ide") || key.StartsWith ("scsi")) {
                    dict.Remove (key);
                }
            }

            // save hard disks
            foreach (VirtualDisk disk in hardDisks) {
                SaveDisk (disk);
            }

            // save cd drives
            foreach (VirtualDisk disk in cds) {
                SaveDisk (disk);
            }

            // save ethernet devices
            for (int i = 0; i < ethernetDevices.Count; i++) {
                VirtualEthernet ethernet = ethernetDevices[i];

                string basekey = String.Format ("ethernet{0}.", i);

                this[basekey + "present"] = "TRUE";
                this[basekey + "connectionType"] = Utility.NetworkTypeToString (ethernet.NetworkType);

                if (ethernet.Address != null) {
                    this[basekey + "address"] = ethernet.Address;
                } else {
                    dict.Remove (basekey + "address");
                }
                
                this[basekey + "virtualDev"] = Utility.EthernetDeviceTypeToString (ethernet.EthernetType);
            }

            // write the file out
            using (StreamWriter writer = new StreamWriter (File.Open (file, FileMode.Create))) {
                List<string> keys = new List<string> (dict.Keys);
                keys.Sort ();
                
                foreach (string key in keys) {
                    writer.WriteLine ("{0} = \"{1}\"", key, dict[key]);
                }
            }

            CreateWatcher ();
        }

        private void SaveDisk (VirtualDisk disk) {
            string diskFile = disk.FileName;
            if (diskFile != null && diskFile != "auto detect" && Path.IsPathRooted (diskFile) &&
                Path.GetDirectoryName (diskFile) == Path.GetDirectoryName (file)) {
                diskFile = Path.GetFileName (diskFile);
            }
            
            string basekey = GetDiskBaseKey (disk);
            dict[basekey + "present"] = "TRUE";
            dict[basekey + "fileName"] = diskFile;
            
            string disktype;
            if (disk is VirtualHardDisk) {
                disktype = "disk";
            } else {
                CdDeviceType cdtype = (disk as VirtualCdDrive).CdDeviceType;
                if (OperatingSystem.IsLegacy && cdtype == CdDeviceType.Raw) {
                    cdtype = CdDeviceType.Legacy;
                }
                
                disktype = Utility.CdDeviceTypeToString (cdtype);
            }
            
            dict[basekey + "deviceType"] = disktype;
        }

        public void Dump () {
            /*
            foreach (string key in dict.Keys) {
                Console.WriteLine ("{0} = {1}", key, dict[key]);
            }
            */

            Console.WriteLine ("File name: " + FileName);
            Console.WriteLine ("Name: " + Name);
            Console.WriteLine ("Memory: " + MemorySize);
            Console.WriteLine ("Operating System: " + OperatingSystem.DisplayName);
            Console.WriteLine ();

            foreach (VirtualHardDisk hd in hardDisks) {
                Console.WriteLine ("Disk {0} {1}:{2} ({3}) {4}", hd.BusType, hd.BusNumber, hd.DeviceNumber,
                                   hd.DeviceType, hd.FileName);
            }
        }

        public void Start () {
            if (IsRunning) {
                return;
            }

            Process.Start (String.Format ("vmplayer \"{0}\"", file));
        }

        public void Delete () {
            foreach (VirtualHardDisk disk in hardDisks) {
                disk.Delete ();
            }

            string dir = Path.GetDirectoryName (file);

            File.Delete (Path.Combine (dir, "vmware.log"));
            if (dict.ContainsKey ("nvram")) {
                File.Delete (Path.Combine (dir, dict["nvram"]));
            }

            if (dict.ContainsKey ("checkpoint.vmState")) {
                File.Delete (Path.Combine (dir, dict["checkpoint.vmState"]));
            }

            File.Delete (Path.Combine (dir, Name + ".vmsd"));
            File.Delete (Path.Combine (dir, Name + ".vmem"));
            File.Delete (file);

            if (Directory.GetFiles (dir).Length == 0 &&
                Directory.GetDirectories (dir).Length == 0) {
                // it's empty, nuke it
                Directory.Delete (dir);
            }
        }

        public override bool Equals (object o) {
            VirtualMachine machine = (o as VirtualMachine);
            return this.FileName == machine.FileName;
        }

        public override int GetHashCode () {
            return this.FileName.GetHashCode ();
        }

        private void CreateWatcher () {
            if (watcher != null) {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose ();
            }

            string dir = Path.GetDirectoryName (file);

            if (Directory.Exists (dir)) {
                watcher = new FileSystemWatcher (Path.GetDirectoryName (file));
                watcher.Created += OnFileCreated;
                watcher.Deleted += OnFileDeleted;
                watcher.EnableRaisingEvents = true;
            }
        }

        private void OnFileCreated (object o, FileSystemEventArgs args) {
            if (args.FullPath == file + ".WRITELOCK") {
                CheckRunning ();
            }
        }

        private void OnFileDeleted (object o, FileSystemEventArgs args) {
            if (args.FullPath == file + ".WRITELOCK") {
                CheckRunning ();
            }
        }

        private void CheckRunning () {
            bool val = File.Exists (file + ".WRITELOCK");

            EventHandler handler = null;
            if (val && !running) {
                handler = Started;
            } else if (!val && running) {
                handler = Stopped;
            }

            running = val;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }
    }
}
