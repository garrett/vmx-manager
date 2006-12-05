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
        private long capacity;

        public long Capacity {
            get {
                if (capacity > 0) {
                    return capacity;
                } else {
                    return (long) sectors * (long) heads * (long) cylinders * (long) 512;
                }
            } set {
                capacity = value;
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

        public VirtualHardDisk (ushort busnum, ushort devnum, DiskBusType busType, long capacity) {
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;
            this.capacity = capacity;
        }

        private void ReadDescriptor () {
            if (!File.Exists (file))
                return;
            
            using (StreamReader reader = new StreamReader (File.OpenRead (file))) {
                ReadDescriptor (reader);
            }
        }

        private void ReadDescriptor (StreamReader reader) {

            char[] buf = new char[4];
            reader.Read (buf, 0, 4);
            if (new String (buf) == "KDMV") {
                //the descriptor is in the 2nd sector, seek there
                reader.BaseStream.Seek (512, SeekOrigin.Begin);
            }
            
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

        private static void CreateDiskFile (string file, long sizeInMb, HardDiskType type) {
            // FIXME: use vmware-vdiskmanager if available
            // FIXME: throw something if we can't create the requested HardDiskType?
            
            Process proc = Process.Start ("/usr/bin/qemu-img", String.Format ("create -f vmdk \"{0}\" \"{1}\"",
                                                                              file, sizeInMb * 1024));
            proc.WaitForExit ();
            if (proc.ExitCode != 0) {
                throw new ApplicationException ("Failed to create virtual disk");
            }
        }

        public void Create (HardDiskType type) {
            CreateDiskFile (FileName, Capacity / 1024 / 1024, type);
        }

        public static VirtualHardDisk Create (string file, long sizeInMb, HardDiskType type) {
            CreateDiskFile (file, sizeInMb, type);
            return new VirtualHardDisk (file, 0, 0, DiskBusType.Ide);
        }
    }

}
