using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Unix;
using Mono.Unix.Native;
using Gtk;

namespace VmxManager {

    public class ProgressArgs : EventArgs {

        private double progress;

        public double Progress {
            get { return progress; }
        }
        
        public ProgressArgs (double progress) {
            this.progress = progress;
        }
    }

    public delegate void ProgressHandler (object o, ProgressArgs args);

    public class Utility {

        public static ScsiDeviceType ParseScsiDeviceType (string device) {
            switch (device) {
            case "BUS":
            case "buslogic":
                return ScsiDeviceType.Buslogic;
            case "LSI":
            case "lsilogic":
                return ScsiDeviceType.LSILogic;
            default:
                Console.Error.WriteLine ("Unknown SCSI device type '{0}'", device);
                return ScsiDeviceType.Buslogic;
            }
        }

        public static EthernetDeviceType ParseEthernetDeviceType (string device) {
            switch (device) {
            case "vlance":
                return EthernetDeviceType.PCnet;
            case "vmxnet":
                return EthernetDeviceType.VMXnet;
            case "e1000":
                return EthernetDeviceType.E1000;
            default:
                Console.Error.WriteLine ("Unknown ethernet device type '{0}'", device);
                return EthernetDeviceType.PCnet;
            }
        }

        public static string EthernetDeviceTypeToString (EthernetDeviceType type) {
            switch (type) {
            case EthernetDeviceType.PCnet:
                return "vlance";
            case EthernetDeviceType.VMXnet:
                return "vmxnet";
            case EthernetDeviceType.E1000:
                return "e1000";
            default:
                return null;
            }
        }

        public static NetworkType ParseNetworkType (string netType) {
            switch (netType) {
            case "bridged":
                return NetworkType.Bridged;
            case "hostonly":
                return NetworkType.HostOnly;
            case "nat":
                return NetworkType.Nat;
            default:
                return NetworkType.Bridged;
            }
        }

        public static string NetworkTypeToString (NetworkType type) {
            switch (type) {
            case NetworkType.Bridged:
                return "bridged";
            case NetworkType.HostOnly:
                return "hostonly";
            case NetworkType.Nat:
                return "nat";
            default:
                return null;
            }
        }

        public static CdDeviceType ParseCdDeviceType (string val) {
            switch (val) {
            case "cdrom-raw":
                return CdDeviceType.Raw;
            case "cdrom-image":
                return CdDeviceType.Iso;
            case "atapi-cdrom":
                return CdDeviceType.Legacy;
            default:
                Console.Error.WriteLine ("WARNING: Unknown disk type '{0}'", val);
                return CdDeviceType.Raw;
            }
        }

        public static string CdDeviceTypeToString (CdDeviceType cdType) {
            switch (cdType) {
            case CdDeviceType.Raw:
                return "cdrom-raw";
            case CdDeviceType.Iso:
                return "cdrom-image";
            case CdDeviceType.Legacy:
                return "atapi-cdrom";
            default:
                Console.WriteLine ("WARNING: Unknown disk type '{0}'", cdType);
                return "cdrom-raw";
            }
        }

        public static HardDiskType ParseHardDiskType (string val) {
            switch (val) {
            case "monolithicSparse":
                return HardDiskType.SingleSparse;
            case "monolithicFlat":
                return HardDiskType.SingleFlat;
            case "twoGbMaxExtentSparse":
                return HardDiskType.SplitSparse;
            case "twoGbMaxExtentFlat":
                return HardDiskType.SplitFlat;
            default:
                Console.Error.WriteLine ("WARNING: Unknown disk type '{0}'", val);
                return HardDiskType.SingleSparse;
            }
        }

        public static string HardDiskTypeToString (HardDiskType type) {
            switch (type) {
            case HardDiskType.SingleSparse:
                return "monolithicSparse";
            case HardDiskType.SingleFlat:
                return "monolithicFlat";
            case HardDiskType.SplitSparse:
                return "twoGbMaxExtentSparse";
            case HardDiskType.SplitFlat:
                return "twoGbMaxExtentFlat";
            default:
                return null;
            }
        }

        public static ExtentAccess ParseExtentAccess (string val) {
            switch (val) {
            case "RW":
                return ExtentAccess.ReadWrite;
            case "RDONLY":
                return ExtentAccess.ReadOnly;
            case "NOACCESS":
                return ExtentAccess.None;
            default:
                throw new ApplicationException ("Unknown extent type: " + val);
            }
        }

        public static string ExtentAccessToString (ExtentAccess access) {
            switch (access) {
            case ExtentAccess.ReadWrite:
                return "RW";
            case ExtentAccess.ReadOnly:
                return "RDONLY";
            case ExtentAccess.None:
                return "NOACCESS";
            default:
                return null;
            }
        }

