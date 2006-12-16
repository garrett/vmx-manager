using System;
using System.IO;
using Gtk;
using Mono.Unix;

namespace VmxManager {

    public class ConfigDialog : Dialog {

        private VirtualMachine machine;
        private DeviceView devview;
        private DeviceModel devmodel;

        private ActionGroup actions;
        private UIManager ui;

        private MainWindow mainWindow;

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
        private CheckButton usbToggle;

        [Glade.Widget]
        private Container deviceContent;

        [Glade.Widget]
        private ToggleButton addDeviceButton;

        [Glade.Widget]
        private Button removeDeviceButton;

        [Glade.Widget]
        private Button configureDeviceButton;

        public VirtualMachine Machine {
            get { return machine; }
        }

        public ConfigDialog (VirtualMachine machine, MainWindow parent) :
            base ("Configure Virtual Machine", parent, DialogFlags.NoSeparator, Stock.Cancel, ResponseType.Cancel,
                  Stock.Ok, ResponseType.Ok) {

            this.mainWindow = parent;
            this.machine = machine;

            IconThemeUtils.SetWindowIcon (this);

            ActionEntry[] actionList = {
                new ActionEntry ("AddHardDisk", null,
                                 Catalog.GetString ("Hard Disk"), null,
                                 Catalog.GetString ("Add a hard disk"),
                                 OnAddHardDisk),
                new ActionEntry ("AddCdDrive", null,
                                 Catalog.GetString ("CD-ROM"), null,
                                 Catalog.GetString ("Add a CD-ROM drive"),
                                 OnAddCdDrive),
                new ActionEntry ("AddEthernet", null,
                                 Catalog.GetString ("Ethernet"), null,
                                 Catalog.GetString ("Add an ethernet device"),
                                 OnAddEthernet),
                new ActionEntry ("AddFloppy", null,
                                 Catalog.GetString ("Floppy"), null,
                                 Catalog.GetString ("Add a floppy drive"),
                                 OnAddFloppy),

            };

            actions = new ActionGroup ("VmxManager Device Actions");
            actions.Add (actionList);

            ui = new UIManager ();
            ui.InsertActionGroup (actions, 0);
            ui.AddUiFromResource ("vmx-manager-config.xml");

            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "configDialogContent");
            xml.Autoconnect (this);

            guestOsCombo.Model = new OSModel ();
            
            CellRendererText renderer = new CellRendererText ();
            guestOsCombo.PackStart (renderer, false);
            guestOsCombo.AddAttribute (renderer, "text", 0);

            devview = new DeviceView ();
            devview.RowActivated += delegate {
                OnConfigureDevice (this, new EventArgs ());
            };
            
            devview.Selection.Changed += OnDeviceSelectionChanged;
            devmodel = new DeviceModel (machine);
            devview.Model = devmodel;
            deviceContent.Add (devview);
            devview.Show ();

            addDeviceButton.Toggled += delegate {
                if (addDeviceButton.Active) {
                    Menu popup = (Menu) ui.GetWidget ("/ui/AddDevicePopup");
                    popup.Unmapped += delegate {
                        addDeviceButton.Active = false;
                    };

                    popup.Popup (null, null, OnPopupPosition, 0, Gtk.Global.CurrentEventTime);
                }
            };

            removeDeviceButton.Clicked += OnRemoveDevice;
            configureDeviceButton.Clicked += OnConfigureDevice;

            VBox.Add (configDialogContent);
            DefaultHeight = 400;

            int maxmem = Utility.GetHostMemorySize ();
            if (maxmem > 0) {
                memorySpin.SetRange (1.0, (double) (maxmem - 128));
            }

            Load ();
        }

        private void OnPopupPosition (Menu menu, out int x, out int y, out bool push_in) {
            addDeviceButton.GdkWindow.GetOrigin (out x, out y);

            x += addDeviceButton.Allocation.X + addDeviceButton.Allocation.Width;
            y += addDeviceButton.Allocation.Y;
            push_in = true;
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
            usbToggle.Active = machine.UsbEnabled;

            SetOsCombo ();
        }

        public bool Save () {
            machine.Name = nameEntry.Text;
            machine.MemorySize = (int) memorySpin.Value;
            machine.SoundEnabled = soundToggle.Active;
            machine.UsbEnabled = usbToggle.Active;

            TreeIter iter;
            if (guestOsCombo.GetActiveIter (out iter)) {
                GuestOperatingSystem os = (GuestOperatingSystem) guestOsCombo.Model.GetValue (iter, 1);
                machine.OperatingSystem = os;
            }

            string vmdir = System.IO.Path.GetDirectoryName (machine.FileName);
            if (!Directory.Exists (vmdir)) {
                Directory.CreateDirectory (vmdir);
            }

            bool haveFlat = false;
            foreach (VirtualHardDisk hd in machine.HardDisks) {
                if (hd.HardDiskType == HardDiskType.SplitFlat ||
                    hd.HardDiskType == HardDiskType.SingleFlat) {
                    haveFlat = true;
                    break;
                }
            }

            return SaveMachine (haveFlat);
        }

