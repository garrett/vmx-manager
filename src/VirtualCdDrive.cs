using System;
using System.IO;

namespace VmxManager {

    public delegate void VirtualCdDriveHandler (object o, VirtualCdDriveArgs args);

    public class VirtualCdDriveArgs : EventArgs {

        private VirtualCdDrive drive;

        public VirtualCdDrive Drive {
            get { return drive; }
        }

        public VirtualCdDriveArgs (VirtualCdDrive drive) {
            this.drive = drive;
        }
    }

    public class VirtualCdDrive : VirtualDisk {

        private CdDeviceType cdType;

        public override VirtualDeviceType DeviceType {
            get { return VirtualDeviceType.CdRom; }
        }
        
        public override string DisplayName {
            get {
                switch (cdType) {
                case CdDeviceType.Raw:
                    return "CD-ROM (Physical)";
                case CdDeviceType.Iso:
                    return String.Format ("CD-ROM ({0})", Path.GetFileName (file));
                case CdDeviceType.Legacy:
                    return "CD-ROM (Physical, Legacy mode)";
                default:
                    return String.Empty;
                }
            }
        }

        public CdDeviceType CdDeviceType {
            get { return cdType; }
            set { cdType = value; }
        }
        
        
        public VirtualCdDrive (string file, ushort busnum, ushort devnum, DiskBusType busType, CdDeviceType cdType) {
            this.FileName = file;
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;
            this.cdType = cdType;
        }
    }


}
