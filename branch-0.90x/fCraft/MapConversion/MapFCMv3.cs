// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> fCraft map format converter, for format version #3 (2011).
    /// Soon to be obsoleted by FCMv4. </summary>
    internal sealed class MapFCMv3 : IMapImporter, IMapExporter {
        const int Identifier = 0x0FC2AF40;
        const byte Revision = 13;

        public string ServerName {
            get { return "fCraft"; }
        }

        public string FileExtension {
            get { return "fcm"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.FCMv3; }
        }


        public bool ClaimsName(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            return fileName.EndsWith(".fcm", StringComparison.OrdinalIgnoreCase);
        }


        public bool Claims(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            using (FileStream mapStream = File.OpenRead(fileName)) {
                try {
                    BinaryReader reader = new BinaryReader(mapStream);
                    int id = reader.ReadInt32();
                    int rev = reader.ReadByte();
                    return (id == Identifier && rev == Revision);
                } catch (Exception) {
                    return false;
                }
            }
        }


        public Map LoadHeader(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            using (FileStream mapStream = File.OpenRead(fileName)) {
                BinaryReader reader = new BinaryReader(mapStream);

                Map map = LoadHeaderInternal(reader);

                // skip the index
                int layerCount = reader.ReadByte();
                mapStream.Seek(25*layerCount, SeekOrigin.Current);

                // read metadata
                int metaCount = reader.ReadInt32();

                using (DeflateStream ds = new DeflateStream(mapStream, CompressionMode.Decompress)) {
                    BinaryReader br = new BinaryReader(ds);
                    for (int i = 0; i < metaCount; i++) {
                        string group = ReadLengthPrefixedString(br);
                        string key = ReadLengthPrefixedString(br);
                        string newValue = ReadLengthPrefixedString(br);

                        string oldValue;
                        if (map.Metadata.TryGetValue(key, group, out oldValue) && oldValue != newValue) {
                            Logger.Log(LogType.Warning,
                                       "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                       "Old value (overwritten): \"{2}\". New value: \"{3}\"",
                                       group,
                                       key,
                                       oldValue,
                                       newValue);
                        }
                        if (group == "zones") {
                            try {
                                map.Zones.Add(new Zone(newValue, map.World));
                            } catch (Exception ex) {
                                Logger.Log(LogType.Error,
                                           "MapFCMv3.LoadHeader: Error importing zone definition: {0}",
                                           ex);
                            }
                        } else {
                            map.Metadata[group, key] = newValue;
                        }
                    }
                }

                return map;
            }
        }


        public Map Load(string fileName) {
            if (fileName == null) throw new ArgumentNullException("fileName");
            using (FileStream mapStream = File.OpenRead(fileName)) {
                BinaryReader reader = new BinaryReader(mapStream);

                Map map = LoadHeaderInternal(reader);

                // read the layer index
                int layerCount = reader.ReadByte();
                if (layerCount < 1) {
                    throw new MapFormatException("MapFCMv3: No data layers found.");
                }
                mapStream.Seek(25*layerCount, SeekOrigin.Current);

                // read metadata
                int metaSize = reader.ReadInt32();

                using (DeflateStream ds = new DeflateStream(mapStream, CompressionMode.Decompress)) {
                    BinaryReader br = new BinaryReader(ds);
                    for (int i = 0; i < metaSize; i++) {
                        string group = ReadLengthPrefixedString(br);
                        string key = ReadLengthPrefixedString(br);
                        string newValue = ReadLengthPrefixedString(br);

                        string oldValue;
                        if (map.Metadata.TryGetValue(key, group, out oldValue) && oldValue != newValue) {
                            Logger.Log(LogType.Warning,
                                       "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                       "Old value (overwritten): \"{2}\". New value: \"{3}\"",
                                       group,
                                       key,
                                       oldValue,
                                       newValue);
                        }
                        if (group == "zones") {
                            try {
                                map.Zones.Add(new Zone(newValue, map.World));
                            } catch (Exception ex) {
                                Logger.Log(LogType.Error,
                                           "MapFCMv3.LoadHeader: Error importing zone definition: {0}",
                                           ex);
                            }
                        } else {
                            map.Metadata[group, key] = newValue;
                        }
                    }
                    map.Blocks = new byte[map.Volume];
                    ds.Read(map.Blocks, 0, map.Blocks.Length);
                    map.RemoveUnknownBlockTypes();
                }
                return map;
            }
        }


        [NotNull]
        static Map LoadHeaderInternal([NotNull] BinaryReader reader) {
            if (reader == null) throw new ArgumentNullException("reader");
            if (reader.ReadInt32() != Identifier || reader.ReadByte() != Revision) {
                throw new MapFormatException("FCMv3: Unexpected file identifier");
            }

            // read dimensions
            int width = reader.ReadInt16();
            int height = reader.ReadInt16();
            int length = reader.ReadInt16();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map(null, width, length, height, false);
            // ReSharper restore UseObjectOrCollectionInitializer

            // read spawn
            short x = (short)reader.ReadInt32();
            short z = (short)reader.ReadInt32();
            short y = (short)reader.ReadInt32();
            map.Spawn = new Position(x,
                                     y,
                                     z,
                                     reader.ReadByte(),
                                     reader.ReadByte());

            // read modification/creation times
            map.DateModified = DateTimeUtil.ToDateTime(reader.ReadUInt32());
            map.DateCreated = DateTimeUtil.ToDateTime(reader.ReadUInt32());

            // read UUID
            map.Guid = new Guid(reader.ReadBytes(16));
            return map;
        }


        public void Save(Map mapToSave, string fileName) {
            if (mapToSave == null) throw new ArgumentNullException("mapToSave");
            if (fileName == null) throw new ArgumentNullException("fileName");
            using (FileStream mapStream = File.Create(fileName)) {
                BinaryWriter writer = new BinaryWriter(mapStream);

                writer.Write(Identifier);
                writer.Write(Revision);

                writer.Write((short)mapToSave.Width);
                writer.Write((short)mapToSave.Height);
                writer.Write((short)mapToSave.Length);

                writer.Write((int)mapToSave.Spawn.X);
                writer.Write((int)mapToSave.Spawn.Z);
                writer.Write((int)mapToSave.Spawn.Y);

                writer.Write(mapToSave.Spawn.R);
                writer.Write(mapToSave.Spawn.L);

                mapToSave.DateModified = DateTime.UtcNow;
                writer.Write((uint)mapToSave.DateModified.ToUnixTime());
                writer.Write((uint)mapToSave.DateCreated.ToUnixTime());

                writer.Write(mapToSave.Guid.ToByteArray());

                writer.Write((byte)1); // layer count

                // skip over index and metacount
                long indexOffset = mapStream.Position;
                writer.Seek(29, SeekOrigin.Current);

                byte[] blocksCache = mapToSave.Blocks;
                int metaCount, compressedLength;
                long offset;
                using (DeflateStream ds = new DeflateStream(mapStream, CompressionMode.Compress, true)) {
                    using (BufferedStream bs = new BufferedStream(ds)) {
                        // write metadata
                        metaCount = WriteMetadata(bs, mapToSave);
                        offset = mapStream.Position; // inaccurate, but who cares
                        bs.Write(blocksCache, 0, blocksCache.Length);
                        compressedLength = (int)(mapStream.Position - offset);
                    }
                }

                // come back to write the index
                writer.BaseStream.Seek(indexOffset, SeekOrigin.Begin);

                writer.Write((byte)0); // data layer type (Blocks)
                writer.Write(offset); // offset, in bytes, from start of stream
                writer.Write(compressedLength); // compressed length, in bytes
                writer.Write(0); // general purpose field
                writer.Write(1); // element size
                writer.Write(blocksCache.Length); // element count

                writer.Write(metaCount);
            }
        }


        [NotNull]
        static string ReadLengthPrefixedString([NotNull] BinaryReader reader) {
            if (reader == null) throw new ArgumentNullException("reader");
            int length = reader.ReadUInt16();
            byte[] stringData = reader.ReadBytes(length);
            return Encoding.ASCII.GetString(stringData);
        }


        static void WriteLengthPrefixedString([NotNull] BinaryWriter writer, [NotNull] string str) {
            if (writer == null) throw new ArgumentNullException("writer");
            if (str == null) throw new ArgumentNullException("str");
            if (str.Length > ushort.MaxValue) throw new ArgumentException("String is too long.", "str");
            byte[] stringData = Encoding.ASCII.GetBytes(str);
            writer.Write((ushort)stringData.Length);
            writer.Write(stringData);
        }


        static int WriteMetadata([NotNull] Stream stream, [NotNull] Map map) {
            if (stream == null) throw new ArgumentNullException("stream");
            if (map == null) throw new ArgumentNullException("map");
            BinaryWriter writer = new BinaryWriter(stream);
            int metaCount = 0;
            lock (map.Metadata.SyncRoot) {
                foreach (var entry in map.Metadata) {
                    WriteLengthPrefixedString(writer, entry.Group);
                    WriteLengthPrefixedString(writer, entry.Key);
                    WriteLengthPrefixedString(writer, entry.Value);
                    metaCount++;
                }
            }

            Zone[] zoneList = map.Zones.Cache;
            foreach (Zone zone in zoneList) {
                WriteLengthPrefixedString(writer, "zones");
                WriteLengthPrefixedString(writer, zone.Name);
                WriteLengthPrefixedString(writer, SerializeZone(zone));
                metaCount++;
            }
            return metaCount;
        }


        [NotNull]
        static string SerializeZone([NotNull] Zone zone) {
            if (zone == null) throw new ArgumentNullException("zone");
            string xHeader;
            if (zone.CreatedBy != null) {
                xHeader = zone.CreatedBy + " " + zone.CreatedDate.ToCompactString() + " ";
            } else {
                xHeader = "- - ";
            }

            if (zone.EditedBy != null) {
                xHeader += zone.EditedBy + " " + zone.EditedDate.ToCompactString();
            } else {
                xHeader += "- -";
            }

            var zoneExceptions = zone.Controller.ExceptionList;

            string whitelist = zone.RawWhitelist ?? zoneExceptions.Included.JoinToString(" ", p => p.Name);
            string blacklist = zone.RawBlacklist ?? zoneExceptions.Excluded.JoinToString(" ", p => p.Name);

            return String.Format("{0},{1},{2},{3}",
                                 String.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
                                               zone.Name,
                                               zone.Bounds.XMin,
                                               zone.Bounds.YMin,
                                               zone.Bounds.ZMin,
                                               zone.Bounds.XMax,
                                               zone.Bounds.YMax,
                                               zone.Bounds.ZMax,
                                               zone.Controller.MinRank.FullName),
                                 whitelist,
                                 blacklist,
                                 xHeader);
        }
    }
}
