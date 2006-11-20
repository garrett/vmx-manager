using System;
using System.IO;

namespace VmxManager {

    public abstract class VirtualDisk : IVirtualDevice {
        protected string file;
        protected ushort devnum;
        protected ushort busnum;
        protected DiskBusType busType;

        public abstract VirtualDeviceType DeviceType { get; }
        public abstract string DisplayName { get; }

        public string FileName {
            get { return file; }
            set {
                file = value;
                if (file != null && file != "auto detect") {
                    file = Path.GetFullPath (file);
                }
            }
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
    }
}
