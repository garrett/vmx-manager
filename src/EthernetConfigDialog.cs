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

        [Glade.Widget]
        private Label ethernetTypeLabel;

        public EthernetConfigDialog (VirtualEthernet device, Window parent) : base (Catalog.GetString ("Configure Ethernet"),
                                                                                  parent, DialogFlags.NoSeparator,
                                                                                  Stock.Cancel, ResponseType.Cancel,
                                                                                  Stock.Ok, ResponseType.Ok) {
            this.device = device;

            IconThemeUtils.SetWindowIcon (this);

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

            ethernetTypeCombo.Changed += OnComboChanged;

            Load ();
        }

        private void OnComboChanged (object o, EventArgs args) {
            switch (ethernetTypeCombo.Active) {
            case 0:
                // bridged
                ethernetTypeLabel.Text = Catalog.GetString ("With a bridged configuration, the ethernet card will be connected to the same network as the host computer.");
                break;
            case 1:
                // host only
                ethernetTypeLabel.Text = Catalog.GetString ("With this configuration, the ethernet card will be on a private network used only by the host computer and the virtual machine.");
                break;
            case 2:
                // nat
                ethernetTypeLabel.Text = Catalog.GetString ("With this configuration, the virtual machine will connect to the host's network through NAT (Network Address Translation)");
                break;
            }
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
