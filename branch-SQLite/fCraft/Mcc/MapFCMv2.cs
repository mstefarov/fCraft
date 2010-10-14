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
using fCraft;

namespace Mcc {
    public sealed class MapFCMv2 : IConverter {
        [CLSCompliantAttribute( false )]
        public const uint Identifier = 0xfc000002;

        public MapFormat Format {
            get { return MapFormat.FCMv2; }
        }

        public string FileExtension {
            get { return ".fcm"; }
        }

        public string ServerName {
            get { return "fCraft"; }
        }

        public Map Load( Stream mapStream, string fileName ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            mapStream.Seek( 0, SeekOrigin.Begin );

            Map map = new Map();

            BinaryReader reader = new BinaryReader( mapStream );

            // Read in the magic number
            if( reader.ReadUInt32() != Identifier ) {
                throw new FormatException();
            }

            // Read in the map dimesions
            map.widthX = reader.ReadInt16();
            map.widthY = reader.ReadInt16();
            map.height = reader.ReadInt16();

            // Read in the spawn location
            map.spawn.x = reader.ReadInt16();
            map.spawn.y = reader.ReadInt16();
            map.spawn.h = reader.ReadInt16();

            // Read in the spawn orientation
            map.spawn.r = reader.ReadByte();
            map.spawn.l = reader.ReadByte();

            // Read the metadata
            map.ReadMetadata( reader );

            if( !map.ValidateHeader() ) {
                throw new Exception( "One or more of the map dimensions are invalid." );
            }

            // Read in the map data
            map.blocks = new Byte[map.GetBlockCount()];
            using( ZLibStream decompressor = ZLibStream.MakeDecompressor( mapStream, ZLibStream.BufferSize ) ) {
                decompressor.Read( map.blocks, 0, map.blocks.Length );
            }

            return map;
        }


        public bool Save( Map mapToSave, Stream mapStream ) {
            BinaryWriter bs = new BinaryWriter( mapStream );

            // Write the magic number
            bs.Write( Identifier );

            // Write the map dimensions
            bs.Write( mapToSave.widthX );
            bs.Write( mapToSave.widthY );
            bs.Write( mapToSave.height );

            // Write the spawn location
            bs.Write( mapToSave.spawn.x );
            bs.Write( mapToSave.spawn.y );
            bs.Write( mapToSave.spawn.h );

            // Write the spawn orientation
            bs.Write( mapToSave.spawn.r );
            bs.Write( mapToSave.spawn.l );

            // Skip metadata pair count
            mapToSave.WriteMetadata( bs );

            // Write the map data
            using ( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress, true ) ) {
                gs.Write( mapToSave.blocks, 0, mapToSave.blocks.Length );
            }

            bs.Close();

            return true;
        }


        public bool Claims( Stream mapStream, string fileName ) {
            try {
                mapStream.Seek( 0, SeekOrigin.Begin );
                BinaryReader reader = new BinaryReader( mapStream );
                return reader.ReadUInt32() == Identifier;
            } catch ( Exception ) {
                return false;
            }

        }
    }
}