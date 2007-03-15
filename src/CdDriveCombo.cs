using System;
using Gtk;

namespace VmxManager {

    public class CdDriveCombo : ComboBox {

        private ListStore store;
        private Hal.Manager hal;

        public bool HaveDevices {
            get { return store.IterNChildren () > 0; }
        }
        
        public CdDriveCombo () {
            hal = new Hal.Manager ();
            store = new ListStore (typeof (string), typeof (string), typeof (string));

            Model = store;

            CellRendererText renderer = new CellRendererText ();
            PackStart (renderer, true);
            AddAttribute (renderer, "text", 0);

            Hal.Device[] list = hal.FindDeviceByCapabilityAsDevice ("storage.cdrom");
            if (list != null) {
                foreach (Hal.Device dev in list) {
                    AddDevice (dev);
                }
            }
        }

        public bool SetActiveDevice (string dev) {
            for (int i = 0; i < store.IterNChildren (); i++) {
                TreeIter iter;

                store.IterNthChild (out iter, i);
                if ((string) store.GetValue (iter, 1) == dev) {
                    Active = i;
                    return true;
                }
            }

            return false;
        }

        public string GetActiveDevice () {
            if (Active < 0) {
                return null;
            }
            
            TreeIter iter;
            store.IterNthChild (out iter, Active);

            return (string) store.GetValue (iter, 1);
        }

        private void AddDevice (Hal.Device dev) {
            store.AppendValues (dev.GetPropertyString ("storage.model"),
                                dev.GetPropertyString ("block.device"),
                                dev.Udi);
        }
    }
}
