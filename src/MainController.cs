using System;
using Gtk;
using Mono.Unix;

namespace VmxManager {

    public class MainController {

        private VMView vmview;
        private VirtualMachineManager manager;
        private MainWindow window;

        public MainWindow MainWindow {
            get { return window; }
            set { window = value; }
        }
        
        public VMView VMView {
            get { return vmview; }
            set { vmview = value; }
        }

        public VirtualMachineManager Manager {
            get { return manager; }
            set { manager = value; }
        }

        public void OnStart (object o, EventArgs args) {
            if (!Utility.CheckForPlayer ())
                return;
            
            foreach (TreePath path in vmview.Selection.GetSelectedRows ()) {
                TreeIter iter;
                vmview.Model.GetIter (out iter, path);
                
                VirtualMachine machine = (VirtualMachine) vmview.Model.GetValue (iter, 0);
                if (!machine.IsRunning) {
                    manager.StartMachine (machine);
                }
            }
        }

        public void OnConfigure (object o, EventArgs args) {
            if (vmview.Selection.CountSelectedRows () == 0) {
                return;
            }
            
            ConfigDialog dialog = new ConfigDialog (vmview.GetSelectedMachine (), window);
            dialog.Response += delegate (object d, ResponseArgs rargs) {
                dialog.Hide ();

                bool saveResult = true;
                if (rargs.ResponseId == ResponseType.Ok) {
                    saveResult = dialog.Save ();
                }

                if (saveResult) {
                    dialog.Destroy ();
                }
            };
            
            dialog.Show ();
        }

        public void OnRemove (object o, EventArgs args) {

            HigMessageDialog dialog = new HigMessageDialog (window, DialogFlags.Modal, MessageType.Question,
                                                            ButtonsType.None,
                                                            Catalog.GetString ("Would you like to delete the virtual machine files, or keep them?"),
                                                            Catalog.GetString ("If you delete them, all data in the virtual machine will be lost."));
            dialog.AddButton (Catalog.GetString ("Keep"), ResponseType.No, true);
            dialog.AddButton (Stock.Delete, ResponseType.Yes, false);
            
            int response = dialog.Run ();
            dialog.Destroy ();
            
            foreach (TreePath path in vmview.Selection.GetSelectedRows ()) {
                TreeIter iter;
                vmview.Model.GetIter (out iter, path);
                
                VirtualMachine machine = (VirtualMachine) vmview.Model.GetValue (iter, 0);
                if (response == (int) ResponseType.Yes) {
                    machine.Delete ();
                }
                
                manager.RemoveMachine (machine);
            }
        }

        public void OnCreateBlank (object o, EventArgs args) {
            for (int i = 0;; i++) {
                string name;

                if (i == 0) {
                    name = Catalog.GetString ("New Virtual Machine");
                } else {
                    name = String.Format (Catalog.GetString ("New Virtual Machine #{0}"), i);
                }

                if (manager.GetMachine (name) == null && manager.GetMachineByFileName (name) == null) {
                    VirtualMachine machine = manager.CreateMachine (name);

                    // add a default 6gb disk
                    VirtualHardDisk disk = new VirtualHardDisk (0, 0, DiskBusType.Ide, (long) 6 * 1024 * 1024 * 1024);
                    disk.HardDiskType = HardDiskType.SplitSparse;
                    machine.AddHardDisk (disk);

                    // add virtual cd devices for each physical one (up to two)
                    ushort numdevs = 0;
                    foreach (string dev in Utility.FindCdDrives ()) {
                        if (numdevs > 1)
                            break;
                        
                        VirtualCdDrive drive = new VirtualCdDrive (dev, 1, numdevs++, DiskBusType.Ide,
                                                                   CdDeviceType.Raw);
                        machine.AddCdDrive (drive);
                    }
                    
                    ConfigDialog dialog = new ConfigDialog (machine, window);
                    dialog.Response += delegate (object d, ResponseArgs respargs) {
                        dialog.Hide ();

                        bool saveResult = true;
                        if (respargs.ResponseId == ResponseType.Ok) {
                            manager.AddMachine (machine);
                            saveResult = dialog.Save ();
                        }

                        if (saveResult) {
                            dialog.Destroy ();
                        }
                    };

                    dialog.Show ();
                    break;
                }
            }
        }
        
        public void OnCreateFromIso (object o, EventArgs args) {
            FileChooserDialog dialog = new FileChooserDialog (Catalog.GetString ("Choose a CD image"), window,
                                                              FileChooserAction.Open, Stock.Cancel,
                                                              ResponseType.Cancel, Stock.Open, ResponseType.Ok);

            dialog.LocalOnly = true;

            FileFilter filter = new FileFilter ();
            filter.Name = Catalog.GetString (Catalog.GetString ("CD Images"));
            filter.AddPattern ("*.iso");

            dialog.AddFilter (filter);
            ResponseType result = (ResponseType) dialog.Run ();
            if (result == ResponseType.Ok) {
                manager.CreateMachineFromIso (null, dialog.Filename);
            }

            dialog.Destroy ();
        }
        
        public void OnAddExisting (object o, EventArgs args) {
            FileChooserDialog dialog = new FileChooserDialog (Catalog.GetString ("Choose a virtual machine file"),
                                                              window,
                                                              FileChooserAction.Open, Stock.Cancel,
                                                              ResponseType.Cancel, Stock.Open, ResponseType.Ok);

            dialog.LocalOnly = true;

            FileFilter filter = new FileFilter ();
            filter.Name = Catalog.GetString ("Virtual Machines");
            filter.AddPattern ("*.vmx");

            dialog.AddFilter (filter);
            ResponseType result = (ResponseType) dialog.Run ();
            if (result == ResponseType.Ok) {
                foreach (string file in dialog.Filenames) {
                    manager.AddMachine (new VirtualMachine (file));
                }
            }

            dialog.Destroy ();
                
        }
    }
}
