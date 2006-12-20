using System;
using System.Collections.Generic;
using Gtk;

namespace VmxManager {

    public class OSModel : TreeStore {

        public OSModel () : base (typeof (string), typeof (GuestOperatingSystem)) {

            Dictionary<string, List<GuestOperatingSystem>> dict = new Dictionary<string, List<GuestOperatingSystem>> ();
            
            foreach (GuestOperatingSystem os in GuestOperatingSystem.List ()) {
                List<GuestOperatingSystem> list;
                if (dict.ContainsKey (os.Section)) {
                    list = dict[os.Section];
                } else {
                    list = new List<GuestOperatingSystem> ();
                    dict[os.Section] = list;
                }

                list.Add (os);
            }

            foreach (string key in dict.Keys) {
                TreeIter iter = AppendValues (key, null);
                foreach (GuestOperatingSystem os in dict[key]) {
                    AppendValues (iter, os.DisplayName, os);
                }
            }
        }
    }
}
