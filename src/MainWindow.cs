using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using Gtk;

namespace VMMan {

    public class MainWindow : Window {

        [Glade.Widget]
        private Widget mainContent;

        [Glade.Widget]
        private VBox leftPane;

        [Glade.Widget]
        private Label machineTasksLabel;

        [Glade.Widget]
        private Label commonTasksLabel;

        [Glade.Widget]
        private ScrolledWindow treeContent;

        [Glade.Widget]
        private Button startButton;

        [Glade.Widget]
        private Button configureButton;

        [Glade.Widget]
        private Button removeButton;

        [Glade.Widget]
        private Button createBlankButton;

        [Glade.Widget]
        private Button createFromIsoButton;

        [Glade.Widget]
        private Button addExistingButton;

        [Glade.Widget]
        private Widget placeholder;

        private VirtualMachineManager manager;
        private VMView vmview;
        private VMModel vmmodel;
        private MainController controller;

        private ActionGroup actions;
        private UIManager ui;

        /* this kills kittens */
        [DllImport ("libslab.so.0")]
        private static extern IntPtr shell_window_get_type ();

        public MainWindow () : base ("Virtual Machine Manager") {
            manager = new VirtualMachineManager ();
            controller = new MainController ();
            controller.MainWindow = this;
            controller.VMManager = manager;
            
            Glade.XML xml = new Glade.XML ("vmman.glade", "mainContent");
            xml.Autoconnect (this);

            xml = new Glade.XML ("vmman.glade", "placeholder");
            xml.Autoconnect (this);

            vmview = new VMView (controller);
            controller.VMView = vmview;
            
            vmmodel = new VMModel (manager);
            vmview.Model = vmmodel;

            treeContent.Add (vmview);
            Add (mainContent);

            ActionEntry[] actionList = {
                new ActionEntry ("Start", Stock.Execute,
                                 Catalog.GetString ("Start Machine"), "<control>r",
                                 Catalog.GetString ("Start the virtual machine"),
                                 controller.OnStart),
                new ActionEntry ("Configure", Stock.Properties,
                                 Catalog.GetString ("Configure Devices..."), "<control>p",
                                 Catalog.GetString ("Configure the connected devices"),
                                 controller.OnConfigure),
                new ActionEntry ("Remove", Stock.Remove,
                                 Catalog.GetString ("Remove Machine"), "<control>d",
                                 Catalog.GetString ("Remove the virtual machine"),
                                 controller.OnRemove),
                new ActionEntry ("CreateBlank", Stock.New,
                                 Catalog.GetString ("Create Blank Machine"), null,
                                 Catalog.GetString ("Create a new empty virtual machine"),
                                 controller.OnCreateBlank),
                new ActionEntry ("CreateFromIso", Stock.New,
                                 Catalog.GetString ("Create Machine From ISO"), null,
                                 Catalog.GetString ("Create a new machine which boots from a ISO"),
                                 controller.OnCreateFromIso),
                new ActionEntry ("AddMachine", Stock.Add,
                                 Catalog.GetString ("Add Existing Machine"), null,
                                 Catalog.GetString ("Add an existing virtual machine"),
                                 controller.OnAddExisting)
            };

            actions = new ActionGroup ("VMMan Actions");
            actions.Add (actionList);

            ui = new UIManager ();
            ui.InsertActionGroup (actions, 0);
            ui.AddUiFromResource ("vmman.xml");

            SetActionsSensitive ();

            DefaultWidth = 600;
            DefaultHeight = 400;

            vmview.ButtonPressEvent += OnTreeButtonPress;

            startButton.Clicked += controller.OnStart;
            configureButton.Clicked += controller.OnConfigure;
            removeButton.Clicked += controller.OnRemove;
            createBlankButton.Clicked += controller.OnCreateBlank;
            createFromIsoButton.Clicked += controller.OnCreateFromIso;
            addExistingButton.Clicked += controller.OnAddExisting;
            
            vmview.Selection.Changed += delegate {
                SetActionsSensitive ();
            };

            vmview.Model.RowInserted += delegate {
                SetActionsSensitive ();
            };

            vmview.Model.RowDeleted += delegate {
                SetActionsSensitive ();
            };
        }

        protected override void OnRealized () {
            base.OnRealized ();

            try {
                GLib.GType type = new GLib.GType (shell_window_get_type ());
                Style style = Rc.GetStyleByPaths (Settings, null, null, type);
                if (style != null) {
                    this.Style = style;
                    machineTasksLabel.Style = style;
                    commonTasksLabel.Style = style;
                }
            } catch (Exception) {
            }
        }

        protected override bool OnExposeEvent (Gdk.EventExpose e) {
            Gdk.GC gc = new Gdk.GC (this.GdkWindow);
            
            gc.Foreground = Style.Background (StateType.Active);

            GdkWindow.DrawRectangle (gc, true,
                                     leftPane.Allocation.X, leftPane.Allocation.Y,
                                     leftPane.Allocation.Width, leftPane.Allocation.Height);

            gc.Foreground = Style.Background (StateType.Selected);
            gc.SetLineAttributes (2, Gdk.LineStyle.Solid, Gdk.CapStyle.Butt, Gdk.JoinStyle.Miter);
            GdkWindow.DrawLine (gc, leftPane.Allocation.X + leftPane.Allocation.Width,
                                leftPane.Allocation.Y, leftPane.Allocation.X + leftPane.Allocation.Width,
                                leftPane.Allocation.Y + leftPane.Allocation.Height - 1);
            
            foreach (Widget child in Children) {
                PropagateExpose (child, e);
            }

            return false;
        }

        private void SetActionsSensitive () {
            bool machineSelected = vmview.Selection.CountSelectedRows () > 0;

            removeButton.Sensitive = configureButton.Sensitive = machineSelected;
            startButton.Sensitive = actions.GetAction ("Start").Sensitive = machineSelected && !vmview.GetSelectedMachine ().IsRunning;

            int count = manager.Machines.Count;
            if (count > 0 && placeholder.Parent != null) {
                treeContent.Remove (placeholder);
                treeContent.Add  (vmview);
                vmview.Show ();
            } else if (count == 0 && vmview.Parent != null) {
                treeContent.Remove (vmview);
                treeContent.Add (placeholder);
                placeholder.Show ();
            }
        }

        [GLib.ConnectBefore]
        private void OnTreeButtonPress (object o, ButtonPressEventArgs args) {
            if (args.Event.Button != 3) {
                args.RetVal = false;
                return;
            }

            Menu menu = (Menu) ui.GetWidget ("/ui/MachinePopup");
            menu.Popup ();
            args.RetVal = false;
        }
    }
}
