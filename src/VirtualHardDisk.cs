using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Mono.Unix;

namespace VmxManager {
    

    public class HardDiskExtent {
        public const long DefaultGrainSize = 128; /* in sectors */

        private const int HeaderMagicNumber = 0x564d444b; /* 'V' 'M' 'D' 'K' */
        private const int FlatChunkSize = 8192;

        private ExtentAccess access;
        private long capacity;
        private ExtentType type;
        private string file;
        private long offset;

        private long grainSize = DefaultGrainSize;
        private long numTables = 0;
        private long numMetadataSectors = 0;
        private long numMetadataSectorsTotal;

        public ExtentAccess ExtentAccess {
            get { return access; }
            set { access = value; }
        }

        public long Capacity {
            get { return capacity; }
            set {
                capacity = value;

                // capacity must be a multiple of grain size
                if (type == ExtentType.Sparse) {
                    capacity += (DefaultGrainSize - capacity%DefaultGrainSize);
                }

                numTables = Utility.CeilingDivide (Utility.CeilingDivide (capacity, grainSize), 512);
                numMetadataSectors = (numTables * 4) + Utility.CeilingDivide (numTables * 4, 512);
                numMetadataSectorsTotal = (numMetadataSectors * 2) + (grainSize - (numMetadataSectors * 2)%grainSize);
            }
        }

        public ExtentType ExtentType {
            get { return type; }
            set { type = value; }
        }

        public string FileName {
            get { return file; }
            set { file = value; }
        }

        public long Offset {
            get { return offset; }
            set { offset = value; }
        }

        public HardDiskExtent (ExtentAccess access, long capacity, ExtentType type, string file) {
            this.ExtentAccess = access;
            this.Capacity = capacity;
            this.ExtentType = type;
            this.FileName = file;
            this.Offset = 0;
        }

        private void WriteSparseHeader (BinaryWriter writer) {
            // FIXME: support embedded descriptor
            
            writer.Write (HeaderMagicNumber);
            writer.Write (1); // version, always 1
            writer.Write (3); // flags, always 3
            writer.Write (capacity); // capacity in sectors
            writer.Write (grainSize); // grain size, also in sectors
            writer.Write ((long) 0); // embedded descriptor offset
            writer.Write ((long) 0); // embedded descriptor size
            writer.Write (512); // number of entries per grain table
            writer.Write ((long) 1); // offset of the redundant grain tables

            writer.Write ((long) 1 + numMetadataSectors); // offset of the 'normal' grain tables

            long paddedMetadataSize = (numMetadataSectors * 2) + (grainSize - (numMetadataSectors * 2)%grainSize);
            writer.Write (paddedMetadataSize); // number of sectors occupied by the metadata
            
            writer.Write ((byte) 0); // whether or not an unclean shutdown happened
            writer.Write ('\n');
            writer.Write (' ');
            writer.Write ('\r');
            writer.Write ('\n');
            writer.Write (new byte[435]); // padding to fill the rest of the sector
        }

        private void CreateFlat (BinaryWriter writer, ProgressHandler handler) {
            Utility.WritePadding (writer, capacity * 512, handler);
        }

        private void WriteGrainDirectory (BinaryWriter writer, ref int currentSector) {
            int remainder;
            int directoryPadding = 0;

            // the directory size must be padded to the nearest sector
            int directorySize = Math.DivRem ((int) numTables * 4, 512, out remainder);
            if (remainder > 0) {
                directorySize++;
                directoryPadding = 512 - remainder;
            }

            currentSector += directorySize;

            // write the directory entries
            for (long i = 0; i < numTables; i++) {
                writer.Write (currentSector);

                // each table is 4 sectors
                currentSector += 4;
            }

            // pad the directory
            Utility.WritePadding (writer, directoryPadding);

            // write out the blank grain tables
            byte[] blankTable = new byte[512 * 4];
            for (long i = 0; i < numTables; i++) {
                writer.Write (blankTable);
            }
        }

        private void CreateSparse (BinaryWriter writer) {
            WriteSparseHeader (writer);

            // one of the tables is the redundant one (obviously).
            int currentSector = 1;
            WriteGrainDirectory (writer, ref currentSector);
            WriteGrainDirectory (writer, ref currentSector);

            // add padding to the metadata, if necessary
            long padding = (numMetadataSectorsTotal - (numMetadataSectors * 2)) * 512;
            Utility.WritePadding (writer, padding);
        }

        public void Create () {
            Create (null);
        }

        public void Create (ProgressHandler handler) {
            using (BinaryWriter writer = new BinaryWriter (File.Open (file, FileMode.Create))) {
                if (type == ExtentType.Sparse) {
                    CreateSparse (writer);
                } else {
                    CreateFlat (writer, handler);
                }
            }

            if (handler != null) {
                handler (this, new ProgressArgs (1.0));
            }
        }

        public override string ToString () {
            string str = String.Format ("{0} {1} {2} {3}", access, capacity, type, file);
            if (offset > 0) {
                str += " " + offset;
            }

            return str;
        }
    }

