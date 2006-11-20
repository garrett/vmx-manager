using System;

namespace VmxManager {

    public enum VirtualDeviceType {
        HardDisk,
        CdRom,
        Ethernet,
        Floppy
    }

    public enum ScsiDeviceType {
        Buslogic,
        LSILogic,
    }
    
    public enum EthernetDeviceType {
        PCnet,
        VMXnet,
        E1000
    }

    public enum NetworkType {
        Bridged,
        HostOnly,
        Nat
    }

    public enum HardDiskType {
        SingleSparse,
        SplitSparse,
        SingleFlat,
        SplitFlat
    }

    public enum CdDeviceType {
        Raw,
        Iso,
        Legacy
    }

    public enum DiskBusType {
        Ide,
        Scsi,
    }

}
