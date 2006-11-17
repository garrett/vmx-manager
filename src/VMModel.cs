using System;
using Gtk;

namespace VMMan {

    public class VMModel : ListStore {

        private VirtualMachineManager manager;

        public VMModel (VirtualMachineManager manager) : base (typeof (VirtualMachine)) {
            this.manager = manager;

            manager.Added += delegate (object o, VirtualMachineArgs args) {
                Application.Invoke (delegate {
                    AddMachine (args.Machine);
                });
            };

            manager.Removed += delegate (object o, VirtualMachineArgs args) {
                Application.Invoke (delegate {
                    RemoveMachine (args.Machine);
                });
            };

            foreach (VirtualMachine machine in manager.Machines) {
                AddMachine (machine);
            }
        }

        private void AddMachine (VirtualMachine machine) {
            AppendValues (machine);
        }

        private void RemoveMachine (VirtualMachine machine) {
            TreeIter iter;
            if (FindMachine (machine, out iter)) {
                Remove (ref iter);
            }
        }

        private bool FindMachine (VirtualMachine machine, out TreeIter iter) {
            for (int i = 0; i < IterNChildren (); i++) {
                IterNthChild (out iter, i);

                VirtualMachine machine2 = (VirtualMachine) GetValue (iter, 0);
                if (machine.Equals (machine2)) {
                    return true;
                }
            }

            iter = TreeIter.Zero;
            return false;
        }
    }
}
