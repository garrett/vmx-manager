using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VmxManager {

    public class GuestOperatingSystem : IComparable<GuestOperatingSystem> {

        private static Dictionary<string, GuestOperatingSystem> oshash;

        private bool legacy;
        private ScsiDeviceType suggestedScsi;
        private EthernetDeviceType suggestedEthernet;
        private int suggestedRam;
        private string name;
        private string displayName;
        private string section;

        public bool IsLegacy {
            get { return legacy; }
        }

        public ScsiDeviceType SuggestedScsiDeviceType {
            get { return suggestedScsi; }
        }

        public EthernetDeviceType SuggestedEthernetDeviceType {
            get { return suggestedEthernet; }
        }

        public int SuggestedRam {
            get { return suggestedRam; }
        }

        public string Name {
            get { return name; }
        }

        public string DisplayName {
            get { return displayName; }
        }

        public string Section {
            get { return section; }
        }

        public GuestOperatingSystem (bool legacy, ScsiDeviceType suggestedScsi, EthernetDeviceType suggestedEthernet,
                                     int suggestedRam, string name, string displayName, string section) {
            this.legacy = legacy;
            this.suggestedScsi = suggestedScsi;
            this.suggestedEthernet = suggestedEthernet;
            this.suggestedRam = suggestedRam;
            this.name = name;
            this.displayName = displayName;
            this.section = section;
        }

        public override bool Equals (object o) {
            GuestOperatingSystem os = (o as GuestOperatingSystem);
            if (os == null)
                return false;

            return os.Name == this.Name;
        }

        public override int GetHashCode () {
            return this.Name.GetHashCode ();
        }

        public int CompareTo (GuestOperatingSystem os) {
            return this.DisplayName.CompareTo (os.DisplayName);
        }

        private static void Load () {
            oshash = new Dictionary<string, GuestOperatingSystem> ();

            using (StreamReader reader = new StreamReader (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("operating-systems.csv"))) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    string[] splitLine = line.Split (',');

                    string displayName = splitLine[6];
                    displayName = displayName.Trim ('"');

                    GuestOperatingSystem os = new GuestOperatingSystem (splitLine[2] == "TRUE",
                                                                        Utility.ParseScsiDeviceType (splitLine[4]),
                                                                        Utility.ParseEthernetDeviceType (splitLine[3]),
                                                                        Int32.Parse (splitLine[5]),
                                                                        splitLine[1], displayName,
                                                                        splitLine[0]);

                    oshash[splitLine[1]] = os;
                }
            }
        }

        public static ReadOnlyCollection<GuestOperatingSystem> List () {
            List<GuestOperatingSystem> list = new List<GuestOperatingSystem> (oshash.Values);
            list.Sort ();

            return new ReadOnlyCollection<GuestOperatingSystem> (list);
        }

        public static GuestOperatingSystem Lookup (string name) {
            if (oshash == null) {
                Load ();
            }

            return oshash[name];
        }
    }
}
