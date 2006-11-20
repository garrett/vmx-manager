using System;
using Mono.Unix;

namespace VmxManager {

    public delegate void VirtualEthernetHandler (object o, VirtualEthernetArgs args);

    public class VirtualEthernetArgs : EventArgs {

        private VirtualEthernet device;

        public VirtualEthernet Device {
            get { return device; }
        }

        public VirtualEthernetArgs (VirtualEthernet device) {
            this.device = device;
        }
    }

    public class VirtualEthernet : IVirtualDevice {

        private NetworkType netType;
        private string address;
        private EthernetDeviceType ethType;
        
        public VirtualDeviceType DeviceType {
            get { return VirtualDeviceType.Ethernet; }
        }

        public string DisplayName {
            get {
                switch (netType) {
                case NetworkType.Bridged:
                    return Catalog.GetString ("Ethernet (Bridged)");
                case NetworkType.HostOnly:
                    return Catalog.GetString ("Ethernet (Host only)");
                case NetworkType.Nat:
                    return Catalog.GetString ("Ethernet (NAT)");
                default:
                    return Catalog.GetString ("Ethernet");
                }
            }
        }

        public NetworkType NetworkType {
            get { return netType; }
            set { netType = value; }
        }

        public string Address {
            get { return address; }
            set { address = value; }
        }

        public EthernetDeviceType EthernetType {
            get { return ethType; }
            set { ethType = value; }
        }

        public VirtualEthernet (NetworkType netType, string address, EthernetDeviceType ethType) {
            this.netType = netType;
            this.address = address;
            this.ethType = ethType;
        }
    }

}

