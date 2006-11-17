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

        public VirtualMachineManager VmxManagerager {
            get { return manager; }
            set { manager = value; }
        }

        public void OnStart (object o, EventArgs args) {
            foreach (TreePath path in vmview.Selection.GetSelectedRows ()) {
                TreeIter iter;
                vmview.Model.GetIter (out iter, path);
                
                VirtualMachine machine = (VirtualMachine) vmview.Model.GetValue (iter, 0);
                if (!machine.IsRunning) {
                    Console.WriteLine ("Starting machine: "+ machine.Name);
                    machine.Start ();
                }
            }
        }

        public void OnConfigure (object o, EventArgs args) {
            if (vmview.Selection.CountSelectedRows () == 0) {
                return;
            }
            
            ConfigDialog dialog = new ConfigDialog (vmview.GetSelectedMachine (), window);
            dialog.Show ();
        }

        public void OnRemove (object o, EventArgs args) {
            foreach (TreePath path in vmview.Selection.GetSelectedRows ()) {
                TreeIter iter;
                vmview.Model.GetIter (out iter, path);
                
                VirtualMachine machine = (VirtualMachine) vmview.Model.GetValue (iter, 0);
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

                if (manager.GetMachine (name) == null) {
                    manager.CreateMachine (name);
                    break;
                }
            }
        }
        
        public void OnCreateFromIso (object o, EventArgs args) {
            FileChooserDialog dialog = new FileChooserDialog ("Choose a CD image", window,
                                                              FileChooserAction.Open, Stock.Cancel,
                                                              ResponseType.Cancel, Stock.Open, ResponseType.Ok);

            dialog.LocalOnly = true;

            FileFilter filter = new FileFilter ();
            filter.Name = "CD Images";
            filter.AddPattern ("*.iso");

            dialog.AddFilter (filter);
            ResponseType result = (ResponseType) dialog.Run ();
            if (result == ResponseType.Ok) {
                manager.CreateMachineFromIso (null, dialog.Filename);
            }

            dialog.Destroy ();
        }
        
        public void OnAddExisting (object o, EventArgs args) {
            FileChooserDialog dialog = new FileChooserDialog ("Choose a virtual machine file", window,
                                                              FileChooserAction.Open, Stock.Cancel,
                                                              ResponseType.Cancel, Stock.Open, ResponseType.Ok);

            dialog.LocalOnly = true;

            FileFilter filter = new FileFilter ();
            filter.Name = "Virtual Machines";
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
