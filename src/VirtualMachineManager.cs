using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VMMan {

    public class VirtualMachineManager {

        private static readonly string MachineDirectory = Path.Combine (Environment.GetEnvironmentVariable ("HOME"),
                                                                        "vmware");

        private List<VirtualMachine> machines = new List<VirtualMachine> ();
        private string configFile = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData) + "/vmman/machines";

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
                    machines.Add (new VirtualMachine (line));
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
            SaveMachines ();

            VirtualMachineHandler handler = Added;
            if (handler != null) {
                handler (this, new VirtualMachineArgs (machine));
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

            string vmfile = String.Format ("{0}/{1}/{2}", MachineDirectory, name, name + ".vmx");
            string vmdir = Path.GetDirectoryName (vmfile);
            if (!Directory.Exists (vmdir)) {
                Directory.CreateDirectory (vmdir);
            }
            
            VirtualMachine machine = VirtualMachine.Create (vmfile, name);
            AddMachine (machine);
            machine.Save ();
            
            return machine;
        }

        public VirtualMachine CreateMachineFromIso (string name, string iso) {
            if (name == null) {
                name = Path.GetFileNameWithoutExtension (iso);
            }

            VirtualMachine machine = CreateMachine (name);
            machine.AddDisk (new VirtualDisk (iso, DiskDeviceType.CDIso, 1, 0, DiskBusType.Ide));
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
