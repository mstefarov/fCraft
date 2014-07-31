using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    public class MapD3Folder : IMapImporter {
        const string DataFileName = "Data-Layer.gz";
        const string ConfigFileName = "Config.txt";

        public string ServerName { get; private set; }
        public MapStorageType StorageType { get; private set; }
        public MapFormat Format { get; private set; }

        public string FileExtension {
            get { throw new NotSupportedException(); }
        }


        public MapD3Folder() {
            ServerName = "D3";
            StorageType = MapStorageType.Directory;
            Format = MapFormat.D3Folder;
        }


        public bool ClaimsName(string path) {
            if( path == null ) throw new ArgumentNullException("path");
            return Directory.Exists(path) &&
                   File.Exists(Path.Combine(path, DataFileName)) &&
                   File.Exists(Path.Combine(path, ConfigFileName));
        }


        public bool Claims(string path) {
            return ClaimsName(path);
        }


        public Map LoadHeader(string path) {
            if( path == null ) throw new ArgumentNullException("path");
            using( FileStream fs = File.OpenRead(Path.Combine(path, ConfigFileName)) ) {
                var config = ReadConfigFile(fs);
                int width = Int32.Parse(config["Size_X"]);
                int length = Int32.Parse(config["Size_Y"]);
                int height = Int32.Parse(config["Size_Z"]);
                return new Map(null, width, length, height, false) {
                    Spawn = new Position(ToPositionCoord(config["Spawn_X"]),
                                         ToPositionCoord(config["Spawn_Y"]),
                                         ToPositionCoord(config["Spawn_Z"]),
                                         ToPositionAngle(config["Spawn_Rot"]),
                                         ToPositionAngle(config["Spawn_Look"]))
                };
            }
        }


        public Map Load(string path) {
            Map map = LoadHeader(path);
            using( FileStream fs = File.OpenRead(Path.Combine(path, DataFileName)) ) {
                using( GZipStream gs = new GZipStream(fs, CompressionMode.Decompress) ) {
                    // Read in the map data
                    byte[] buffer = new byte[4];
                    map.Blocks = new byte[map.Volume];
                    for( int i = 0; i < map.Volume; i++ ) {
                        gs.Read(buffer, 0, 4);
                        map.Blocks[i] = buffer[0];
                    }
                    map.ConvertBlockTypes(MapD3.Mapping);
                }
            }
            return map;
        }


        static byte ToPositionAngle(string str) {
            int binDegrees = (int)Math.Round(Double.Parse(str)/360*256);
            return (byte)(binDegrees < 0 ? (256 + binDegrees) : binDegrees);
        }


        static short ToPositionCoord(string str) {
            return (short)(Double.Parse(str)*32);
        }


        static Dictionary<string, string> ReadConfigFile([NotNull] Stream stream) {
            if( stream == null ) throw new ArgumentNullException("stream");
            Dictionary<string, string> contents = new Dictionary<string, string>();
            StreamReader reader = new StreamReader(stream);
            while( true ) {
                string line = reader.ReadLine();
                if( line == null ) break;

                // Skip comments and blank lines
                if( line.Length == 0 || line[0] == ';' ) continue;

                int separatorIdx = line.IndexOf('=');
                // Skip lines without '='
                if( separatorIdx < 0 ) continue;
                string key = line.Substring(0, separatorIdx).Trim();
                string value = line.Substring(separatorIdx + 1).Trim();
                contents[key] = value;
            }
            return contents;
        }
    }
}