        public static ExtentType ParseExtentType (string val) {
            switch (val) {
            case "FLAT":
                return ExtentType.Flat;
            case "SPARSE":
                return ExtentType.Sparse;
            default:
                throw new ApplicationException ("Unknown extent type: " + val);
            }
        }

        public static string ExtentTypeToString (ExtentType type) {
            switch (type) {
            case ExtentType.Flat:
                return "FLAT";
            case ExtentType.Sparse:
                return "SPARSE";
            default:
                return null;
            }
        }

        public static string StripDoubleQuotes (string value) {
            if (value[0] == '"' && value[value.Length - 1] == '"') {
                value = value.Substring (1, value.Length - 2);
            }

            return value;
        }

        public static bool ReadConfigLine (string line, out string key, out string value) {
            string[] splitLine = line.Split (new char[] { '=' }, 2);
                    
            if (splitLine.Length != 2) {
                key = null;
                value = null;
                return false;
            }
                
            key = splitLine[0].Trim ();

            value = StripDoubleQuotes (splitLine[1].Trim ());

            return true;
        }

        public static string FormatBytes (long val) {
            if (val == 0)
                return null;

            int level = 1;
            decimal dval = (decimal) val;

            while (dval > 1024) {
                dval = dval / 1024;
                level++;
            }

            string unit;
            
            switch (level) {
            case 1:
                unit = "byte";
                break;
            case 2:
                unit = "KB";
                break;
            case 3:
                unit = "MB";
                break;
            case 4:
                unit = "GB";
                break;
            case 5:
                unit = "TB";
                break;
            default:
                // riiiiiiight....
                return null;
            }

            return Decimal.Round (dval, 1) + " " + unit;
        }

        public static void ShowError (string m1, string m2) {
            ShowError (null, m1, m2);
        }
        
        public static void ShowError (Window parent, string m1, string m2) {
            string message = String.Format ("<b>{0}</b>\n\n{1}", GLib.Markup.EscapeText (m1), m2);
            
            MessageDialog dialog = new MessageDialog (parent, DialogFlags.Modal, MessageType.Error,
                                                      ButtonsType.Close, message);
            dialog.Title = Catalog.GetString ("Virtual Machine Manager Error");
            dialog.Run ();
            dialog.Destroy ();
        }

        [DllImport ("libglib-2.0.so.0")]
        private static extern IntPtr g_find_program_in_path (string program);

        public static bool CheckProgramAvailable (string program) {
            IntPtr result = g_find_program_in_path (program);
            if (result != IntPtr.Zero) {
                Stdlib.free (result);
            }

            return result != IntPtr.Zero;
        }

        public static bool CheckForPlayer () {
            return CheckProgramAvailable ("vmplayer");
        }

        public static long CeilingDivide (long a, long b) {
            long rem = 0;

            long result = Math.DivRem (a, b, out rem);
            if (rem > 0) {
                result++;
            }

            return result;
        }

        public static int CeilingDivide (int a, int b) {
            int rem = 0;

            int result = Math.DivRem (a, b, out rem);
            if (rem > 0) {
                result++;
            }

            return result;
        }

        public static void WritePadding (BinaryWriter writer, long amount) {
            WritePadding (writer, amount, null);
        }
        
        public static void WritePadding (BinaryWriter writer, long amount, ProgressHandler handler) {
            int chunkSize = 8192;
            
            byte[] zeros = new byte[chunkSize];
            long written = 0;

            double lastProgress = 0.0;
            
            while (written < amount) {
                long diff = amount - written;
                if (diff >= chunkSize) {
                    writer.Write (zeros);
                    written += chunkSize;
                } else {
                    writer.Write (new byte[diff]);
                    written += diff;
                }

                double progress = (double) written / (double) amount;
                if (progress >= (lastProgress + 0.01)) {
                    lastProgress = progress;

                    if (handler != null) {
                        handler (null, new ProgressArgs (progress));
                    }
                }

                while (handler != null && Application.EventsPending ()) {
                    Application.RunIteration ();
                }
            }
        }

        public static List<string> FindCdDrives () {
            Hal.Manager hal = new Hal.Manager ();

            List<string> devices = new List<string> ();
            foreach (Hal.Device dev in hal.FindDeviceByCapabilityAsDevice ("storage.cdrom")) {
                devices.Add (dev.GetPropertyString ("block.device"));
            }

            return devices;
        }

        public static int GetHostMemorySize () {
            Regex regex = new Regex ("MemTotal:.*?([0-9]+).*kB");
            using (StreamReader reader = new StreamReader (File.OpenRead ("/proc/meminfo"))) {
                string line;
                while ((line = reader.ReadLine ()) != null) {
                    Match match = regex.Match (line);
                    if (match.Success) {
                        return Int32.Parse (match.Groups[1].ToString ()) / 1024;
                    }
                }
            }

            return 0;
        }
    }
}
