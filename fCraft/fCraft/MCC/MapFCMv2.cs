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

namespace mcc {
    public class MapFCMv2 : IConverter {

        const uint identifier = 0xfc000002u;

        public MapFormats Format {
            get {
                return MapFormats.FCMv2;
            }
        }

        public string[] UsedBy {
            get {
                return new string[] { "fcraft" };
            }
        }

        public Map Load( Stream MapStream ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            MapStream.Seek( 0, SeekOrigin.Begin );
            
            Map map = new Map(  );
            
            BinaryReader reader = new BinaryReader( MapStream );
            
            // Read in the magic number
            if( reader.ReadUInt32(  ) != identifier ) {
                throw new System.FormatException(  );
            }
            
            // Read in the map dimesions
            map.Width = reader.ReadUInt16(  );
            map.Depth = reader.ReadUInt16(  );
            map.Height = reader.ReadUInt16(  );
            
            // Read in the spawn location
            map.SpawnX = (ushort)( reader.ReadUInt16(  ) / 32 );
            map.SpawnZ = (ushort)( reader.ReadUInt16(  ) / 32 );
            map.SpawnY = (ushort)( reader.ReadUInt16(  ) / 32 );
            
            // Read in the spawn orientation
            map.SpawnRotation = reader.ReadByte(  );
            map.SpawnPitch = reader.ReadByte(  );
            
            // Skip over the metadata
            int metadataStringCount = reader.ReadUInt16(  ) * 2;
            for( int i = 0; i < metadataStringCount; i++ ) {
                reader.ReadBytes( reader.ReadInt32(  ) );
            }
            
            // Read in the map data
            // Write the map data
            map.MapData = new Byte[map.BlockCount];
            using( GZipStream decompressor = new GZipStream( MapStream, CompressionMode.Decompress, true ) ) {
                decompressor.Read( map.MapData, 0, map.BlockCount );
            }
            
            return map;
        }

        public bool Save( Map MapToSave, Stream MapStream ) {
            BinaryWriter bs = new BinaryWriter( MapStream );
            
            // Write the magic number
            bs.Write( identifier );
            
            // Write the map dimensions
            bs.Write( MapToSave.Width );
            bs.Write( MapToSave.Depth );
            bs.Write( MapToSave.Height );
            
            // Write the spawn location
            bs.Write( (ushort)( MapToSave.SpawnX * 32 ) );
            bs.Write( (ushort)( MapToSave.SpawnZ * 32 ) );
            bs.Write( (ushort)( MapToSave.SpawnY * 32 ) );
            
            // Write the spawn orientation
            bs.Write( MapToSave.SpawnRotation );
            bs.Write( MapToSave.SpawnPitch );
            
            // Skip metadata pair count
            // TODO: if any metadata is to be preserved, alter this
            bs.Write( (ushort)0 );
            
            // Write the map data
            using( GZipStream gs = new GZipStream( MapStream, CompressionMode.Compress, true ) ) {
                gs.Write( MapToSave.MapData, 0, MapToSave.BlockCount );
            }
            
            bs.Close(  );
            
            return true;
        }

        public bool Claims( Stream MapStream ) {
            MapStream.Seek( 0, SeekOrigin.Begin );
            
            BinaryReader reader = new BinaryReader( MapStream );
            
            try {
                return reader.ReadUInt32(  ) == identifier;
            } catch( IOException ) {
                return false;
            }
            
        }
    }
}