        private bool SaveMachine (bool showProgress) {
            DiskProgressPane pane = null;
            ProgressHandler handler = null;
            
            if (showProgress) {
                pane = new DiskProgressPane ();

                handler = delegate (object o, ProgressArgs args) {
                    if (!pane.IsMapped) {
                        Hide ();
                        mainWindow.SetContent (pane);
                        pane.Show ();
                    }
                    
                    pane.Progress = args.Progress;
                };
            }

            try {
                machine.Save (handler);
            } catch (Exception e) {
                Console.Error.WriteLine ("Failed to save machine: " + e);
                Utility.ShowError (Catalog.GetString ("Could not save changes"), e.Message);
                return false;
            } finally {
                if (pane != null) {
                    mainWindow.SetContent (null);
                }
            }

            return true;
        }

        private void OnAddHardDisk (object o, EventArgs args) {
            VirtualHardDisk disk = new VirtualHardDisk (0, 0, DiskBusType.Ide, (long) 6 * 1024 * 1024 * 1024);

            HardDiskConfigDialog dialog = new HardDiskConfigDialog (disk, true, this);
            dialog.Response += delegate (object b, ResponseArgs rargs) {
                if (rargs.ResponseId == ResponseType.Ok) {
                    machine.AddHardDisk (disk);
                }
            };

            dialog.Show ();
        }

        private void OnAddCdDrive (object o, EventArgs args) {
            VirtualCdDrive drive = new VirtualCdDrive ("/dev/hdc", 1, 0, DiskBusType.Ide, CdDeviceType.Raw);

            CdConfigDialog dialog = new CdConfigDialog (drive, this);
            dialog.Response += delegate (object b, ResponseArgs rargs) {
                if (rargs.ResponseId == ResponseType.Ok) {
                    machine.AddCdDrive (drive);
                }
            };

            dialog.Show ();
        }

        private void OnAddEthernet (object o, EventArgs args) {
            VirtualEthernet ethernet = new VirtualEthernet (NetworkType.Bridged, null, 
                                                            machine.OperatingSystem.SuggestedEthernetDeviceType);
            EthernetConfigDialog dialog = new EthernetConfigDialog (ethernet, this);
            dialog.Response += delegate (object b, ResponseArgs rargs) {
                if (rargs.ResponseId == ResponseType.Ok) {
                    machine.AddEthernetDevice (ethernet);
                }
            };

            dialog.Show ();
        }

        private void OnAddFloppy (object o, EventArgs args) {
            Console.WriteLine ("Adding floppy");
        }

        private void OnRemoveDevice (object o, EventArgs args) {
            IVirtualDevice device = devview.GetSelectedDevice ();
            switch (device.DeviceType) {
            case VirtualDeviceType.HardDisk:
                VirtualHardDisk disk = device as VirtualHardDisk;

                if (disk.FileName != null && File.Exists (disk.FileName)) {
                    HigMessageDialog dialog = new HigMessageDialog (this, DialogFlags.Modal, MessageType.Question,
                                                                    ButtonsType.None,
                                                                    Catalog.GetString ("Would you like to delete the disk files, or keep them?"),
                                                                    Catalog.GetString ("If you delete them, all data on the disk will be lost."));
                    dialog.AddButton (Catalog.GetString ("Keep"), ResponseType.No, true);
                    dialog.AddButton (Stock.Delete, ResponseType.Yes, false);
                    
                    int response = dialog.Run ();
                    dialog.Destroy ();
                
                    machine.RemoveHardDisk (disk);
                    if (response == (int) ResponseType.Yes) {
                        disk.Delete ();
                    }
                } else {
                    machine.RemoveHardDisk (disk);
                }
                break;
            case VirtualDeviceType.CdRom:
                machine.RemoveCdDrive ((VirtualCdDrive) device);
                break;
            case VirtualDeviceType.Ethernet:
                machine.RemoveEthernetDevice ((VirtualEthernet) device);
                break;
            default:
                break;
            }
        }

        private void OnConfigureDevice (object o, EventArgs args) {
            IVirtualDevice dev = devview.GetSelectedDevice ();

            Dialog dialog = null;
            
            switch (dev.DeviceType) {
            case VirtualDeviceType.HardDisk:
                VirtualHardDisk disk = (dev as VirtualHardDisk);
                dialog = new HardDiskConfigDialog ((VirtualHardDisk) dev,
                                                   disk.FileName == null || !File.Exists (disk.FileName),
                                                   this);
                break;
            case VirtualDeviceType.Ethernet:
                dialog = new EthernetConfigDialog ((VirtualEthernet) dev, this);
                break;
            case VirtualDeviceType.CdRom:
                dialog = new CdConfigDialog ((VirtualCdDrive) dev, this);
                break;
            default:
                break;
            }

            if (dialog != null) {
                dialog.Response += delegate {
                    devview.QueueDraw ();
                };
                
                dialog.Show ();
            }
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
