using System;
using Gtk;

namespace VmxManager {
    
    public class DeviceModel : ListStore {

        private VirtualMachine machine;

        public DeviceModel (VirtualMachine machine) : base (typeof (IVirtualDevice)) {
            this.machine = machine;

            foreach (VirtualDisk disk in machine.Disks) {
                Console.WriteLine ("Added disk");
                AppendValues (disk);
            }
        }

        /*
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
        */
    }
}
