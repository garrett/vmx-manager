using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
        private HardDiskType type;

        [DllImport ("libglib-2.0.so.0")]
        private static extern bool g_find_program_in_path (string program);

        public static IList<HardDiskType> SupportedTypes {
            get {
                if (CheckVDiskManager ()) {
                    return new List<HardDiskType> (new HardDiskType[] { HardDiskType.SingleSparse,
                                                                        HardDiskType.SplitSparse,
                                                                        HardDiskType.SingleFlat,
                                                                        HardDiskType.SplitFlat });
                } else if (CheckQemu ()) {
                    return new List<HardDiskType> (new HardDiskType[] { HardDiskType.SingleSparse });
                } else {
                    return new List<HardDiskType> ();
                }
            }
        }

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

        public HardDiskType HardDiskType {
            get { return type; }
            set {
                if (file != null && File.Exists (file)) {
                    throw new ApplicationException ("Cannot change the hard disk type of an existing disk");
                }
                
                type = value;
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
            Process proc;

            if (CheckVDiskManager ()) {
                int vdiskType = 0;
                switch (type) {
                case HardDiskType.SingleSparse:
                    vdiskType = 0;
                    break;
                case HardDiskType.SplitSparse:
                    vdiskType = 1;
                    break;
                case HardDiskType.SingleFlat:
                    vdiskType = 2;
                    break;
                case HardDiskType.SplitFlat:
                    vdiskType = 3;
                    break;
                }

                proc = Process.Start ("vmware-vdiskmanager",
                                      String.Format ("-c -s {0}Mb -a ide -t {1} \"{2}\"", sizeInMb,
                                                     vdiskType, file));
            } else if (CheckQemu () && type == HardDiskType.SingleSparse) {
                proc = Process.Start ("qemu-img", String.Format ("create -f vmdk \"{0}\" \"{1}\"",
                                                                 file, sizeInMb * 1024));
            } else {
                throw new ApplicationException ("Cannot create disk");
            }
            
            proc.WaitForExit ();
            if (proc.ExitCode != 0) {
                throw new ApplicationException ("Failed to create virtual disk");
            }
        }

        public void Delete () {
            // FIXME: does not work for split disks currently
            File.Delete (file);
        }

        public void Create () {
            CreateDiskFile (FileName, Capacity / 1024 / 1024, type);
        }

        public static VirtualHardDisk Create (string file, long sizeInMb, HardDiskType type) {
            CreateDiskFile (file, sizeInMb, type);
            return new VirtualHardDisk (file, 0, 0, DiskBusType.Ide);
        }

        private static bool CheckVDiskManager () {
            return g_find_program_in_path ("vmware-vdiskmanager");
        }

        private static bool CheckQemu () {
            return g_find_program_in_path ("qemu-img");
        }
    }

}
