using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using fCraft.MapGeneration;
using fNbt;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    internal class MapCW : IMapExporter, IMapImporter {
        const string RootTagName = "ClassicWorld";


        internal MapCW() {
            ServerName = "fCraft/CloudBox";
            FileExtension = "cw";
            StorageType = MapStorageType.SingleFile;
            Format = MapFormat.ClassicWorld;
        }


        public string ServerName { get; private set; }
        public string FileExtension { get; private set; }
        public MapStorageType StorageType { get; private set; }
        public MapFormat Format { get; private set; }


        public void Save(Map mapToSave, string path) {
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                using (GZipStream gs = new GZipStream(fs, CompressionMode.Compress)) {
                    using (BufferedStream bs = new BufferedStream(gs, 8192)) {
                        NbtWriter writer = new NbtWriter(bs, RootTagName);
                        {
                            WriteHeader(mapToSave, path, writer);
                            writer.WriteByteArray("BlockArray", mapToSave.Blocks);
                            WriteMetadata(mapToSave, writer);
                        }
                        writer.EndCompound();
                        writer.Finish();
                    }
                }
            }
        }


        static void WriteHeader([NotNull] Map mapToSave, [NotNull] string path, [NotNull] NbtWriter writer) {
            writer.WriteByte("FormatVersion", 1);

            // write name and UUID
            World mapWorld = mapToSave.World;
            string mapName;
            if (mapWorld != null) {
                mapName = mapWorld.Name;
            } else {
                mapName = Path.GetFileNameWithoutExtension(path);
            }
            writer.WriteString("Name", mapName);
            writer.WriteByteArray("UUID", mapToSave.Guid.ToByteArray());

            // write map dimensions
            writer.WriteShort("X", (short)mapToSave.Width);
            writer.WriteShort("Y", (short)mapToSave.Height);
            writer.WriteShort("Z", (short)mapToSave.Length);

            // write spawn
            writer.BeginCompound("Spawn");
            {
                Position spawn = mapToSave.Spawn;
                writer.WriteShort("X", spawn.X);
                writer.WriteShort("Y", spawn.Z);
                writer.WriteShort("Z", spawn.Y);
                writer.WriteByte("H", spawn.R);
                writer.WriteByte("P", spawn.L);
            }
            writer.EndCompound();

            // write timestamps
            writer.WriteLong("TimeCreated", mapToSave.DateCreated.ToUnixTime());
            writer.WriteLong("LastModified", mapToSave.DateCreated.ToUnixTime());
            // TODO: TimeAccessed

            // TODO: write CreatedBy

            // Write map origin information
            writer.BeginCompound("MapGenerator");
            {
                writer.WriteString("Software", "fCraft " + Updater.CurrentRelease.VersionString);
                string genName;
                if (!mapToSave.Metadata.TryGetValue(MapGenUtil.ParamsMetaGroup,
                                                    MapGenUtil.GenNameMetaKey,
                                                    out genName)) {
                    genName = "Unknown";
                }
                writer.WriteString("MapGeneratorName", genName);
            }
            writer.EndCompound();
        }


        static void WriteMetadata([NotNull] Map mapToSave, [NotNull] NbtWriter writer) {
            writer.BeginCompound("Metadata");
            {
                // write fCraft's native metadata
                writer.BeginCompound("fCraft");
                {
                    string oldEntry = null;
                    foreach (MetadataEntry<string> entry in mapToSave.Metadata) {
                        if (oldEntry != entry.Group) {
                            // TODO: Modify MetadataCollection to allow easy iteration group-at-a-time
                            if (oldEntry != null) writer.EndCompound();
                            oldEntry = entry.Group;
                            writer.BeginCompound(entry.Group);
                        }
                        writer.WriteString(entry.Key, entry.Value);
                    }
                    if (oldEntry != null) writer.EndCompound();
                }
                writer.EndCompound();

                // TODO: write CPE metadata here

                // write foreign metadata
                if (MapUtility.PreserveForeignMetadata && mapToSave.ForeignMetadata != null) {
                    foreach (NbtTag metaGroup in mapToSave.ForeignMetadata) {
                        writer.WriteTag(metaGroup);
                    }
                }
            }
            writer.EndCompound();
        }


        public bool ClaimsName(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            return fileName.EndsWith(".cw", StringComparison.OrdinalIgnoreCase);
        }


        public bool Claims(string path) {
            return NbtFile.ReadRootTagName(path) == RootTagName;
        }


        public Map LoadHeader(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (GZipStream gs = new GZipStream(fs, CompressionMode.Decompress)) {
                    NbtReader reader = new NbtReader(gs);
                    reader.ReadToFollowing(); // skip root tag
                    reader.ReadToFollowing(); // skip to first inner tag
                    int width = 0,
                        length = 0,
                        height = 0;
                    do {
                        switch (reader.TagName) {
                            case "X":
                                width = reader.ReadValueAs<short>();
                                break;
                            case "Y":
                                height = reader.ReadValueAs<short>();
                                break;
                            case "Z":
                                length = reader.ReadValueAs<short>();
                                break;
                        }
                        if (width > 0 && length > 0 && height > 0) {
                            return new Map(null, width, length, height, false);
                        }
                    } while (reader.ReadToNextSibling());
                }
            }
            throw new MapFormatException("Could not locate map dimensions.");
        }


        public Map Load(string path) {
            NbtFile file = new NbtFile(path);
            NbtCompound root = file.RootTag;

            int formatVersion = root["FormatVersion"].ByteValue;
            if (formatVersion != 1) {
                throw new MapFormatException("Unsupported format version: " + formatVersion);
            }

            // Read dimensions and create the map
            Map map = new Map(null,
                              root["X"].ShortValue,
                              root["Z"].ShortValue,
                              root["Y"].ShortValue,
                              false);

            // read spawn coordinates
            NbtCompound spawn = (NbtCompound)root["Spawn"];
            map.Spawn = new Position(spawn["X"].ShortValue,
                                     spawn["Z"].ShortValue,
                                     spawn["Y"].ShortValue,
                                     spawn["H"].ByteValue,
                                     spawn["P"].ByteValue);

            // read UUID
            map.Guid = new Guid(root["UUID"].ByteArrayValue);

            // read creation/modification dates of the file (for fallback)
            DateTime fileCreationDate = File.GetCreationTime(path);
            DateTime fileModTime = File.GetCreationTime(path);

            // try to read embedded creation date
            NbtLong creationDate = root.Get<NbtLong>("TimeCreated");
            if (creationDate != null) {
                map.DateCreated = DateTimeUtil.ToDateTime(creationDate.Value);
            } else {
                // for fallback, pick the older of two filesystem dates
                map.DateCreated = (fileModTime > fileCreationDate) ? fileCreationDate : fileModTime;
            }

            // try to read embedded modification date
            NbtLong modTime = root.Get<NbtLong>("LastModified");
            if (modTime != null) {
                map.DateModified = DateTimeUtil.ToDateTime(modTime.Value);
            } else {
                // for fallback, use file modification date
                map.DateModified = fileModTime;
            }

            // TODO: LastAccessed

            // TODO: read CreatedBy and MapGenerator

            // read blocks
            map.Blocks = root["BlockArray"].ByteArrayValue;

            // TODO: CPE CustomBlock conversion

            // read metadata, if present
            NbtCompound metadata = root.Get<NbtCompound>("Metadata");
            if (metadata == null) return map;

            NbtCompound fCraftMetadata = metadata.Get<NbtCompound>("fCraft");
            if (fCraftMetadata != null) {
                foreach (NbtCompound groupTag in fCraftMetadata) {
                    string groupName = groupTag.Name;
                    foreach (NbtString keyValueTag in groupTag) {
                        // ReSharper disable AssignNullToNotNullAttribute // names are never null within compound
                        map.Metadata.Add(groupName, keyValueTag.Name, keyValueTag.Value);
                        // ReSharper restore AssignNullToNotNullAttribute
                    }
                }
            }

            // read CPE settings
            NbtCompound cpeMetadata = metadata.Get<NbtCompound>("CPE");
            if (cpeMetadata != null) {
                // TODO: CPE metadata
            }

            // preserve foreign metadata, if needed
            if (MapUtility.PreserveForeignMetadata) {
                metadata.Remove("fCraft");
                metadata.Remove("CPE");
                map.ForeignMetadata = metadata;
            }

            return map;
        }
    }
}
