using System;
using Mono.Unix;
using Gtk;

namespace VmxManager {

    public class Utility {

        public static ScsiDeviceType ParseScsiDeviceType (string device) {
            switch (device) {
            case "BUS":
                return ScsiDeviceType.Buslogic;
            case "LSI":
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

        public static bool ReadConfigLine (string line, out string key, out string value) {
            string[] splitLine = line.Split (new char[] { '=' }, 2);
                    
            if (splitLine.Length != 2) {
                key = null;
                value = null;
                return false;
            }
                
            key = splitLine[0].Trim ();

            value = splitLine[1].Trim ();
            if (value[0] == '"') {
                // naively strip the double quotes
                value = value.Substring (1, value.Length - 2);
            }

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
    }
}
