using System;
using System.IO;
using Gtk;

namespace VMMan {

    public class EntryPoint {
        
        public static int Main (string[] args) {
            
            if (args.Length == 0) {
                RunGUI ();
                return 0;
            }

            VirtualMachineManager manager = new VirtualMachineManager ();

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
                
                VirtualMachine machine = CreateFromIso (manager, args);
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
            GuestOperatingSystem.Lookup ("suse");
            
            VirtualMachine mach = new VirtualMachine (args[0]);
            mach.Dump ();
            mach.Save ();
            */

            /*
            VirtualMachine machine = VirtualMachine.Create ("testing/Foo.vmx", "FooBar");
            machine.AddDisk (VirtualDisk.Create ("testing/Foo1.vmdk", 1024 * 2000));

            VirtualDisk secondary = VirtualDisk.Create ("testing/Foo2.vmdk", 1024 * 500);
            secondary.IsMaster = false;
            machine.AddDisk (secondary);
            
            machine.AddDisk (new VirtualDisk ("testing/current.iso", VirtualDiskType.CDIso, 1, true));
            machine.Dump ();
            machine.Save ();
            */

            /*
            Application.Init ();
            
            ShellWindow win = new ShellWindow ();
            win.ShowAll ();

            Application.Run ();
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
            Application.Run ();
        }
    }
}
