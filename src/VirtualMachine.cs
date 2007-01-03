using System;
using System.Text;
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

    public enum VirtualMachineStatus {
        Running,
        Suspended,
        Off
    }

    public class VirtualMachine {

        private const int PreviewIconSize = 64;

        private string file;
        private Dictionary<string, string> dict = new Dictionary<string, string> ();
        private VirtualMachineStatus status = VirtualMachineStatus.Off;
        private FileSystemWatcher watcher;
        
        private List<VirtualHardDisk> hardDisks = new List<VirtualHardDisk> ();
        private List<VirtualCdDrive> cds = new List<VirtualCdDrive> ();
        private List<VirtualEthernet> ethernetDevices = new List<VirtualEthernet> ();

        private Gdk.Pixbuf icon;

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

        private string LockFileName {
            get { return file + ".WRITELOCK"; }
        }

        
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

        private string CheckPointFileName {
            get {
                string vmss = this["checkpoint.vmState"];
                if (vmss == null)
                    return null;
                
                if (!Path.IsPathRooted (vmss)) {
                    vmss = Path.Combine (Path.GetDirectoryName (file), vmss);
                }
                
                return vmss;
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

        public Gdk.Pixbuf PreviewIcon {
            get {
                if (icon == null) {
                    LoadPreviewIcon ();
                }

                return icon;
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

        public VirtualMachineStatus Status {
            get { return status; }
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
            UpdateStatus ();
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

                        ScsiDeviceType scsiType = OperatingSystem.SuggestedScsiDeviceType;
                        if (this[basekey + "virtualDev"] != null) {
                            scsiType = Utility.ParseScsiDeviceType (this[basekey + "virtualDev"]);
                        }

                        string devtype = this[basekey + "deviceType"];
                        if (devtype == null || devtype == "disk") {
                            VirtualHardDisk disk = new VirtualHardDisk (diskFile,
                                                                        i, j, busType);
                            disk.ScsiDeviceType = scsiType;
                            hardDisks.Add (disk);
                        } else {
                            VirtualCdDrive drive = new VirtualCdDrive (diskFile, i, j, busType,
                                                                       Utility.ParseCdDeviceType (devtype));
                            drive.ScsiDeviceType = scsiType;
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
                    // string dev = this[basekey + "virtualDev"];

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
            Save (null);
        }

        public void Save (ProgressHandler handler) {
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
            for (int i = 0; i < hardDisks.Count; i++) {
                VirtualDisk disk = hardDisks[i];
                if (handler != null) {
                    SaveDisk (disk, delegate (object o, ProgressArgs args) {
                        double progress = (double) i / (double) hardDisks.Count;
                        progress += args.Progress * ((double) 1 / (double) hardDisks.Count);
                        handler (this, new ProgressArgs (progress));
                    });
                } else {
                    SaveDisk (disk, null);
                }
            }

            // save cd drives
            foreach (VirtualDisk disk in cds) {
                SaveDisk (disk, handler);
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

        private string GetNewDiskPath () {
            string dir = Path.GetDirectoryName (file);
            
            for (int i = 0;; i++) {
                string path = System.IO.Path.Combine (dir, String.Format ("disk{0}.vmdk", i));
                if (!File.Exists (path)) {
                    return path;
                }
            }
        }

        private void SaveDisk (VirtualDisk disk, ProgressHandler handler) {
            if (disk.FileName == null && disk is VirtualHardDisk) {
                disk.FileName = GetNewDiskPath ();
            }

            if (!File.Exists (disk.FileName) && disk is VirtualHardDisk) {
                VirtualHardDisk hd = disk as VirtualHardDisk;
                
                if (hd.BusType == DiskBusType.Scsi) {
                    hd.ScsiDeviceType = OperatingSystem.SuggestedScsiDeviceType;
                }

                hd.Create (handler);
            }

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

        public void Delete () {
            foreach (VirtualHardDisk disk in hardDisks) {
                disk.Delete ();
            }

            string dir = Path.GetDirectoryName (file);

            File.Delete (Path.Combine (dir, "vmware.log"));
            if (dict.ContainsKey ("nvram")) {
                File.Delete (Path.Combine (dir, dict["nvram"]));
            }

            string vmss = CheckPointFileName;
            if (vmss != null) {
                File.Delete (vmss);
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
            if (args.FullPath == LockFileName || args.FullPath == CheckPointFileName) {
                UpdateStatus ();
            }
        }

        private void OnFileDeleted (object o, FileSystemEventArgs args) {
            if (args.FullPath == LockFileName || args.FullPath == CheckPointFileName) {
                UpdateStatus ();
            }
        }

        private void UpdateStatus () {
            bool lockExists = File.Exists (LockFileName);
            bool checkPointExists = File.Exists (CheckPointFileName);

            VirtualMachineStatus newstatus;
            if (lockExists) {
                newstatus = VirtualMachineStatus.Running;
            } else if (checkPointExists) {
                newstatus = VirtualMachineStatus.Suspended;
            } else {
                newstatus = VirtualMachineStatus.Off;
                icon = null;
            }

            if (newstatus == status)
                return;

            status = newstatus;
            
            EventHandler handler = null;
            if (status == VirtualMachineStatus.Running) {
                handler = Started;
            } else {
                LoadPreviewIcon ();
                handler = Stopped;
            }

            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        private void LoadPreviewIcon () {
            string vmss = CheckPointFileName;
            if (vmss == null || !File.Exists (vmss))
                return;

            int header = BitConverter.ToInt32 (new byte[] { 0x89, (byte) 'P', (byte) 'N', (byte) 'G' }, 0);
            int chunkSize = 1024;

            using (FileStream stream = File.OpenRead (vmss)) {
                byte[] chunk = new byte[chunkSize];

                for (;;) {
                    int len = stream.Read (chunk, 0, chunkSize);
                    if (len == 0)
                        break;

                    for (int i = 0; i < len - 4; i++) {
                        int val = BitConverter.ToInt32 (chunk, i);
                        if (val == header) {
                            stream.Seek (-(len - i), SeekOrigin.Current);

                            try {
                                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (stream);

                                double scale = Math.Min  (PreviewIconSize / (double) pixbuf.Width,
                                                          PreviewIconSize / (double) pixbuf.Height);
                                int scaleWidth = (int) (scale * pixbuf.Width);
                                int scaleHeight = (int) (scale * pixbuf.Height);

                                icon = pixbuf.ScaleSimple (scaleWidth, scaleHeight, Gdk.InterpType.Bilinear);
                            } catch {
                            }
                            
                            return;
                        }
                    }
                }
            }
        }
    }
}
