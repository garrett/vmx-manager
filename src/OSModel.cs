using System;
using System.Collections.Generic;
using Gtk;

namespace VMMan {

    public class OSModel : ListStore {

        public OSModel () : base (typeof (string), typeof (GuestOperatingSystem)) {
            foreach (GuestOperatingSystem os in GuestOperatingSystem.List ()) {
                AppendValues (os.DisplayName, os);
            }
        }
    }
}
