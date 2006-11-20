using System;
using Gtk;

namespace VmxManager {
    
    public class DeviceModel : ListStore {

        private VirtualMachine machine;

        public DeviceModel (VirtualMachine machine) : base (typeof (IVirtualDevice)) {
            this.machine = machine;

            foreach (VirtualHardDisk disk in machine.HardDisks) {
                AddDevice (disk);
            }

            foreach (VirtualCdDrive drive in machine.CdDrives) {
                AddDevice (drive);
            }

            foreach (VirtualEthernet dev in machine.EthernetDevices) {
                AddDevice (dev);
            }

            machine.HardDiskAdded += OnHardDiskAdded;
            machine.HardDiskRemoved += OnHardDiskRemoved;
            machine.CdDriveAdded += OnCdDriveAdded;
            machine.CdDriveRemoved += OnCdDriveRemoved;
            machine.EthernetDeviceAdded += OnEthernetDeviceAdded;
            machine.EthernetDeviceRemoved += OnEthernetDeviceRemoved;
        }

        private void OnHardDiskAdded (object o, VirtualHardDiskArgs args) {
            AddDevice (args.Disk);
        }

        private void OnHardDiskRemoved (object o, VirtualHardDiskArgs args) {
            RemoveDevice (args.Disk);
        }

        private void OnCdDriveAdded (object o, VirtualCdDriveArgs args) {
            AddDevice (args.Drive);
        }

        private void OnCdDriveRemoved (object o, VirtualCdDriveArgs args) {
            RemoveDevice (args.Drive);
        }

        private void OnEthernetDeviceAdded (object o, VirtualEthernetArgs args) {
            AddDevice (args.Device);
        }

        private void OnEthernetDeviceRemoved (object o, VirtualEthernetArgs args) {
            RemoveDevice (args.Device);
        }

        private void AddDevice (IVirtualDevice device) {
            AppendValues (device);
        }

        private void RemoveDevice (IVirtualDevice device) {
            TreeIter iter;
            if (FindDevice (device, out iter)) {
                Remove (ref iter);
            }
        }

        private bool FindDevice (IVirtualDevice device, out TreeIter iter) {
            for (int i = 0; i < IterNChildren (); i++) {
                IterNthChild (out iter, i);

                IVirtualDevice device2 = (IVirtualDevice) GetValue (iter, 0);
                if (device == device2) {
                    return true;
                }
            }

            iter = TreeIter.Zero;
            return false;
        }
    }
}
