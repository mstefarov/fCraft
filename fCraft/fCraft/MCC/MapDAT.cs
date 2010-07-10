// 
//  Author:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from
//       this software without specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.IO;
using System.IO.Compression;
using fCraft;


namespace mcc {
    public sealed class MapDAT : IConverter {
        public MapFormats Format {
            get { return MapFormats.Creative; }
        }

        public string FileExtension {
            get { return ".dat"; }
        }

        public string ServerName {
            get { return "vanilla"; }
        }


        public Map Load( Stream MapStream ) {
            byte[] temp = new byte[8];
            Map map = new Map();
            byte[] data;
            int length;
            try {
                MapStream.Seek( -4, SeekOrigin.End );
                MapStream.Read( temp, 0, sizeof( int ) );
                MapStream.Seek( 0, SeekOrigin.Begin );
                length = BitConverter.ToInt32( temp, 0 );
                data = new byte[length];
                using( GZipStream reader = new GZipStream( MapStream, CompressionMode.Decompress, true ) ) {
                    reader.Read( data, 0, length );
                }

                for( int i = 0; i < length - 1; i++ ) {
                    if( data[i] == 0xAC && data[i + 1] == 0xED ) {

                        // bypassing the header crap
                        int pointer = i + 6;
                        Array.Copy( data, pointer, temp, 0, sizeof( short ) );
                        pointer += Server.SwapBytes( BitConverter.ToInt16( temp, 0 ) );
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
                            short skip = Server.SwapBytes( BitConverter.ToInt16( temp, 0 ) );
                            pointer += 2;

                            // look for relevant variables
                            Array.Copy( data, headerEnd + offset - 4, temp, 0, sizeof( int ) );
                            if( MemCmp( data, pointer, "width" ) ) {
                                map.widthX = (ushort)Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) );
                            } else if( MemCmp( data, pointer, "depth" ) ) {
                                map.height = (ushort)Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) );
                            } else if( MemCmp( data, pointer, "height" ) ) {
                                map.widthY = (ushort)Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) );
                            } else if( MemCmp( data, pointer, "xSpawn" ) ) {
                                map.spawn.x = (short)(Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                            } else if( MemCmp( data, pointer, "ySpawn" ) ) {
                                map.spawn.h = (short)(Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
                            } else if( MemCmp( data, pointer, "zSpawn" ) ) {
                                map.spawn.y = (short)(Server.SwapBytes( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16);
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
                            if( !map.ValidateBlockTypes( true ) ) {
                                throw new Exception( "Map validation failed: unknown block types found. Either parsing has done wrong, or this is an incompatible format." );
                            }
                        } else {
                            throw new Exception( "Could not locate block array." );
                        }
                        break;
                    }
                }

            } catch( Exception ex ) {
                Logger.Log( "Conversion failed: {0}", LogType.Error, ex.Message );
                Logger.Log( ex.StackTrace, LogType.Debug );
                return null;
            }

            return map;
        }


        public bool Save( Map MapToSave, Stream MapStream ) {
            throw new NotImplementedException();
        }


        public bool Claims( Stream MapStream ) {
            byte[] temp = new byte[8];
            byte[] data;
            int length;
            try {
                MapStream.Seek( -4, SeekOrigin.End );
                MapStream.Read( temp, 0, sizeof( int ) );
                MapStream.Seek( 0, SeekOrigin.Begin );
                length = BitConverter.ToInt32( temp, 0 );
                data = new byte[length];
                using( GZipStream reader = new GZipStream( MapStream, CompressionMode.Decompress, true ) ) {
                    reader.Read( data, 0, length );
                }

                for( int i = 0; i < length - 1; i++ ) {
                    if( data[i] == 0xAC && data[i + 1] == 0xED ) {
                        return true;
                    }
                }
                return false;

            } catch( Exception ) {
                return false;
            }
        }


        static bool MemCmp( byte[] data, int offset, string value ) {
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Length || data[offset + i] != value[i] ) return false;
            }
            return true;
        }
    }
}
