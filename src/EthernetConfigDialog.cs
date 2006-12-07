using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class EthernetConfigDialog : Dialog {

        private VirtualEthernet device;

        [Glade.Widget]
        private Widget ethernetConfigContent;

        [Glade.Widget]
        private ComboBox ethernetTypeCombo;

        public EthernetConfigDialog (VirtualEthernet device, Window parent) : base (Catalog.GetString ("Configure Ethernet"),
                                                                                  parent, DialogFlags.NoSeparator,
                                                                                  Stock.Cancel, ResponseType.Cancel,
                                                                                  Stock.Ok, ResponseType.Ok) {
            this.device = device;

            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "ethernetConfigContent");
            xml.Autoconnect (this);

            VBox.Add (ethernetConfigContent);
            ethernetConfigContent.ShowAll ();

            Response += delegate (object o, ResponseArgs args) {
                if (args.ResponseId == ResponseType.Ok) {
                    Save ();
                }
                
                this.Destroy ();
            };

            Load ();
        }

        private void Load () {
            int index = 0;

            switch (device.NetworkType) {
            case NetworkType.Bridged:
                index = 0;
                break;
            case NetworkType.HostOnly:
                index = 1;
                break;
            case NetworkType.Nat:
                index = 2;
                break;
            }

            ethernetTypeCombo.Active = index;
        }

        private void Save () {

            switch (ethernetTypeCombo.Active) {
            case 0:
                device.NetworkType = NetworkType.Bridged;
                break;
            case 1:
                device.NetworkType = NetworkType.HostOnly;
                break;
            case 2:
                device.NetworkType = NetworkType.Nat;
                break;
            }
        }
    }
}
