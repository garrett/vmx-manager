using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace VmxManager {

    public class VMView : TreeView {

        private MainController controller;
        private CellLayoutDataFunc cellTextLayoutFunc;
        private CellLayoutDataFunc cellPixbufLayoutFunc;

        public VMView (MainController controller) : base () {
            this.controller = controller;
            
            HeadersVisible = false;

            cellTextLayoutFunc = new CellLayoutDataFunc (OnCellTextLayout);
            cellPixbufLayoutFunc = new CellLayoutDataFunc (OnCellPixbufLayout);
            AppendColumn ("icon", new CellRendererPixbuf (), cellPixbufLayoutFunc);
            AppendColumn ("machines", new CellRendererText (), cellTextLayoutFunc);

            EnableModelDragDest (new TargetEntry[] { new TargetEntry ("text/uri-list", 0, 0) },
                                 Gdk.DragAction.Copy | Gdk.DragAction.Default);
            EnableModelDragSource (Gdk.ModifierType.Button1Mask,
                                   new TargetEntry[] { new TargetEntry ("text/uri-list", 0, 0) },
                                   Gdk.DragAction.Copy | Gdk.DragAction.Default);
                                   

            this.DragDataGet += OnDragDataGet;
            this.DragDataReceived += OnDragDataReceived;
        }

        private void OnDragDataGet (object o, DragDataGetArgs args) {
                
            if (args.SelectionData.Target != "text/uri-list")
                return;

            VirtualMachine machine = GetSelectedMachine ();

            args.SelectionData.Set (Gdk.Atom.Intern ("text/uri-list", false),
                                    8, Encoding.UTF8.GetBytes (controller.Manager.GetDesktopFileName (machine)));
        }

        private void OnDragDataReceived (object sender, DragDataReceivedArgs args) {
            if (args.SelectionData.Length > 0 && args.SelectionData.Format == 8) {
                string uris = Encoding.ASCII.GetString (args.SelectionData.Data, 0, args.SelectionData.Length);

                foreach (string str in uris.Trim ().Split ('\n')) {
                    string uristr = str.Trim (); // remove the '\r'
                    
                    if (uristr == String.Empty)
                        continue;
                    
                    Uri uri = new Uri (uristr);
                    if (!uri.IsFile) {
                        continue;
                    }

                    try {
                        VirtualMachine machine = new VirtualMachine (uri.LocalPath);
                        controller.Manager.AddMachine (machine);
                    } catch (Exception e) {
                        Console.Error.WriteLine ("Could not load virtual machine: " + e);
                    }
                }
            }
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
        }

        private void OnCellTextLayout (CellLayout layout, CellRenderer cell,
                                   TreeModel model, TreeIter iter) {

            CellRendererText textCell = (cell as CellRendererText);

            VirtualMachine machine = (VirtualMachine) model.GetValue (iter, 0);
            textCell.Markup = String.Format ("<b><span size=\"large\">{0}</span></b>\nStatus: {1}, Operating System: {2}", machine.Name, machine.IsRunning ? "Running" : "Stopped", machine.OperatingSystem.DisplayName);
        }

        private void OnCellPixbufLayout (CellLayout layout, CellRenderer cell,
                                   TreeModel model, TreeIter iter) {

            CellRendererPixbuf pixbufCell = (cell as CellRendererPixbuf);

            VirtualMachine machine = (VirtualMachine) model.GetValue (iter, 0);
            pixbufCell.Pixbuf = machine.PreviewIcon;
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
