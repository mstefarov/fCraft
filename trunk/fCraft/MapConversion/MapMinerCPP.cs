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

namespace fCraft.MapConversion {
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
                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                    return LoadHeaderInternal( gs );
                }
            }
        }


        static Map LoadHeaderInternal( Stream stream ) {
            BinaryReader bs = new BinaryReader( stream );

            // Read in the magic number
            if( bs.ReadByte() != 0xbe || bs.ReadByte() != 0xee || bs.ReadByte() != 0xef ) {
                throw new MapFormatException( "MinerCPP map header is incorrect." );
            }

            // Read in the map dimesions
            // Saved in big endian for who-know-what reason.
            // XYZ(?)
            int widthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            int widthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

            Map map = new Map( null, widthX, widthY, height, false );

            // Read in the spawn location
            // XYZ(?)
            map.Spawn.X = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            map.Spawn.H = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
            map.Spawn.Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

            // Read in the spawn orientation
            map.Spawn.R = bs.ReadByte();
            map.Spawn.L = bs.ReadByte();

            // Skip over the block count, totally useless
            bs.ReadInt32();

            return map;
        }


        public Map Load( string fileName ) {
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                // Setup a GZipStream to decompress and read the map file
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {

                    Map map = LoadHeaderInternal( gs );

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "One or more of the map dimensions are invalid." );
                    }

                    // Read in the map data
                    map.Blocks = new byte[map.WidthX * map.WidthY * map.Height];
                    mapStream.Read( map.Blocks, 0, map.Blocks.Length );

                    return map;
                }
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
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.WidthX ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.Height ) );
                    bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)mapToSave.WidthY ) );

                    // Save the spawn location
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.X ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.H ) );
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Spawn.Y ) );

                    // Save the spawn orientation
                    bs.Write( mapToSave.Spawn.R );
                    bs.Write( mapToSave.Spawn.L );

                    // Write out the block count (which is totally useless, can't stress that enough.)
                    bs.Write( IPAddress.HostToNetworkOrder( mapToSave.Blocks.Length ) );

                    // Write out the map data
                    bs.Write( mapToSave.Blocks );
                    return true;
                }
            }
        }
    }
}