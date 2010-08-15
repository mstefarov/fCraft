// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010, Tyler Kennedy & Matvei Stefarov
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
    public sealed class MapMyne : IConverter {

        public MapFormat Format {
            get { return MapFormat.JTE; }
        }

        public string FileExtension {
            get { return ".gz"; }
        }

        public string ServerName {
            get { return "Myne"; }
        }

        public MapMyne() {
        }

        public Map Load( Stream mapStream ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            mapStream.Seek( 0, SeekOrigin.Begin );

            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            Map map = new Map();

            bs.ReadByte(); // version, either 1 or 2

            // Read in the spawn location
            map.spawn.x = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);
            map.spawn.h = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);
            map.spawn.y = (short)(IPAddress.NetworkToHostOrder( bs.ReadInt16() ) * 32);

            // Read in the spawn orientation
            map.spawn.r = bs.ReadByte();
            map.spawn.l = bs.ReadByte();

            // Read in the map dimesions
            map.widthX = IPAddress.NetworkToHostOrder(bs.ReadInt16());
            map.widthY = IPAddress.NetworkToHostOrder(bs.ReadInt16());
            map.height = IPAddress.NetworkToHostOrder(bs.ReadInt16());

            if( !map.ValidateHeader() ) {
                throw new Exception( "One or more of the map dimensions are invalid." );
            }

            // Read in the map data
            map.blocks = bs.ReadBytes( map.GetBlockCount() );

            return map;
        }


        public bool Save( Map mapToSave, Stream mapStream ) {
            using ( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress, true ) ) {
                BinaryWriter bs = new BinaryWriter( gs );

                // Write the magic number
                bs.Write( (byte)0x01 );

                // Write the spawn location
                bs.Write( IPAddress.NetworkToHostOrder((short)(mapToSave.spawn.x/32)) );
                bs.Write( IPAddress.NetworkToHostOrder((short)(mapToSave.spawn.h/32)) );
                bs.Write( IPAddress.NetworkToHostOrder((short)(mapToSave.spawn.y/32)) );

                //Write the spawn orientation
                bs.Write( mapToSave.spawn.r );
                bs.Write( mapToSave.spawn.l );

                // Write the map dimensions
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.widthX ) );
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.widthY ) );
                bs.Write( IPAddress.NetworkToHostOrder( mapToSave.height ) );

                // Write the map data
                bs.Write( mapToSave.blocks, 0, mapToSave.blocks.Length );

                bs.Close();
            }
            return true;
        }


        public bool Claims( Stream mapStream ) {
            mapStream.Seek( 0, SeekOrigin.Begin );
            try {
                GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress, true );
                BinaryReader bs = new BinaryReader( gs );
                byte version = bs.ReadByte();
                return (version == 1 || version == 2);
            } catch( Exception ) {
                return false;
            }
        }

    }
}