using System;
using System.IO;
using System.Diagnostics;

namespace VmxManager {

    public delegate void VirtualHardDiskHandler (object o, VirtualHardDiskArgs args);

    public class VirtualHardDiskArgs : EventArgs {

        private VirtualHardDisk disk;

        public VirtualHardDisk Disk {
            get { return disk; }
        }

        public VirtualHardDiskArgs (VirtualHardDisk disk) {
            this.disk = disk;
        }
    }
    
    public class VirtualHardDisk : VirtualDisk {

        private string adapter;
        private int sectors;
        private int heads;
        private int cylinders;

        public long Capacity {
            get {
                return (long) sectors * (long) heads * (long) cylinders * (long) 512;
            }
        }

        public override VirtualDeviceType DeviceType {
            get { return VirtualDeviceType.HardDisk; }
        }

        public override string DisplayName {
            get { return String.Format ("Hard Disk ({0})", Utility.FormatBytes (Capacity)); }
        }

        public VirtualHardDisk (string file, ushort busnum, ushort devnum, DiskBusType busType) {
            this.FileName = file;
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;

            ReadDescriptor ();
        }

        private void ReadDescriptor () {
            using (StreamReader reader = new StreamReader (File.OpenRead (file))) {
                ReadDescriptor (reader);
            }
        }

        private void ReadDescriptor (StreamReader reader) {
            string line;
            while ((line = reader.ReadLine ()) != null && reader.BaseStream.Position < 2048) {
                string key, value;
                if (Utility.ReadConfigLine (line, out key, out value)) {
                    switch (key) {
                    case "ddb.geometry.sectors":
                        sectors = Int32.Parse (value);
                        break;
                    case "ddb.geometry.heads":
                        heads = Int32.Parse (value);
                        break;
                    case "ddb.geometry.cylinders":
                        cylinders = Int32.Parse (value);
                        break;
                    default:
                        break;
                    }
                }
            }
        }

        public static VirtualHardDisk Create (string file, long sizeInMb, HardDiskType type) {
            Process proc = Process.Start (String.Format ("qemu-img create -f vmdk \"{0}\" \"{1}\"",
                                                         file, sizeInMb * 1024));
            proc.WaitForExit ();
            if (proc.ExitCode != 0) {
                throw new ApplicationException ("Failed to create virtual disk");
            }
            
            return new VirtualHardDisk (file, 0, 0, DiskBusType.Ide);
        }
    }

}
