using System;

namespace VmxManager {

    public class Utility {

        public static ScsiDeviceType ParseScsiDeviceType (string device) {
            switch (device) {
            case "BUS":
                return ScsiDeviceType.Buslogic;
            case "LSI":
                return ScsiDeviceType.LSILogic;
            default:
                Console.Error.WriteLine ("Unknown SCSI device type '{0}'", device);
                return ScsiDeviceType.Buslogic;
            }
        }

        public static EthernetDeviceType ParseEthernetDeviceType (string device) {
            switch (device) {
            case "vlance":
                return EthernetDeviceType.PCnet;
            case "vmxnet":
                return EthernetDeviceType.VMXnet;
            case "e1000":
                return EthernetDeviceType.E1000;
            default:
                Console.Error.WriteLine ("Unknown ethernet device type '{0}'", device);
                return EthernetDeviceType.PCnet;
            }
        }

        public static NetworkType ParseNetworkType (string netType) {
            switch (netType) {
            case "bridged":
                return NetworkType.Bridged;
            case "hostonly":
                return NetworkType.HostOnly;
            case "nat":
                return NetworkType.Nat;
            default:
                return NetworkType.Bridged;
            }
        }
    }

}
