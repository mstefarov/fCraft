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
    public class MapMCSharp : IConverter {

        public MapFormats Format {
            get {
                return MapFormats.MCSharp;
            }
        }

        public string[] UsedBy {
            get {
                return new string[] { "mcsharp" };
            }
        }

        public Map Load( System.IO.Stream MapStream ) {
            // Reset the seeker to the front of the stream
            // This should probably be done differently.
            MapStream.Seek( 0, SeekOrigin.Begin );
            
            // Setup a GZipStream to decompress and read the map file
            GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );
            
            Map m = new Map(  );
            
            // Read in the magic number
            if( bs.ReadUInt16(  ) != 0x752 ) {
                throw new System.FormatException(  );
            }
            
            // Read in the map dimesions
            m.Width = bs.ReadUInt16(  );
            m.Depth = bs.ReadUInt16(  );
            m.Height = bs.ReadUInt16(  );
            
            // Read in the spawn location
            m.SpawnX = bs.ReadUInt16(  );
            m.SpawnZ = bs.ReadUInt16(  );
            m.SpawnY = bs.ReadUInt16(  );
            
            // Read in the spawn orientation
            m.SpawnRotation = bs.ReadByte(  );
            m.SpawnPitch = bs.ReadByte(  );
            
            // Skip over the VisitPermission and BuildPermission bytes
            bs.ReadByte(  );
            bs.ReadByte(  );
            
            // Read in the map data
            m.MapData = new Byte[m.BlockCount];
            m.MapData = bs.ReadBytes( m.BlockCount );
            
            return m;
        }

        public bool Save( Map MapToSave, System.IO.Stream MapStream ) {
            using( GZipStream gs = new GZipStream( MapStream, CompressionMode.Compress, true ) ) {
                BinaryWriter bs = new BinaryWriter( gs );
                
                // Write the magic number
                bs.Write( (ushort)0x752 );
                
                // Write the map dimensions
                bs.Write( MapToSave.Width );
                bs.Write( MapToSave.Depth );
                bs.Write( MapToSave.Height );
                
                // Write the spawn location
                bs.Write( MapToSave.SpawnX );
                bs.Write( MapToSave.SpawnZ );
                bs.Write( MapToSave.SpawnY );
                
                //Write the spawn orientation
                bs.Write( MapToSave.SpawnRotation );
                bs.Write( MapToSave.SpawnPitch );
                
                // Write the VistPermission and BuildPermission bytes
                bs.Write( (byte)0 );
                bs.Write( (byte)0 );
                
                // Write the map data
                bs.Write( MapToSave.MapData, 0, MapToSave.BlockCount );
                
                bs.Close(  );
            }
            return true;
            
        }

        public bool Claims( System.IO.Stream MapStream ) {
            MapStream.Seek( 0, SeekOrigin.Begin );
            
            GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );
            
            try {
                if( bs.ReadUInt16(  ) == 0x752 ) {
                    return true;
                }
            } catch( IOException ) {
                return false;
            } catch( InvalidDataException ) {
                return false;
            }
            
            return false;
            
        }
        
    }
}
