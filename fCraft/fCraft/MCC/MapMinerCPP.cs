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
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
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


namespace mcc {
    public sealed class MapMinerCPP : IConverter {

        public MapFormats Format {
            get { return MapFormats.MinerCPP; }
        }

        public string FileExtension {
            get { return ".dat"; }
        }

        public string ServerName {
            get { return "MinerCPP/LuaCraft"; }
        }


        public Map Load( Stream MapStream ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            MapStream.Seek( 0, SeekOrigin.Begin );

            Map m = new Map();

            // Setup a GZipStream to decompress and read the map file
            using ( GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true ) ) {
                BinaryReader bs = new BinaryReader( gs );

                // Read in the magic number
                if ( bs.ReadByte() != 0xbe || bs.ReadByte() != 0xee || bs.ReadByte() != 0xef ) {
                    throw new FormatException( "MinerCPP map header is incorrect." );
                }

                // Read in the map dimesions
                // Saved in big endian for who-know-what reason.
                // XYZ(?)
                m.widthX = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                m.height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                m.widthY = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                // Read in the spawn location
                // XYZ(?)
                m.spawn.x = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                m.spawn.h = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                m.spawn.y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                // Read in the spawn orientation
                m.spawn.r = bs.ReadByte();
                m.spawn.l = bs.ReadByte();

                // Skip over the block count, totally useless
                bs.ReadInt32();

                // Read in the map data
                m.blocks = bs.ReadBytes( m.GetBlockCount() );
                if ( !m.ValidateBlockTypes( true ) ) {
                    throw new Exception( "Unrecognized block types in the map." );
                }
            }

            return m;
        }


        public bool Save( Map MapToSave, System.IO.Stream MapStream ) {
            // Todo: Implement saving
            using ( GZipStream gs = new GZipStream( MapStream, CompressionMode.Compress, true ) ) {
                BinaryWriter bs = new BinaryWriter( gs );

                // Write out the magic number
                bs.Write( new byte[] { 0xbe, 0xee, 0xef } );

                // Save the map dimensions
                // XYZ(?)
                bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)MapToSave.widthX ) );
                bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)MapToSave.height ) );
                bs.Write( (ushort)IPAddress.HostToNetworkOrder( (short)MapToSave.widthY ) );

                // Save the spawn location
                bs.Write( IPAddress.HostToNetworkOrder( MapToSave.spawn.x ) );
                bs.Write( IPAddress.HostToNetworkOrder( MapToSave.spawn.h ) );
                bs.Write( IPAddress.HostToNetworkOrder( MapToSave.spawn.y ) );

                // Save the spawn orientation
                bs.Write( MapToSave.spawn.r );
                bs.Write( MapToSave.spawn.l );

                // Write out the block count (which is totally useless, can't stress that enough.)
                bs.Write( IPAddress.HostToNetworkOrder( MapToSave.blocks.Length ) );

                // Write out the map data
                bs.Write( MapToSave.blocks );

                // Make sure the output gets flushed, fixes a bug in mono where the destructor doesn't flush
                // on its own
                bs.Close();
            }

            return true;
        }


        public bool Claims( Stream MapStream ) {
            MapStream.Seek( 0, SeekOrigin.Begin );

            GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            try {
                if ( bs.ReadByte() == 0xbe && bs.ReadByte() == 0xee && bs.ReadByte() == 0xef ) {
                    return true;
                }
            } catch ( IOException ) {
                return false;
            } catch ( InvalidDataException ) {
                return false;
            }

            return false;

        }
    }
}