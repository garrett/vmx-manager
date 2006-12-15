using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class DiskProgressPane : Viewport {

        private ProgressBar progressBar;
        private double progress;
        private uint source;

        private MessagePane pane;

        public double Progress {
            get { return progress; }
            set {
                progress = value;
                if (source == 0) {
                    source = GLib.Timeout.Add (300, OnTimeout);
                }
            }
        }

        public DiskProgressPane () : base () {
            this.ShadowType = ShadowType.None;

            pane = new MessagePane ();
            pane.HeaderIconStock = Stock.DialogInfo;
            pane.HeaderMarkup = Catalog.GetString ("<b>Creating Hard Disks</b>");
            pane.Append (Catalog.GetString ("Creating the hard disk(s) may take several minutes, depending on their size."), false);

            progressBar = new ProgressBar ();
            pane.Append (progressBar, AttachOptions.Expand | AttachOptions.Fill, 0, false);
            progressBar.Show ();

            Add (pane);
            pane.Show ();
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
