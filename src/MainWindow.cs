using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using Gtk;

namespace VmxManager {

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

        private Viewport noMachinesWidget;

        private VirtualMachineManager manager;
        private VMView vmview;
        private VMModel vmmodel;
        private MainController controller;

        private ActionGroup actions;
        private UIManager ui;

        private bool havePlayer;

        /* this kills kittens */
        [DllImport ("libslab.so.0")]
        private static extern IntPtr shell_window_get_type ();

        public MainWindow () : base ("Virtual Machine Manager") {
            IconThemeUtils.SetWindowIcon (this);
            
            manager = new VirtualMachineManager ();
            controller = new MainController ();
            controller.MainWindow = this;
            controller.Manager = manager;
            
            Glade.XML xml = new Glade.XML ("vmx-manager.glade", "mainContent");
            xml.Autoconnect (this);

            noMachinesWidget = new Viewport ();
            noMachinesWidget.ShadowType = ShadowType.None;
            MessagePane pane = new MessagePane ();
            pane.HeaderIcon = IconThemeUtils.LoadIcon (48, "face-surprise", Stock.DialogInfo);
            pane.HeaderMarkup = Catalog.GetString ("<b>There are currently no virtual machines.</b>");
            pane.Append (Catalog.GetString ("You can add or create a new virtual machine using the buttons on the left"), false);
            pane.Show ();
            noMachinesWidget.Add (pane);
            
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
                                 Catalog.GetString ("Configure Machine"), "<control>p",
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

            actions = new ActionGroup ("VmxManager Actions");
            actions.Add (actionList);

            ui = new UIManager ();
            ui.InsertActionGroup (actions, 0);
            ui.AddUiFromResource ("vmx-manager.xml");

            havePlayer = Utility.CheckForPlayer ();
            if (!havePlayer) {
                HigMessageDialog dialog = new HigMessageDialog (this, DialogFlags.Modal, MessageType.Warning,
                                                                ButtonsType.Close,
                                                                Catalog.GetString ("VMware Player not found"),
                                                                Catalog.GetString ("It will not be possible to run virtual machines."));
                dialog.Run ();
                dialog.Destroy ();
            }
            
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

            foreach (VirtualMachine machine in manager.Machines) {
                machine.Started += OnMachineChanged;
                machine.Stopped += OnMachineChanged;
            }
            
            manager.Added += delegate (object o, VirtualMachineArgs args) {
                args.Machine.Started += OnMachineChanged;
                args.Machine.Stopped += OnMachineChanged;
            };
        }

        private void OnMachineChanged (object o, EventArgs args) {
            Application.Invoke (delegate {
                SetActionsSensitive ();
                vmview.QueueDraw ();
            });
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
            } catch {
            }
        }

        public void SetContent (Widget widget) {
            SetContentInternal (widget);
            SetActionsSensitive ();
        }

        public void SetContentInternal (Widget widget) {
            if (widget == null) {
                widget = vmview;
            }
            
            if (treeContent.Child != null) {
                treeContent.Remove (treeContent.Child);
            }
            
            treeContent.Add (widget);
            vmview.Show ();
        }

        private void SetActionsSensitive () {
            bool machineSelected = vmview.Selection.CountSelectedRows () > 0;

            configureButton.Sensitive = removeButton.Sensitive = actions.GetAction ("Configure").Sensitive =
                actions.GetAction ("Remove").Sensitive = vmview.IsMapped && machineSelected && vmview.GetSelectedMachine ().Status != VirtualMachineStatus.Running;

            startButton.Sensitive = actions.GetAction ("Start").Sensitive = configureButton.Sensitive && havePlayer;

            int count = manager.Machines.Count;
            if (count > 0 && noMachinesWidget.Parent != null) {
                SetContent (vmview);
                vmview.Show ();
            } else if (count == 0 && vmview.Parent != null) {
                SetContent (noMachinesWidget);
                noMachinesWidget.Show ();
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
