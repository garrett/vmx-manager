using System;
using Gtk;

namespace VmxManager {

    public class ConfigDialog : Dialog {

        private VirtualMachine machine;
        private DeviceView devview;
        private DeviceModel devmodel;

        [Glade.Widget]
        private Widget configDialogContent;

        [Glade.Widget]
        private Entry nameEntry;

        [Glade.Widget]
        private ComboBox guestOsCombo;

        [Glade.Widget]
        private SpinButton memorySpin;

        [Glade.Widget]
        private CheckButton soundToggle;

        [Glade.Widget]
        private Container deviceContent;

        [Glade.Widget]
        private Menu addDevicePopup;

        [Glade.Widget]
        private ToggleButton addDeviceButton;

        [Glade.Widget]
        private Button removeDeviceButton;

        [Glade.Widget]
        private Button configureDeviceButton;

        public ConfigDialog (VirtualMachine machine, Window parent) :
            base ("Configure Virtual Machine", parent, DialogFlags.NoSeparator, Stock.Cancel, ResponseType.Cancel,
                  Stock.Ok, ResponseType.Ok) {

            this.machine = machine;

            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "configDialogContent");
            xml.Autoconnect (this);

            xml = new Glade.XML ("vmx-manager.glade", "addDevicePopup");
            xml.Autoconnect (this);

            guestOsCombo.Model = new OSModel ();
            
            CellRendererText renderer = new CellRendererText ();
            guestOsCombo.PackStart (renderer, false);
            guestOsCombo.AddAttribute (renderer, "text", 0);

            devview = new DeviceView ();
            devview.Selection.Changed += OnDeviceSelectionChanged;
            devmodel = new DeviceModel (machine);
            devview.Model = devmodel;
            deviceContent.Add (devview);
            devview.Show ();

            addDeviceButton.Toggled += delegate {
                if (addDeviceButton.Active) {
                    addDevicePopup.Popup ();
                }
            };

            addDevicePopup.Unmapped += delegate {
                addDeviceButton.Active = false;
            };

            removeDeviceButton.Clicked += OnRemoveDevice;
            configureDeviceButton.Clicked += OnConfigureDevice;

            VBox.Add (configDialogContent);
            DefaultHeight = 400;

            Load ();
        }

        private void SetOsCombo () {
            TreeIter iter;

            ListStore model = (ListStore) guestOsCombo.Model;
            for (int i = 0; i < model.IterNChildren (); i++) {
                model.IterNthChild (out iter, i);

                GuestOperatingSystem os = (GuestOperatingSystem) model.GetValue (iter, 1);
                if (os.Equals (machine.OperatingSystem)) {
                    guestOsCombo.SetActiveIter (iter);
                }
            }
        }

        private void Load () {
            nameEntry.Text = machine.Name;
            memorySpin.Value = machine.MemorySize;
            soundToggle.Active = machine.SoundEnabled;

            SetOsCombo ();
        }

        private void Save () {
            machine.Name = nameEntry.Text;
            machine.MemorySize = (int) memorySpin.Value;
            machine.SoundEnabled = soundToggle.Active;

            TreeIter iter;
            if (guestOsCombo.GetActiveIter (out iter)) {
                GuestOperatingSystem os = (GuestOperatingSystem) guestOsCombo.Model.GetValue (iter, 1);
                machine.OperatingSystem = os;
            }
            
            machine.Save ();
        }

        protected override void OnResponse (ResponseType response) {
            if (response == ResponseType.Ok) {
                Save ();
            }

            Destroy ();
        }

        private void OnAddHardDisk (object o, EventArgs args) {
            Console.WriteLine ("Adding hard disk");
        }

        private void OnAddCd (object o, EventArgs args) {
            Console.WriteLine ("Adding cd rom");
        }

        private void OnAddEthernet (object o, EventArgs args) {
            Console.WriteLine ("Adding ethernet");
        }

        private void OnAddFloppy (object o, EventArgs args) {
            Console.WriteLine ("Adding floppy");
        }

        private void OnRemoveDevice (object o, EventArgs args) {
            IVirtualDevice device = devview.GetSelectedDevice ();
            switch (device.DeviceType) {
            case VirtualDeviceType.HardDisk:
                machine.RemoveHardDisk ((VirtualHardDisk) device);
                break;
            case VirtualDeviceType.CdRom:
                break;
            case VirtualDeviceType.Ethernet:
                machine.RemoveEthernetDevice ((VirtualEthernet) device);
                break;
            default:
                break;
            }
        }

        private void OnConfigureDevice (object o, EventArgs args) {
        }

        private void OnDeviceSelectionChanged (object o, EventArgs args) {
            SetButtonsSensitive ();
        }

        private void SetButtonsSensitive () {
            bool selected = devview.Selection.CountSelectedRows () > 0;

            removeDeviceButton.Sensitive = configureDeviceButton.Sensitive = selected;
        }
    }
}
