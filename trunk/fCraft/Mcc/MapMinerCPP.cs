// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010-2011, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this
//       list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of MCC nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
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
using System.Net;
using fCraft;


namespace Mcc {
    public sealed class MapMinerCPP : IMapConverter {

        public string ServerName {
            get { return "MinerCPP/LuaCraft"; }
        }


        public MapFormatType FormatType {
            get { return MapFormatType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.MinerCPP; }
        }


        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".dat", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( string fileName ) {
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        return (bs.ReadByte() == 0xbe && bs.ReadByte() == 0xee && bs.ReadByte() == 0xef);
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                Map map = new Map();

                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                    BinaryReader bs = new BinaryReader( gs );

                    // Read in the magic number
                    if( bs.ReadByte() != 0xbe || bs.ReadByte() != 0xee || bs.ReadByte() != 0xef ) {
                        throw new FormatException( "MinerCPP map header is incorrect." );
                    }

                    // Read in the map dimesions
                    // Saved in big endian for who-know-what reason.
                    // XYZ(?)
                    map.widthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.widthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    return map;
                }
            }
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                Map map = new Map();

                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                    BinaryReader bs = new BinaryReader( gs );

                    // Read in the magic number
                    if( bs.ReadByte() != 0xbe || bs.ReadByte() != 0xee || bs.ReadByte() != 0xef ) {
                        throw new FormatException( "MinerCPP map header is incorrect." );
                    }

                    // Read in the map dimesions
                    // Saved in big endian for who-know-what reason.
                    // XYZ(?)
                    map.widthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.widthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "MapFCMv3.Load: One or more of the map dimensions are invalid." );
                    }

                    // Read in the spawn location
                    // XYZ(?)
                    map.spawn.x = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.spawn.h = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                    map.spawn.y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                    // Read in the spawn orientation
                    map.spawn.r = bs.ReadByte();
                    map.spawn.l = bs.ReadByte();

                    // Skip over the block count, totally useless
                    bs.ReadInt32();

                    // Read in the map data
                    map.blocks = bs.ReadBytes( map.GetBlockCount() );
                }

                return map;
            }
        }


        public bool Save( Map mapToSave, string fileName ) {
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write out the magic number
                    bs.Write( new byte[] { 0xbe, 0xee, 0xef } );

                    // Save the map dimensions
                    // XYZ(?)
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.widthX ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.height ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.widthY ) );

                    // Save the spawn location
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.spawn.x ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.spawn.h ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.spawn.y ) );

                    // Save the spawn orientation
                    bs.Write( mapToSave.spawn.r );
                    bs.Write( mapToSave.spawn.l );

                    // Write out the block count (which is totally useless, can't stress that enough.)
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.blocks.Length ) );

                    // Write out the map data
                    bs.Write( mapToSave.blocks );
                    return true;
                }
            }
        }
    }
}