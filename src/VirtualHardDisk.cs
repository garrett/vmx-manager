using System;
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

        public override VirtualDeviceType DeviceType {
            get { return VirtualDeviceType.HardDisk; }
        }

        public override string DisplayName {
            get { return "Hard Disk"; }
        }

        public VirtualHardDisk (string file, ushort busnum, ushort devnum, DiskBusType busType) {
            this.FileName = file;
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;
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