    public delegate void VirtualHardDiskHandler (object o, VirtualHardDiskArgs args);

    public class VirtualHardDiskArgs : EventArgs {

        private VirtualHardDisk disk;

        public VirtualHardDisk Disk {
            get { return disk; }
        }

        public VirtualHardDiskArgs (VirtualHardDisk disk) {
            this.disk = disk;
        }
    }
    
    public class VirtualHardDisk : VirtualDisk {

        
        private const int SectorsIn2Gb = 4194304;

        private string adapter;
        private int sectors;
        private int heads;
        private int cylinders;
        private long capacity;
        private HardDiskType type;
        
        private List<HardDiskExtent> extents = new List<HardDiskExtent> ();

        public long Capacity {
            get {
                if (capacity > 0) {
                    return capacity;
                } else {
                    return (long) sectors * (long) heads * (long) cylinders * (long) 512;
                }
            } set {
                capacity = value;
            }
        }

        public HardDiskType HardDiskType {
            get { return type; }
            set {
                if (file != null && File.Exists (file)) {
                    throw new ApplicationException ("Cannot change the hard disk type of an existing disk");
                }
                
                type = value;
            }
        }

        public override VirtualDeviceType DeviceType {
            get { return VirtualDeviceType.HardDisk; }
        }

        public override string DisplayName {
            get { return String.Format ("Hard Disk ({0})", Utility.FormatBytes (Capacity)); }
        }

        public VirtualHardDisk (string file, ushort busnum, ushort devnum, DiskBusType busType) {
            this.FileName = file;
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;

            ReadDescriptor ();
        }

        public VirtualHardDisk (ushort busnum, ushort devnum, DiskBusType busType, long capacity) {
            this.busnum = busnum;
            this.devnum = devnum;
            this.busType = busType;
            this.capacity = capacity;
        }

        private void ReadDescriptor () {
            if (!File.Exists (file))
                return;
            
            using (StreamReader reader = new StreamReader (File.OpenRead (file))) {
                ReadDescriptor (reader);
            }
        }

        private void ReadDescriptor (StreamReader reader) {

            char[] buf = new char[4];
            reader.Read (buf, 0, 4);
            if (new String (buf) == "KDMV") {
                //the descriptor is in the 2nd sector (probably), seek there
                reader.BaseStream.Seek (512, SeekOrigin.Begin);
             }

            extents.Clear ();
            
            string line;
            while ((line = reader.ReadLine ()) != null && reader.BaseStream.Position < 1024) {

                HardDiskExtent extent = ParseExtentDescriptor (line);
                if (extent != null) {
                    extents.Add (extent);
                    continue;
                }
                
                string key, value;
                if (Utility.ReadConfigLine (line, out key, out value)) {
                    switch (key) {
                    case "createType":
                        type = Utility.ParseHardDiskType (value);
                        break;
                    case "ddb.geometry.sectors":
                        sectors = Int32.Parse (value);
                        break;
                    case "ddb.geometry.heads":
                        heads = Int32.Parse (value);
                        break;
                    case "ddb.geometry.cylinders":
                        cylinders = Int32.Parse (value);
                        break;
                    default:
                        break;
                    }
                }
            }
        }

        private HardDiskExtent ParseExtentDescriptor (string line) {
            if (!line.StartsWith ("RW") && !line.StartsWith ("RDONLY") && !line.StartsWith ("NOACCESS"))
                return null;

            string[] splitLine = line.Split (new char[] { ' ' }, 5);
            if (splitLine.Length < 4) {
                return null;
            }

            ExtentAccess access;
            long extentCapacity;
            ExtentType extentType;
            string extentFile;

            try {
                access = Utility.ParseExtentAccess (splitLine[0]);
            } catch {
                return null;
            }

            try {
                extentCapacity = Int64.Parse (splitLine[1]);
            } catch {
                return null;
            }

            try {
                extentType = Utility.ParseExtentType (splitLine[2]);
            } catch {
                return null;
            }

            extentFile = Path.Combine (Path.GetDirectoryName (file), Utility.StripDoubleQuotes (splitLine[3]));

            HardDiskExtent extent = new HardDiskExtent (access, extentCapacity, extentType, extentFile);

            try {
                extent.Offset = Int64.Parse (splitLine[4]);
            } catch {
            }

            return extent;
        }

