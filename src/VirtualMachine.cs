using System;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VmxManager {

    public enum ScsiDeviceType {
        Buslogic,
        LSILogic,
    }
    
    public enum EthernetDeviceType {
        PCnet,
        VMXnet,
        E1000
    }
    
    public enum HardDiskType {
        SingleSparse,
        SplitSparse,
        SingleFlat,
        SplitFlat
    }

    public enum DiskDeviceType {
        HardDisk,
        CDRaw,
        CDIso,
        CDLegacy
    }

    public enum DiskBusType {
        Ide,
        Scsi,
    }

    public class VirtualDisk : IVirtualDevice {
        private string file;
        private DiskDeviceType diskType;
        private ushort devnum;
        private ushort busnum;
        private DiskBusType busType;

        public VirtualDeviceType DeviceType {
            get {
                if (diskType == DiskDeviceType.HardDisk) {
                    return VirtualDeviceType.HardDisk;
                } else {
                    return VirtualDeviceType.CdRom;
                }
            }
        }

        public string DisplayName {
            get {
                switch (diskType) {
                case DiskDeviceType.HardDisk:
                    return "Hard Disk";
                case DiskDeviceType.CDRaw:
                    return "CD-ROM (Physical)";
                case DiskDeviceType.CDIso:
                    return String.Format ("CD-ROM ({0})", Path.GetFileName (file));
                case DiskDeviceType.CDLegacy:
                    return "CD-ROM (Physical, Legacy mode)";
                default:
                    return String.Empty;
                }
            }
        }

        public string FileName {
            get { return file; }
            set {
                file = value;
                if (file != null && file != "auto detect") {
                    file = Path.GetFullPath (file);
                }
            }
        }

        public DiskDeviceType DiskType {
            get { return diskType; }
            set { diskType = value; }
        }

        public ushort BusNumber {
            get { return busnum; }
            set { busnum = value; }
        }

        public ushort DeviceNumber {
            get { return devnum; }
            set { devnum = value; }
        }

        public DiskBusType BusType {
            get { return busType; }
            set { busType = value; }
        }

        public VirtualDisk (string file, DiskDeviceType diskType, ushort busnum, ushort devnum, DiskBusType busType) {
            this.FileName = file;
            this.diskType = diskType;
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;
        }

        public static VirtualDisk CreateHardDisk (string file, long sizeInMb, HardDiskType type) {
            Process proc = Process.Start (String.Format ("qemu-img create -f vmdk \"{0}\" \"{1}\"",
                                                         file, sizeInMb * 1024));
            proc.WaitForExit ();
            if (proc.ExitCode != 0) {
                throw new ApplicationException ("Failed to create virtual disk");
            }
            
            return new VirtualDisk (file, DiskDeviceType.HardDisk, 0, 0, DiskBusType.Ide);
        }
    }

    public enum VirtualDeviceType {
        HardDisk,
        CdRom,
        Ethernet,
        Floppy
    }

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
        private List<VirtualDisk> disks = new List<VirtualDisk> ();

        public string FileName {
            get { return file; }
            set {
                file = Path.GetFullPath (value);
            }
        }

        public string Name {
            get { return dict["displayName"]; }
            set { dict["displayName"] = value; }
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

        public ReadOnlyCollection<VirtualDisk> Disks {
            get { return new ReadOnlyCollection<VirtualDisk> (disks); }
        }

        public bool IsRunning {
            get { return File.Exists (file + ".WRITELOCK"); }
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

        public void AddDisk (VirtualDisk disk) {
            foreach (VirtualDisk existingDisk in disks) {
                if (existingDisk.BusType == disk.BusType && existingDisk.BusNumber == disk.BusNumber &&
                    existingDisk.DeviceNumber == disk.DeviceNumber) {
                    throw new ApplicationException ("There is already a disk at this position");
                }
            }
            
            disks.Add (disk);
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

        public void RemoveDisk (VirtualDisk disk) {
            if (disks.Contains (disk)) {
                disks.Remove (disk);

                string basekey = GetDiskBaseKey (disk);
                foreach (string key in new List<string> (dict.Keys)) {
                    if (key.StartsWith (basekey)) {
                        dict.Remove (key);
                    }
                }
            }
        }

        private void LoadDefaults () {
            LoadFromStream (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("defaults.vmx"));
        }

        private void LoadFromFile () {
            if (!File.Exists (file))
                return;

            LoadFromStream (File.OpenRead (file));
        }

        private void LoadFromStream (Stream stream) {
            dict.Clear ();
            disks.Clear ();
            
            using (StreamReader reader = new StreamReader (stream)) {
                string line;

                while ((line = reader.ReadLine ()) != null) {
                    string[] splitLine = line.Split (new char[] { '=' }, 2);
                    
                    if (splitLine.Length != 2)
                        continue;

                    string key = splitLine[0].Trim ();

                    string value = splitLine[1].Trim ();
                    value = value.Substring (1, value.Length - 2);

                    dict[key] = value;
                }
            }

            LoadDisks (DiskBusType.Ide);
            LoadDisks (DiskBusType.Scsi);
        }

        private void LoadDisks (DiskBusType busType) {
            for (ushort i = 0; i < 2; i++) {
                for (ushort j = 0; j < 6; j++) {
                    string basekey = String.Format ("{0}{1}:{2}.", busType == DiskBusType.Ide ? "ide" : "scsi",
                                                    i, j);
                    
                    if (this[basekey + "present"] != null) {
                        string diskFile = this[basekey + "fileName"];
                        if (diskFile != null && diskFile != "auto detect" && !Path.IsPathRooted (diskFile)) {
                            diskFile = Path.GetFullPath (diskFile);
                        }
                        
                        VirtualDisk disk = new VirtualDisk (diskFile,
                                                            ParseDiskType (this[basekey + "deviceType"]),
                                                            i, j, busType);
                        disks.Add (disk);
                    }
                }
            }
        }

        private DiskDeviceType ParseDiskType (string val) {
            switch (val) {
            case null:
            case "disk":
                return DiskDeviceType.HardDisk;
            case "cdrom-raw":
                return DiskDeviceType.CDRaw;
            case "cdrom-image":
                return DiskDeviceType.CDIso;
            case "atapi-cdrom":
                return DiskDeviceType.CDLegacy;
            default:
                Console.Error.WriteLine ("WARNING: Unknown disk type '{0}'", val);
                return DiskDeviceType.HardDisk;
            }
        }

        private string DiskTypeToString (DiskDeviceType dtype) {
            switch (dtype) {
            case DiskDeviceType.HardDisk:
                return "disk";
            case DiskDeviceType.CDRaw:
                return "cdrom-raw";
            case DiskDeviceType.CDIso:
                return "cdrom-image";
            case DiskDeviceType.CDLegacy:
                return "atapi-cdrom";
            default:
                Console.WriteLine ("WARNING: Unknown disk type '{0}'", dtype);
                return "disk";
            }
        }

        public void Save () {
            foreach (VirtualDisk disk in disks) {
                string diskFile = disk.FileName;
                if (diskFile != null && diskFile != "auto detect" && Path.IsPathRooted (diskFile) &&
                    Path.GetDirectoryName (diskFile) == Path.GetDirectoryName (file)) {
                    diskFile = Path.GetFileName (diskFile);
                }
                
                string basekey = GetDiskBaseKey (disk);
                dict[basekey + "present"] = "TRUE";
                dict[basekey + "fileName"] = diskFile;
                dict[basekey + "deviceType"] = DiskTypeToString (disk.DiskType);
            }
            
            using (StreamWriter writer = new StreamWriter (File.Open (file, FileMode.Create))) {
                List<string> keys = new List<string> (dict.Keys);
                keys.Sort ();
                
                foreach (string key in keys) {
                    writer.WriteLine ("{0} = \"{1}\"", key, dict[key]);
                }
            }
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

            foreach (VirtualDisk disk in disks) {
                Console.WriteLine ("Disk {0} {1}:{2} ({3}) {4}", disk.BusType, disk.BusNumber, disk.DeviceNumber,
                                   disk.DeviceType, disk.FileName);
            }
        }

        public void Start () {
            if (IsRunning) {
                return;
            }

            Process.Start (String.Format ("vmplayer \"{0}\"", file));
        }

        public override bool Equals (object o) {
            VirtualMachine machine = (o as VirtualMachine);
            return this.FileName == machine.FileName;
        }

        public override int GetHashCode () {
            return this.FileName.GetHashCode ();
        }
    }
}
