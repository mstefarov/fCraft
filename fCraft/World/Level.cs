using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Net;


namespace fCraft {
    sealed class Level {
        public int widthX, widthY, height;
        public Position spawn;
        public Dictionary<string, string> meta;

        byte[] blocks;
        int blockCount;
        //private byte[][] shadows;
        Queue<BlockUpdate> updates;
        private object dataLock;


        public Level() {
            dataLock = new object();
            updates = new Queue<BlockUpdate>();
        }

        public Level( int _widthX, int _widthY, int _height ) {
            widthX = _widthX;
            widthY = _widthY;
            height = _height;

            blockCount = widthX * widthY * height;

            dataLock = new object();
            updates = new Queue<BlockUpdate>();

            blocks = new byte[blockCount];
            for( int i = 0; i < blocks.Length; i++ ) {
                blocks[i] = 0;
            }
        }


        // ==== Saving ========================================================

        bool Save( string fileName ) {
            string tempFileName = fileName + "." + (new Random().Next().ToString());
            using( FileStream fs = File.OpenWrite( tempFileName ) ) {
                try {
                    WriteHeader( fs );
                    WriteLevel( fs );
                    WriteMetadata( fs );
                } catch( Exception ex ) {
                    Logger.LogError( "Level.Save: Unable to open file \"" + tempFileName + "\" for writing: " + ex.Message );
                    return false;
                }
            }
            File.Delete( fileName );
            File.Move( tempFileName, fileName );
            return true;
        }


        void WriteHeader( FileStream fs ) {
            using( BinaryWriter writer = new BinaryWriter( fs ) ) {
                writer.Write( Config.LevelFormatID );
                writer.Write( (ushort)widthX );
                writer.Write( (ushort)widthY );
                writer.Write( (ushort)height );
                writer.Write( (ushort)spawn.x );
                writer.Write( (ushort)spawn.y );
                writer.Write( (ushort)spawn.h );
                writer.Write( (sbyte)spawn.r );
                writer.Write( (sbyte)spawn.l );
            }
        }


        void WriteMetadata( FileStream fs ) {
            using( BinaryWriter writer = new BinaryWriter( fs ) ) {
                writer.Write( (uint)meta.Count );
                foreach( KeyValuePair<string, string> pair in meta ) {
                    WriteLengthPrefixedString( writer, pair.Key );
                    WriteLengthPrefixedString( writer, pair.Value );
                }
            }
        }


        void WriteLevel( FileStream fs ) {
            using( GZipStream compressor = new GZipStream( fs, CompressionMode.Compress ) ) {
                compressor.Write( blocks, 0, blocks.Length );
            }
        }


        void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
            writer.Write( (uint)stringData.Length );
            writer.Write( stringData );
        }


        // ==== Loading =======================================================

        public static Level Load( string fileName ) {
            FileStream fs = null;
            Level level = null;
            if( !File.Exists( fileName ) ) {
                Logger.LogError( "Level.Load: Specified file does not exist: " + fileName );
                return null;
            }

            try {
                fs = File.OpenRead( fileName );
                if( level.ReadHeader( fs ) ) {
                    level.ReadLevel( fs );
                    level.ReadMetadata( fs );
                    return level;
                } else {
                    return null;
                }
            } catch( EndOfStreamException ) {
                Logger.LogError( "Level.Load: Unexpected end of file - possible corruption!" );
                return null;
            } catch( Exception ex ) {
                Logger.LogError( "Level.Load: Error trying to read from " + fileName + ": " + ex.Message );
                return null;
            } finally {
                if( fs != null ) {
                    fs.Close();
                }
            }
        }


