using System;
using System.Collections.Generic;
using Gtk;

namespace VmxManager {

    public class OSModel : ListStore {

        public OSModel () : base (typeof (string), typeof (GuestOperatingSystem)) {

            foreach (GuestOperatingSystem os in GuestOperatingSystem.List ()) {
                AppendValues (os.DisplayName, os);
            }
        }
    }
}
