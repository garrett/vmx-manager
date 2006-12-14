using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class CdConfigDialog : Dialog {

        private VirtualCdDrive drive;

        [Glade.Widget]
        private Widget cdConfigContent;

        [Glade.Widget]
        private ComboBox busTypeCombo;

        [Glade.Widget]
        private ComboBox busNumberCombo;

        [Glade.Widget]
        private ComboBox deviceNumberCombo;

        [Glade.Widget]
        private VBox cdComboContent;

        [Glade.Widget]
        private RadioButton physicalDriveRadio;

        [Glade.Widget]
        private RadioButton isoRadio;
        
        [Glade.Widget]
        private FileChooserButton isoChooserButton;

        private CdDriveCombo combo;

        public CdConfigDialog (VirtualCdDrive drive, Window parent) : base (Catalog.GetString ("Configure CD-ROM Drive"),
                                                                            parent, DialogFlags.NoSeparator,
                                                                            Stock.Cancel, ResponseType.Cancel,
                                                                            Stock.Ok, ResponseType.Ok) {
            this.drive = drive;

            IconThemeUtils.SetWindowIcon (this);

            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "cdConfigContent");
            xml.Autoconnect (this);

            busTypeCombo.Changed += delegate {
                PopulateDeviceNumberCombo ();
            };

            VBox.Add (cdConfigContent);
            cdConfigContent.ShowAll ();

            combo = new CdDriveCombo ();
            cdComboContent.Add (combo);
            combo.Show ();

            FileFilter filter = new FileFilter ();
            filter.Name = Catalog.GetString (Catalog.GetString ("CD Images"));
            filter.AddPattern ("*.iso");

            isoChooserButton.AddFilter (filter);

            physicalDriveRadio.Toggled += delegate {
                SetSensitivity ();
            };

            Response += delegate (object o, ResponseArgs args) {
                if (args.ResponseId == ResponseType.Ok) {
                    Save ();
                }
                
                this.Destroy ();
            };

            Load ();

            SetSensitivity ();
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
            if (drive.BusType == DiskBusType.Ide) {
                busTypeCombo.Active = 0;
            } else {
                busTypeCombo.Active = 1;
            }

            SetSensitivity ();

            busNumberCombo.Active = drive.BusNumber;
            deviceNumberCombo.Active = drive.DeviceNumber;
            combo.Active = 0;
            
            if ((drive.CdDeviceType == CdDeviceType.Raw ||
                 drive.CdDeviceType == CdDeviceType.Legacy) &&
                physicalDriveRadio.Sensitive) {

                physicalDriveRadio.Active = true;
                if (combo.Sensitive && !combo.SetActiveDevice (drive.FileName)) {
                    combo.Active = 0;
                }
            } else {
                isoRadio.Active = true;

                if (drive.FileName != null) {
                    isoChooserButton.SetFilename (drive.FileName);
                }
            }
                
        }

        private void Save () {
            if (busTypeCombo.Active == 0) {
                drive.BusType = DiskBusType.Ide;
            } else {
                drive.BusType = DiskBusType.Scsi;
            }

            drive.BusNumber = (ushort) busNumberCombo.Active;
            drive.DeviceNumber = (ushort) deviceNumberCombo.Active;

            drive.CdDeviceType = physicalDriveRadio.Active ? CdDeviceType.Raw : CdDeviceType.Iso;
            if (drive.CdDeviceType == CdDeviceType.Raw) {
                drive.FileName = combo.GetActiveDevice ();
            } else {
                drive.FileName = isoChooserButton.Filename;
            }
        }

        private void SetSensitivity () {
            physicalDriveRadio.Sensitive = combo.HaveDevices;
            combo.Sensitive = physicalDriveRadio.Active;
            isoChooserButton.Sensitive = isoRadio.Active;
        }
    }
}
