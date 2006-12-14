using System;
using System.IO;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class EntryPoint {
        
        public static int Main (string[] args) {

            if (args.Length == 0) {
                RunGUI ();
                return 0;
            }

            VirtualMachineManager manager = new VirtualMachineManager ();
            VirtualMachine machine;

            switch (args[0]) {
            case "--create-from-iso":
                if (args.Length < 2) {
                    Usage ();
                    return 1;
                }

                CreateFromIso (manager, args);
                break;
            case "--boot-from-iso":
                if (args.Length < 2) {
                    Usage ();
                    return 1;
                }
                
                machine = CreateFromIso (manager, args);
                machine.Start ();
                break;
            case "--create-blank":
                if (args.Length < 2) {
                    Usage ();
                    return 1;
                }

                manager.CreateMachine (args[1]);
                break;
            default:
                Usage ();
                break;
            }

            /*
            VirtualHardDisk disk = new VirtualHardDisk ("foobar.vmdk", 0, 0, DiskBusType.Scsi);
            disk.HardDiskType = HardDiskType.SplitSparse;
            disk.Capacity = (long) 6 * 1024 * 1024 * 1024;
            disk.Create ();
            */


            return 0;
        }

        private static VirtualMachine CreateFromIso (VirtualMachineManager manager, string[] args) {
            string iso = Path.GetFullPath (args[1]);
            string name = null;
            
            if (args.Length > 2) {
                name = args[2];
            }

            return manager.CreateMachineFromIso (iso, name);
        }

        private static void Usage () {
            Console.WriteLine ("Usage: vmman [options]");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            Console.WriteLine ("  --create-from-iso <iso> [name]    Create a machine from a bootable ISO");
            Console.WriteLine ("  --boot-from-iso <iso> [name]      Same as --create-from-iso but also boots");
            Console.WriteLine ("                                    the resulting virtual machine");
            Console.WriteLine ("  --create-blank <name>             Create a new blank machine");
        }

        private static void RunGUI () {
            Application.Init ();
            MainWindow win = new MainWindow ();
            win.ShowAll ();
            win.DeleteEvent += delegate {
                Application.Quit ();
            };

            try {
                Application.Run ();
            } catch (Exception e) {
                Console.Error.WriteLine (e);
                Utility.ShowError (win,
                                   Catalog.GetString ("An unexpected error occurred, Virtual Machine Manager will now exit:"),
                                   e.Message);
            }
        }
    }
}
