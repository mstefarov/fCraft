using System.IO;
using System.IO.Compression;
using fCraft.MapGeneration;
using fNbt;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    internal class MapCW : IMapExporter {
        public string ServerName {
            get { return "fCraft/CloudBox"; }
        }

        public string FileExtension {
            get { return "cw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.ClassicWorld; }
        }

        public void Save( Map mapToSave, string path ) {
            using( FileStream fs = new FileStream( path, FileMode.Create ) ) {
                using( GZipStream gs = new GZipStream( fs, CompressionMode.Compress ) ) {
                    using( BufferedStream bs = new BufferedStream( gs ) ) {
                        NbtWriter writer = new NbtWriter( bs, "ClassicWorld" );
                        {
                            WriteHeader( mapToSave, path, writer );
                            writer.WriteByteArray( "BlockArray", mapToSave.Blocks );
                            WriteMetadata( mapToSave, writer );
                        }
                        writer.EndCompound();
                        writer.Finish();
                    }
                }
            }
        }

        static void WriteMetadata( [NotNull] Map mapToSave, [NotNull] NbtWriter writer ) {
            writer.BeginCompound( "Metadata" );
            {
                writer.BeginCompound( "fCraft" );
                {
                    string oldEntry = null;
                    foreach( MetadataEntry<string> entry in mapToSave.Metadata ) {
                        if( oldEntry != entry.Group ) {
                            // TODO: Modify MetadataCollection to allow easy iteration group-at-a-time
                            if( oldEntry != null ) writer.EndCompound();
                            oldEntry = entry.Group;
                            writer.BeginCompound( entry.Group );
                        }
                        writer.WriteString( entry.Key, entry.Value );
                    }
                    if( oldEntry != null ) writer.EndCompound();
                }
                writer.EndCompound();
            }
            writer.EndCompound();
        }

        static void WriteHeader( [NotNull] Map mapToSave, [NotNull] string path, [NotNull] NbtWriter writer ) {
            writer.WriteByte( "FormatVersion", 1 );

            // write name and UUID
            World mapWorld = mapToSave.World;
            if( mapWorld != null ) {
                writer.WriteString( "Name", mapWorld.Name );
            } else {
                writer.WriteString( Path.GetFileName( path ) );
            }
            writer.WriteByteArray( "UUID", mapToSave.Guid.ToByteArray() );

            // write map dimensions
            writer.WriteShort( "X", (short)mapToSave.Width );
            writer.WriteShort( "Y", (short)mapToSave.Height );
            writer.WriteShort( "Z", (short)mapToSave.Length );

            // write spawn
            writer.BeginCompound( "Spawn" );
            {
                Position spawn = mapToSave.Spawn;
                writer.WriteShort( "X", spawn.X );
                writer.WriteShort( "Y", spawn.Z );
                writer.WriteShort( "Z", spawn.Y );
                writer.WriteShort( "H", spawn.R );
                writer.WriteShort( "P", spawn.L );
            }
            writer.EndCompound();

            // write timestamps
            writer.WriteLong( "TimeCreated", mapToSave.DateCreated.ToUnixTime() );
            // TODO: TimeAccessed
            writer.WriteLong( "LastModified", mapToSave.DateCreated.ToUnixTime() );

            // TODO: write CreatedBy

            // Write map origin information
            writer.BeginCompound( "MapGenerator" );
            {
                writer.WriteString( "Software", "fCraft " + Updater.CurrentRelease.VersionString );
                string genName;
                if( !mapToSave.Metadata.TryGetValue( MapGenUtil.ParamsMetaGroup,
                                                     MapGenUtil.GenNameMetaKey,
                                                     out genName ) ) {
                    genName = "Unknown";
                }
                writer.WriteString( "MapGeneratorName", genName );
            }
            writer.EndCompound();
        }
    }
}
