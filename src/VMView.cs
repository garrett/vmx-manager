using System;
using Gtk;

namespace VMMan {

    public class VMView : TreeView {

        private MainController controller;
        private CellLayoutDataFunc cellLayoutFunc;

        public VMView (MainController controller) : base () {
            this.controller = controller;
            
            HeadersVisible = false;

            cellLayoutFunc = new CellLayoutDataFunc (OnCellLayout);
            AppendColumn ("machines", new CellRendererText (), cellLayoutFunc);
        }

        public VirtualMachine GetSelectedMachine () {
            if (Selection.CountSelectedRows () == 0)
                return null;

            TreeIter iter;
            Model.GetIter (out iter, Selection.GetSelectedRows ()[0]);
            return (VirtualMachine) Model.GetValue (iter, 0);
        }

        protected override void OnRowActivated (TreePath path, TreeViewColumn column) {
            controller.OnStart (this, new EventArgs ());
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

        private void OnCellLayout (CellLayout layout, CellRenderer cell,
                                   TreeModel model, TreeIter iter) {

            CellRendererText textCell = (cell as CellRendererText);

            VirtualMachine machine = (VirtualMachine) model.GetValue (iter, 0);
            textCell.Markup = String.Format ("<b><span size=\"large\">{0}</span></b>\nStatus: {1}, Operating System: {2}", machine.Name, machine.IsRunning ? "Running" : "Stopped", machine.OperatingSystem.DisplayName);
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey key) {
            if (key.Key == Gdk.Key.Delete) {
                controller.OnRemove (this, new EventArgs ());
                return true;
            } else {
                return base.OnKeyPressEvent (key);
            }
        }
    }
}
