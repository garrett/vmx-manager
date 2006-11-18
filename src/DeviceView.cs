using System;
using Gtk;

namespace VmxManager {

    public class DeviceView : TreeView {

        private CellLayoutDataFunc pixbufLayoutFunc;
        private CellLayoutDataFunc textLayoutFunc;

        private Gdk.Pixbuf cdromPixbuf;
        private Gdk.Pixbuf hdPixbuf;
        private Gdk.Pixbuf ethernetPixbuf;
        private Gdk.Pixbuf floppyPixbuf;

        public DeviceView () : base () {
            HeadersVisible = false;

            pixbufLayoutFunc = new CellLayoutDataFunc (OnPixbufCellLayout);
            textLayoutFunc = new CellLayoutDataFunc (OnTextCellLayout);

            AppendColumn ("icon", new CellRendererPixbuf (), pixbufLayoutFunc);
            AppendColumn ("device", new CellRendererText (), textLayoutFunc);

            cdromPixbuf = IconThemeUtils.LoadIcon ("gnome-dev-cdrom", 24);
            hdPixbuf = IconThemeUtils.LoadIcon ("gnome-dev-harddisk", 24);
            ethernetPixbuf = IconThemeUtils.LoadIcon ("gnome-dev-pcmcia", 24); // FIXME
            floppyPixbuf = IconThemeUtils.LoadIcon ("gnome-dev-floppy", 24);

        }

        protected override void OnRowActivated (TreePath path, TreeViewColumn column) {
            /*
            TreeIter iter;
            Model.GetIter (out iter, path);

            VirtualMachine machine = (VirtualMachine) Model.GetValue (iter, 0);
            if (!machine.IsRunning) {
                Console.WriteLine ("Starting machine: "+ machine.Name);
                machine.Start ();
            }
            */
        }

        private void OnTextCellLayout (CellLayout layout, CellRenderer cell,
                                       TreeModel model, TreeIter iter) {

            CellRendererText textCell = (cell as CellRendererText);

            IVirtualDevice device = (IVirtualDevice) model.GetValue (iter, 0);
            textCell.Text = device.DisplayName;
        }

        private void OnPixbufCellLayout (CellLayout layout, CellRenderer cell,
                                         TreeModel model, TreeIter iter) {

            CellRendererPixbuf pixbufCell = (cell as CellRendererPixbuf);

            IVirtualDevice device = (IVirtualDevice) model.GetValue (iter, 0);

            Gdk.Pixbuf pixbuf;
            
            switch (device.DeviceType) {
            case VirtualDeviceType.HardDisk:
                pixbufCell.Pixbuf = hdPixbuf;
                break;
            case VirtualDeviceType.CdRom:
                pixbufCell.Pixbuf = cdromPixbuf;
                break;
            case VirtualDeviceType.Ethernet:
                pixbufCell.Pixbuf = ethernetPixbuf;
                break;
            case VirtualDeviceType.Floppy:
                pixbufCell.Pixbuf = floppyPixbuf;
                break;
            default:
                pixbufCell.Pixbuf = null;
                break;
            }
        }

        /*
        protected override bool OnKeyPressEvent (Gdk.EventKey key) {
            if (key.Key == Gdk.Key.Delete) {
                controller.OnRemove (this, new EventArgs ());
                return true;
            } else {
                return base.OnKeyPressEvent (key);
            }
        }
        */
    }
}
