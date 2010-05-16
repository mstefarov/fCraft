// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;


namespace fCraft {
    // encapsulates 
    public static class MapLoaderDAT {
        public static Map Load( World world, string fileName ) {
            Logger.Log( "Converting {0}...", LogType.SystemActivity, fileName );
            byte[] temp = new byte[8];
            Map map = new Map( world );
            byte[] data;
            int length;
            try {
                using( FileStream stream = File.OpenRead( fileName ) ) {
                    stream.Seek( -4, SeekOrigin.End );
                    stream.Read( temp, 0, sizeof( int ) );
                    stream.Seek( 0, SeekOrigin.Begin );
                    length = BitConverter.ToInt32( temp, 0 );
                    data = new byte[length];
                    using( GZipStream reader = new GZipStream( stream, CompressionMode.Decompress ) ) {
                        reader.Read( data, 0, length );
                    }
                }

                //if( data[0] == 0xBE && data[1] == 0xEE && data[2] == 0xEF ) {
                    for( int i = 0; i < length - 1; i++ ) {
                        if( data[i] == 0xAC && data[i + 1] == 0xED ) {

                            // bypassing the header crap
                            int pointer = i + 6;
                            Array.Copy( data, pointer, temp, 0, sizeof( short ) );
                            pointer += Server.htons( BitConverter.ToInt16( temp, 0 ) );
                            pointer += 13;

                            int headerEnd = 0;
                            // find the end of serialization listing
                            for( headerEnd = pointer; headerEnd < data.Length - 1; headerEnd++ ) {
                                if( data[headerEnd] == 0x78 && data[headerEnd + 1] == 0x70 ) {
                                    headerEnd += 2;
                                    break;
                                }
                            }

                            // start parsing serialization listing
                            int offset = 0;
                            while( pointer < headerEnd ) {
                                if( data[pointer] == 'Z' ) offset++;
                                else if( data[pointer] == 'I' || data[pointer] == 'F' ) offset += 4;
                                else if( data[pointer] == 'J' ) offset += 8;

                                pointer += 1;
                                Array.Copy( data, pointer, temp, 0, sizeof( short ) );
                                short skip = Server.htons( BitConverter.ToInt16( temp, 0 ) );
                                pointer += 2;

                                // look for relevant variables
                                Array.Copy( data, headerEnd + offset - 4, temp, 0, sizeof( int ) );
                                if( MemCmp( data, pointer, "width" ) ) {
                                    map.widthX = (ushort)Server.htons( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "depth" ) ) {
                                    map.height = (ushort)Server.htons( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "height" ) ) {
                                    map.widthY = (ushort)Server.htons( BitConverter.ToInt32( temp, 0 ) );
                                } else if( MemCmp( data, pointer, "xSpawn" ) ) {
                                    map.spawn.x = (short)(Server.htons( BitConverter.ToInt32( temp, 0 ) )*32+16);
                                } else if( MemCmp( data, pointer, "ySpawn" ) ) {
                                    map.spawn.h = (short)(Server.htons( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                                } else if( MemCmp( data, pointer, "zSpawn" ) ) {
                                    map.spawn.y = (short)(Server.htons( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                                }

                                pointer += skip;
                            }

                            // find the start of the block array
                            bool foundBlockArray = false;
                            offset = Array.IndexOf<byte>( data, 0x00, headerEnd );
                            while( offset != -1 && offset < data.Length - 2 ) {
                                if( data[offset] == 0x00 && data[offset + 1] == 0x78 && data[offset + 2] == 0x70 ) {
                                    foundBlockArray = true;
                                    pointer = offset + 7;
                                }
                                offset = Array.IndexOf<byte>( data, 0x00, offset + 1 );
                            }

                            // copy the block array... or fail
                            if( foundBlockArray ) {
                                map.CopyBlocks( data, pointer );
                                if( !map.ValidateBlockTypes() ) {
                                    throw new Exception( "Map validation failed: unknown block types found. Either parsing has done wrong, or this is an incompatible format." );
                                }
                            } else {
                                throw new Exception( "Could not locate block array." );
                            }
                            break;
                        }
                    //}
                }
            } catch( Exception ex ) {
                Logger.Log( "Conversion failed: {0}", LogType.Error, ex.Message );
                Logger.Log( ex.StackTrace, LogType.Debug );
                return null;
            }
            //map.Save();
            Logger.Log( "Conversion completed succesfully succesful.", LogType.SystemActivity, fileName );
            return map;
        }

        static bool MemCmp( byte[] data, int offset, string value ) {
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Length || data[offset + i] != value[i] ) return false;
            }
            return true;
        }
    }
}
