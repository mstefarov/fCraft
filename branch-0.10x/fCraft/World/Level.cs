using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace fCraft {
    sealed class Level {
        uint widthX, widthY, height;
        byte[][][] blocks;
        byte[][] shadows;
        Dictionary<string, string> meta;

        #region Saving
        bool Save( string fileName ) {
            string tempFileName = fileName + "." + ( new Random().Next().ToString() );
            FileStream fs = null;
            try {
                fs = File.OpenWrite( fileName );
                WriteHeader( fs );
                WriteLevel( fs );
            } catch( Exception ex ) {
                Logger.LogError( "Level.Save: Unable to open file \"" + tempFileName + "\" for writing: " + ex.Message );
                return false;
            } finally {
                if( fs != null ) {
                    fs.Close();
                }
            }
            File.Move( tempFileName, fileName );
            return true;
        }

        void WriteHeader( FileStream fs ) {
            BinaryWriter writer = new BinaryWriter( fs );
            writer.Write( Config.LevelFormatID );
            writer.Write( widthX );
            writer.Write( widthY );
            writer.Write( height );

            // write metadata
            writer.Write( (uint)meta.Count );
            foreach( KeyValuePair<string, string> pair in meta ) {
                WriteLengthPrefixedString( writer, pair.Key );
                WriteLengthPrefixedString( writer, pair.Value );
            }
        }

        void WriteLevel( FileStream fs ) {
            GZipStream zs = new GZipStream( fs, CompressionMode.Compress );
            for( int x = 0; x < widthX; x++ ) {
                for( int y = 0; y < widthY; y++ ) {
                    zs.Write( blocks[x][y], 0, (int)height );
                }
            }
        }

        void WriteLengthPrefixedString( BinaryWriter writer, string s ) {
            byte[] stringData = ASCIIEncoding.ASCII.GetBytes( s );
            writer.Write( (uint)stringData.Length );
            writer.Write( stringData );
        }

        #endregion

        #region Loading

        public static Level Load( string fileName ) {
            FileStream fs = null;
            Level level = null;
            if( !File.Exists(fileName)){
                Logger.LogError("Level.Load: Specified file does not exist: "+fileName);
                return null;
            }

            try {
                fs = File.OpenRead( fileName );
                if( level.ReadHeader( fs ) ) {
                    level.ReadLevel( fs );
                    return level;
                } else {
                    return null;
                }
            } catch( Exception) {
                Logger.LogError("Level.Load: Error trying to read from file: "+fileName);
                return null;
            } finally {
                if( fs != null ) {
                    fs.Close();
                }
            }
        }

        // read the ascii-encoded header message
        bool ReadHeader( FileStream fs ) {
            BinaryReader reader = new BinaryReader( fs );

            try {
                if( reader.ReadUInt32() != Config.LevelFormatID ) {
                    Logger.LogError( "Level.ReadHeader: Incorrect level format id (expected: " + Config.LevelFormatID + ")." );
                    return false;
                }

                widthX = reader.ReadUInt32();
                if( !IsValidDimension( widthX ) ) {
                    Logger.LogError( "Level.ReadHeader: Invalid dimension specified for widthX: " + widthX );
                    return false;
                }

                widthY = reader.ReadUInt32();
                if( !IsValidDimension( widthY ) ) {
                    Logger.LogError( "Level.ReadHeader: Invalid dimension specified for widthY: " + widthX );
                    return false;
                }

                height = reader.ReadUInt32();
                if( !IsValidDimension( height ) ) {
                    Logger.LogError( "Level.ReadHeader: Invalid dimension specified for height: " + widthX );
                    return false;
                }

                int metaSize = (int)reader.ReadUInt32();
                meta = new Dictionary<string, string>( metaSize );

                for( int i = 0; i < metaSize; i++ ) {
                    string key = ReadLengthPrefixedString( reader );
                    string value = ReadLengthPrefixedString( reader );
                    meta.Add( key, value );
                }

            } catch( FormatException ex ) {
                Logger.LogError( "Level.ReadHeader: Cannot parse one or more of the header entries: " + ex.Message );
                return false;
            } catch( EndOfStreamException ex ) {
            }
            return true;
        }

        void ReadLevel( FileStream fs ) {
            blocks = new byte[widthX][][];
            GZipStream zip = new GZipStream( fs, CompressionMode.Decompress );
            for( int x = 0; x < widthX; x++ ) {
                blocks[x] = new byte[widthY][];
                for( int y = 0; y < widthY; y++ ) {
                    blocks[x][y] = new byte[height];
                    zip.Read( blocks[x][y], 0, (int)height );
                }
            }
        }

        string ReadLengthPrefixedString( BinaryReader reader ) {
            int length = (int)reader.ReadUInt32();
            byte[] stringData = reader.ReadBytes( length );
            return ASCIIEncoding.ASCII.GetString( stringData );
        }

        // Only power-of-2 dimensions are allowed
        public static bool IsValidDimension( uint dimension ) {
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
        #endregion
    }
}