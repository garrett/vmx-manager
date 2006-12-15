using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace VmxManager {

    public class VirtualMachineManager {

        private static readonly string MachineDirectory = Path.Combine (Environment.GetEnvironmentVariable ("HOME"),
                                                                        "vmware");
        private static readonly string ConfigDirectory = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "vmx-manager");
        private static readonly string ConfigFile = Path.Combine (ConfigDirectory, "machines");
        private static readonly string DesktopFileDirectory = Path.Combine (ConfigDirectory, "launchers");
        
        private List<VirtualMachine> machines = new List<VirtualMachine> ();

        public event VirtualMachineHandler Added;
        public event VirtualMachineHandler Removed;

        public ReadOnlyCollection<VirtualMachine> Machines {
            get { return new ReadOnlyCollection<VirtualMachine> (machines); }
        }
        
        public VirtualMachineManager () {
            LoadMachines ();
        }

        private void LoadMachines () {
            using (StreamReader reader = new StreamReader (File.OpenRead (ConfigFile))) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    try {
                        VirtualMachine machine = new VirtualMachine (line);
                        machines.Add (machine);
                        CreateDesktopFile (machine);
                    } catch (Exception e) {
                        File.Delete (GetDesktopFileName (line));
                        Console.Error.WriteLine ("Failed to load virtual machine '{0}': {1}", line, e);
                    }
                }
            }
        }

        private void SaveMachines () {
            string configDir = Path.GetDirectoryName (ConfigFile);
            if (!Directory.Exists (configDir)) {
                Directory.CreateDirectory (configDir);
            }
            
            using (StreamWriter writer = new StreamWriter (File.Open (ConfigFile, FileMode.Create))) {
                foreach (VirtualMachine machine in machines) {
                    writer.WriteLine (machine.FileName);
                }
            }
        }

        public void AddMachine (VirtualMachine machine) {
            foreach (VirtualMachine existing in machines) {
                if (existing.FileName == machine.FileName) {
                    return;
                }
            }
            
            machines.Add (machine);
            machine.NameChanged += OnMachineNameChanged;
            machine.FileNameChanged += delegate {
                SaveMachines ();
            };

            CreateDesktopFile (machine);

            // UGH: fake a name change in case it changed between now and when it was created
            OnMachineNameChanged (machine, new EventArgs ());
            
            SaveMachines ();

            VirtualMachineHandler handler = Added;
            if (handler != null) {
                handler (this, new VirtualMachineArgs (machine));
            }
        }

        private string CreateMachinePath (string name) {
            return String.Format ("{0}/{1}/{2}", MachineDirectory, name, name + ".vmx");
        }

        private void OnMachineNameChanged (object o, EventArgs args) {
            VirtualMachine machine = o as VirtualMachine;

            if (!File.Exists (machine.FileName)) {
                // machine has not been saved for the first time yet, we'll fix up the path
                machine.FileName = CreateMachinePath (machine.Name);
            }
        }

        public void RemoveMachine (VirtualMachine machine) {
            machines.Remove (machine);
            SaveMachines ();

            DeleteDesktopFile (machine);

            VirtualMachineHandler handler = Removed;
            if (handler != null) {
                handler (this, new VirtualMachineArgs (machine));
            }
        }

        public VirtualMachine CreateMachine (string name) {
            return VirtualMachine.Create (CreateMachinePath (name), name);
        }

        public VirtualMachine CreateMachineFromIso (string name, string iso) {
            if (name == null) {
                name = Path.GetFileNameWithoutExtension (iso);
            }

            VirtualMachine machine = CreateMachine (name);
            machine.AddCdDrive (new VirtualCdDrive (iso, 1, 0, DiskBusType.Ide, CdDeviceType.Iso));
            machine.Save ();
            
            return machine;
        }

        public VirtualMachine GetMachine (string name) {
            foreach (VirtualMachine machine in machines) {
                if (machine.Name == name) {
                    return machine;
                }
            }

            return null;
        }

        public VirtualMachine GetMachineByFileName (string file) {
            if (!Path.IsPathRooted (file)) {
                // this is crack
                file = CreateMachinePath (file);
            }

            foreach (VirtualMachine machine in machines) {
                if (machine.FileName == file) {
                    return machine;
                }
            }

            return null;
        }

        [DllImport ("libgnome-desktop-2.so.2")]
        private static extern IntPtr gnome_desktop_item_new_from_file (string file, int flags, IntPtr error);

        [DllImport ("libgnome-desktop-2.so.2")]
        private static extern int gnome_desktop_item_launch (IntPtr item, IntPtr args, int flags, IntPtr error);

        public void StartMachine (VirtualMachine machine) {
            if (machine.IsRunning) {
                return;
            }
            
            IntPtr ditem = gnome_desktop_item_new_from_file (GetDesktopFileName (machine), 0, IntPtr.Zero);
            if (ditem == IntPtr.Zero) {
                throw new ApplicationException (Catalog.GetString ("Failed to load launcher"));
            }

            gnome_desktop_item_launch (ditem, IntPtr.Zero, 0, IntPtr.Zero);
        }

        private string GetDesktopFileName (string machineFileName) {
            return Path.Combine (DesktopFileDirectory, machineFileName.GetHashCode () + ".desktop");
        }

        public string GetDesktopFileName (VirtualMachine machine) {
            return GetDesktopFileName (machine.FileName);
        }

        private void DeleteDesktopFile (VirtualMachine machine) {
            string file = GetDesktopFileName (machine);
            if (File.Exists (file)) {
                File.Delete (file);
            }
        }

        private string CreateDesktopFile (VirtualMachine machine) {
            string file = GetDesktopFileName (machine);
            if (File.Exists (file)) {
                return file;
            }

            StringBuilder builder = new StringBuilder ();
            builder.Append ("[Desktop Entry]\nVersion=1.0\nEncoding=UTF-8\n");
            builder.AppendFormat ("Name={0}\n", machine.Name);
            builder.Append ("GenericName=Virtual machine shortcut\n");
            builder.AppendFormat ("Exec=vmplayer \"{0}\"\n", machine.FileName);
            builder.Append ("Icon=vmx-manager\nStartupNotify=true\nTerminal=false\n");
            builder.Append ("Type=Application");

            if (!Directory.Exists (DesktopFileDirectory)) {
                Directory.CreateDirectory (DesktopFileDirectory);
            }
            
            using (StreamWriter writer = new StreamWriter (File.Open (file, FileMode.Create))) {
                writer.Write (builder.ToString ());
            }

            return file;
        }
    }
}
