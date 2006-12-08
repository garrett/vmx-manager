using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VmxManager {

    public class VirtualMachineManager {

        private static readonly string MachineDirectory = Path.Combine (Environment.GetEnvironmentVariable ("HOME"),
                                                                        "vmware");

        private List<VirtualMachine> machines = new List<VirtualMachine> ();
        private string configFile = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData) + "/vmx-manager/machines";

        public event VirtualMachineHandler Added;
        public event VirtualMachineHandler Removed;

        public ReadOnlyCollection<VirtualMachine> Machines {
            get { return new ReadOnlyCollection<VirtualMachine> (machines); }
        }
        
        public VirtualMachineManager () {
            try {
                LoadMachines ();
            } catch (FileNotFoundException) {
            } catch (DirectoryNotFoundException) {
            }
        }

        private void LoadMachines () {
            using (StreamReader reader = new StreamReader (File.OpenRead (configFile))) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    try {
                        machines.Add (new VirtualMachine (line));
                    } catch (Exception e) {
                        Console.Error.WriteLine ("Failed to load virtual machine '{0}': {1}", line, e);
                    }
                }
            }
        }

        private void SaveMachines () {
            string configDir = Path.GetDirectoryName (configFile);
            if (!Directory.Exists (configDir)) {
                Directory.CreateDirectory (configDir);
            }
            
            using (StreamWriter writer = new StreamWriter (File.Open (configFile, FileMode.Create))) {
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
    }
}