        // Parse the level header
        bool ReadHeader( FileStream fs ) {
            using( BinaryReader reader = new BinaryReader( fs ) ) {
                try {
                    if( reader.ReadUInt32() != Config.LevelFormatID ) {
                        Logger.LogError( "Level.ReadHeader: Incorrect level format id (expected: " + Config.LevelFormatID + ")." );
                        return false;
                    }

                    widthX = reader.ReadUInt16();
                    if( !IsValidDimension( widthX ) ) {
                        Logger.LogError( "Level.ReadHeader: Invalid dimension specified for widthX: " + widthX );
                        return false;
                    }

                    widthY = reader.ReadUInt16();
                    if( !IsValidDimension( widthY ) ) {
                        Logger.LogError( "Level.ReadHeader: Invalid dimension specified for widthY: " + widthX );
                        return false;
                    }

                    height = reader.ReadUInt16();
                    if( !IsValidDimension( height ) ) {
                        Logger.LogError( "Level.ReadHeader: Invalid dimension specified for height: " + widthX );
                        return false;
                    }

                    spawn.x = reader.ReadUInt16();
                    spawn.y = reader.ReadUInt16();
                    spawn.h = reader.ReadUInt16();
                    spawn.r = reader.ReadSByte();
                    spawn.l = reader.ReadSByte();
                    if( spawn.x > widthX || spawn.y > widthY || spawn.h > height ) {
                        Logger.LogAlert( "Level.ReadHeader: Spawn coordinates are outside the valid range!" );
                        spawn.Set( widthX / 2, widthY / 2, height, 0, 0 );
                    }

                } catch( FormatException ex ) {
                    Logger.LogError( "Level.ReadHeader: Cannot parse one or more of the header entries: " + ex.Message );
                    return false;
                }
                return true;
            }
        }

        void ReadMetadata( FileStream fs ) {
            using( BinaryReader reader = new BinaryReader( fs ) ) {
                try {
                    int metaSize = (int)reader.ReadUInt32();
                    meta = new Dictionary<string, string>( metaSize );

                    for( int i = 0; i < metaSize; i++ ) {
                        string key = ReadLengthPrefixedString( reader );
                        string value = ReadLengthPrefixedString( reader );
                        meta.Add( key, value );
                    }
                } catch( FormatException ex ) {
                    Logger.LogError( "Level.ReadHeader: Cannot parse one or more of the header entries: " + ex.Message );
                }
            }
        }

        void ReadLevel( FileStream fs ) {
            blockCount = widthX * widthY * height;
            blocks = new byte[blockCount];
            using( GZipStream compressor = new GZipStream( fs, CompressionMode.Decompress ) ) {
                compressor.Read( blocks, 0, blocks.Length );
            }
        }


        string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = (int)reader.ReadUInt32();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }


        // Only power-of-2 dimensions are allowed
        public static bool IsValidDimension( int dimension ) {
            switch( dimension ) {
                case 2:
                case 4:
                case 8:
                case 16:
                case 32:
                case 64:
                case 128:
                case 256:
                case 512:
                case 1024:
                    return true;
                default:
                    return false;
            }
        }



        // zips a copy of the block array
        public byte[] GetCompressedCopy() {
            byte[] compressedData;

            using( MemoryStream stream = new MemoryStream() ) {
                using( GZipStream compressor = new GZipStream( stream, CompressionMode.Compress ) ) {
                    // convert block count to big-endian
                    int convertedBlockCount = IPAddress.HostToNetworkOrder( blocks.Length );

                    // write block count to gzip stream
                    compressor.Write( BitConverter.GetBytes( convertedBlockCount ), 0, sizeof( int ) );

                    // write out the block data
                    //compressor.Write( blocks, 0, blocks.Length );
                    // hyx hxy
                    for( int h = 0; h < height; h++ ) {
                        for( int y = 0; y < widthY; y++ ) {
                            for( int x = 0; x < widthX; x++ ) {
                                compressor.WriteByte( GetBlock( x, y, h ) );
                            }
                        }
                    }
                }
                compressedData = stream.ToArray();
            }

            return compressedData;
        }


        // ==== Simulation ====================================================

        public int Index( int x, int y, int h ) {
            return (x * widthY + y) * height + h;
        }

        public void SetBlock( int x, int y, int h, byte type ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                blocks[Index( x, y, h )] = type;
        }

        public byte GetBlock( int x, int y, int h ) {
            if( x < widthX && y < widthY && h < height && x >= 0 && y >= 0 && h >= 0 )
                return blocks[Index( x, y, h )];
            else
                return 0;
        }

        public void QueueUpdate( BlockUpdate update ) {
            updates.Enqueue( update );
        }

        public void ProcessUpdates() {
            lock( dataLock ) {
                while( updates.Count > 0 ) {
                    BlockUpdate update = updates.Dequeue();
                    SetBlock( update.x, update.y, update.h, update.type );
                    World.SendToAll( PacketWriter.MakeSetBlock( update.x, update.y, update.h, update.type ), update.origin );
                    if( update.origin != null ) {
                        update.origin.info.ProcessBlockBuild( update.type );
                    }
                }
            }
        }
    }
}