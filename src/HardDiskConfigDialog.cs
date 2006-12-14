using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class HardDiskConfigDialog : Dialog {

        private VirtualHardDisk disk;

        [Glade.Widget]
        private Widget diskConfigContent;

        [Glade.Widget]
        private ComboBox busTypeCombo;

        [Glade.Widget]
        private ComboBox busNumberCombo;

        [Glade.Widget]
        private ComboBox deviceNumberCombo;

        [Glade.Widget]
        private SpinButton diskSizeSpin;

        [Glade.Widget]
        private ToggleButton allocateDiskCheck;

        public HardDiskConfigDialog (VirtualHardDisk disk, bool capacitySensitive, Window parent) : base (Catalog.GetString ("Configure Hard Disk"),
                                                                                  parent, DialogFlags.NoSeparator,
                                                                                  Stock.Cancel, ResponseType.Cancel,
                                                                                  Stock.Ok, ResponseType.Ok) {
            this.disk = disk;

            IconThemeUtils.SetWindowIcon (this);

            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "diskConfigContent");
            xml.Autoconnect (this);

            busTypeCombo.Changed += delegate {
                PopulateDeviceNumberCombo ();
            };
            
            VBox.Add (diskConfigContent);
            diskConfigContent.ShowAll ();

            diskSizeSpin.Sensitive = capacitySensitive;

            Response += delegate (object o, ResponseArgs args) {
                if (args.ResponseId == ResponseType.Ok) {
                    Save ();
                }
                
                this.Destroy ();
            };

            allocateDiskCheck.Sensitive = capacitySensitive;

            Load ();
        }

        private void PopulateDeviceNumberCombo () {
            int current = deviceNumberCombo.Active;
            
            for (int i = 0; i < 7; i++) {
                deviceNumberCombo.RemoveText (0);
            }

            int max = 2;
            if (busTypeCombo.Active == 1) {
                max = 7;
            }

            for (int i = 0; i < max; i++) {
                deviceNumberCombo.AppendText (i.ToString ());
            }

            if (current < max) {
                deviceNumberCombo.Active = current;
            } else {
                deviceNumberCombo.Active = max - 1;
            }
        }

        private void Load () {
            if (disk.BusType == DiskBusType.Ide) {
                busTypeCombo.Active = 0;
            } else {
                busTypeCombo.Active = 1;
            }

            busNumberCombo.Active = disk.BusNumber;
            deviceNumberCombo.Active = disk.DeviceNumber;
            diskSizeSpin.Value = (double) disk.Capacity / (double) 1024 / (double) 1024 / (double) 1024;

            if (disk.HardDiskType == HardDiskType.SingleFlat ||
                disk.HardDiskType == HardDiskType.SplitFlat) {
                allocateDiskCheck.Active = true;
            }
        }

        private void Save () {
            if (busTypeCombo.Active == 0) {
                disk.BusType = DiskBusType.Ide;
            } else {
                disk.BusType = DiskBusType.Scsi;
            }

            disk.BusNumber = (ushort) busNumberCombo.Active;
            disk.DeviceNumber = (ushort) deviceNumberCombo.Active;

            if (diskSizeSpin.Sensitive) {
                disk.Capacity = (long) (diskSizeSpin.Value * (double) 1024 * (double) 1024 * (double) 1024);
            }

            if (allocateDiskCheck.Sensitive && allocateDiskCheck.Active) {
                disk.HardDiskType = HardDiskType.SplitFlat;
            } else {
                disk.HardDiskType = HardDiskType.SplitSparse;
            }
        }
    }
}
