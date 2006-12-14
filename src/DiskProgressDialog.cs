using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class DiskProgressDialog : Dialog {

        private ProgressBar progressBar;
        private double progress;
        private uint source;

        public double Progress {
            get { return progress; }
            set {
                progress = value;
                if (source == 0) {
                    source = GLib.Timeout.Add (300, OnTimeout);
                }
            }
        }

        public DiskProgressDialog (Window parent) : base (Catalog.GetString ("Creating Hard Disk..."), parent,
                                                          DialogFlags.Modal | DialogFlags.NoSeparator) {

            VBox box = new VBox (false, 6);
            box.BorderWidth = 6;

            Label label = new Label (Catalog.GetString ("<b>Creating hard disk, please wait...</b>"));
            label.UseMarkup = true;
            box.Add (label);

            progressBar = new ProgressBar ();
            box.Add (progressBar);

            box.ShowAll ();
            VBox.Add (box);
        }

        private bool OnTimeout () {
            if (progressBar.Fraction != progress) {
                progressBar.Fraction = progress;
            }

            if (progress == 1.0) {
                Destroy ();
                return false;
            }

            return true;
        }

        public override void Destroy () {
            if (source > 0) {
                GLib.Source.Remove (source);
            }

            base.Destroy ();
        }
    }
}