        private void WriteDescriptor () {
            using (StreamWriter writer = new StreamWriter (File.Open (file, FileMode.Create))) {
                writer.Write ("#Disk Descriptor File\n\n");
                writer.Write ("version=1\n");
                writer.Write (String.Format ("CID={0}\n", new Random ().Next ().ToString ("x")));
                writer.Write ("parentCID=ffffffff\n");
                writer.Write (String.Format ("createType = \"{0}\"\n\n", Utility.HardDiskTypeToString (type)));

                writer.Write ("# Extent description\n\n");
                foreach (HardDiskExtent extent in extents) {
                    string line = String.Format ("{0} {1} {2} \"{3}\"",
                                                 Utility.ExtentAccessToString (extent.ExtentAccess),
                                                 extent.Capacity,
                                                 Utility.ExtentTypeToString (extent.ExtentType),
                                                 Path.GetFileName (extent.FileName));
                    if (extent.ExtentType == ExtentType.Flat) {
                        line += " " + extent.Offset;
                    }

                    writer.Write (line + "\n");
                }

                writer.Write ("\n#The Disk Data Base\n#DDB\n");
                writer.Write ("ddb.virtualHWVersion = \"3\"\n");

                string adapter;
                
                if (busType == DiskBusType.Ide) {
                    adapter = "ide";
                } else if (scsiType == ScsiDeviceType.Buslogic) {
                    adapter = "buslogic";
                } else {
                    adapter = "lsilogic";
                }
                
                writer.Write (String.Format ("ddb.adapterType = \"{0}\"\n", adapter));
                writer.Write (String.Format ("ddb.geometry.sectors = \"{0}\"\n", sectors));
                writer.Write (String.Format ("ddb.geometry.heads = \"{0}\"\n", heads));
                writer.Write (String.Format ("ddb.geometry.cylinders = \"{0}\"\n", cylinders));
            }
        }

        private int GetCapacityInSectors () {
            return (int) Utility.CeilingDivide (capacity, 512);
        }

        private void CalculateGeometry () {
            if (busType == DiskBusType.Scsi) {
                heads = 255;
                sectors = 63;
            } else {
                heads = 16;
                sectors = 63;
            }

            cylinders = ((GetCapacityInSectors () / heads) / sectors);
        }

        public void Delete () {
            foreach (HardDiskExtent extent in extents) {
                if (extent.FileName != null && File.Exists (extent.FileName)) {
                    File.Delete (extent.FileName);
                }
            }

            if (file != null && File.Exists (file)) {
                File.Delete (file);
            }
        }

        public void Create () {
            Create (null);
        }

        private void CheckDiskSpace () {
            UnixDriveInfo drive = null;
            
            foreach (UnixDriveInfo d in UnixDriveInfo.GetDrives ()) {
                if (file.IndexOf (d.RootDirectory.FullName) == 0 &&
                    (drive == null || d.RootDirectory.FullName.Length > drive.RootDirectory.FullName.Length)) {
                    drive = d;
                }
            }

            if (drive == null) {
                throw new ApplicationException (Catalog.GetString ("Failed to find mount point for disk"));
            }

            if (drive.AvailableFreeSpace < Capacity) {
                throw new ApplicationException (String.Format (Catalog.GetString ("You do not have enough free space to create this disk.  Please free up {0} of space."), Utility.FormatBytes (Capacity - drive.AvailableFreeSpace)));
            }
        }
        
        public void Create (ProgressHandler handler) {
            if (type == HardDiskType.SplitFlat ||
                type == HardDiskType.SingleFlat) {
                CheckDiskSpace ();
            }
            
            extents.Clear ();

            CalculateGeometry ();
            
            string extentFormatString = Path.Combine (Path.GetDirectoryName (file),
                                                      Path.GetFileNameWithoutExtension (file) + "-{0}.vmdk");
            
            if (type == HardDiskType.SplitFlat ||
                type == HardDiskType.SplitSparse) {

                long extentRemainder = 0;
                int numMaxExtents = (int) Math.DivRem (GetCapacityInSectors (), SectorsIn2Gb, out extentRemainder);

                for (int i = 0; i < numMaxExtents; i++) {
                    HardDiskExtent extent = new HardDiskExtent (ExtentAccess.ReadWrite, SectorsIn2Gb,
                                                                type == HardDiskType.SplitFlat ? ExtentType.Flat : ExtentType.Sparse,
                                                                String.Format (extentFormatString, i));
                    extents.Add (extent);
                }

                if (extentRemainder > 0) {
                    extents.Add (new HardDiskExtent (ExtentAccess.ReadWrite, extentRemainder,
                                                     type == HardDiskType.SplitFlat ? ExtentType.Flat : ExtentType.Sparse,
                                                     String.Format (extentFormatString, numMaxExtents)));
                }
            } else {
                extents.Add (new HardDiskExtent (ExtentAccess.ReadWrite, GetCapacityInSectors (),
                                                 type == HardDiskType.SingleFlat ? ExtentType.Flat : ExtentType.Sparse,
                                                 String.Format (extentFormatString, 0)));
            }

            WriteDescriptor ();
            for (int i = 0; i < extents.Count; i++) {
                HardDiskExtent extent = extents[i];
                if (handler == null) {
                    extent.Create ();
                } else {
                    extent.Create (delegate (object o, ProgressArgs args) {
                        double progress = (double) i / (double) extents.Count;
                        progress += args.Progress * ((double) 1 / (double) extents.Count);
                        handler (this, new ProgressArgs (progress));
                    });
                }
            }
        }
    }

}
