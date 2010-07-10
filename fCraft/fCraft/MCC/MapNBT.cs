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
using fCraft;


namespace mcc {
    public sealed class MapNBT : IConverter {

        public MapFormats Format {
            get { return MapFormats.NBT; }
        }

        public string FileExtension {
            get { return ".mclevel"; }
        }

        public string ServerName {
            get { return "indev"; }
        }

        public Map Load( Stream MapStream ) {
            MapStream.Seek( 0, SeekOrigin.Begin );
            GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true );
            NBTag tag = NBTag.ReadStream( gs );

            Map map = new Map();

            NBTag mapTag = tag["Map"];
            map.widthX = mapTag["Width"].GetShort();
            map.height = mapTag["Height"].GetShort();
            map.widthY = mapTag["Length"].GetShort();

            if( !map.ValidateHeader() ) {
                throw new Exception( "One or more of the map dimensions are invalid." );
            }

            map.blocks = mapTag["Blocks"].GetBytes();
            map.ValidateBlockTypes( false );

            map.spawn.x = mapTag["Spawn"][0].GetShort();
            map.spawn.h = mapTag["Spawn"][1].GetShort();
            map.spawn.y = mapTag["Spawn"][2].GetShort();
            map.spawn.r = 0;
            map.spawn.l = 0;

            return map;
        }


        public bool Save( Map MapToSave, Stream MapStream ) {
            throw new NotImplementedException();
        }


        public bool Claims( Stream MapStream ) {
            MapStream.Seek( 0, SeekOrigin.Begin );

            GZipStream gs = new GZipStream( MapStream, CompressionMode.Decompress, true );
            BinaryReader bs = new BinaryReader( gs );

            try {
                if ( bs.ReadByte() == 10 && NBTag.ReadString( bs ) == "MinecraftLevel" )
                    return true;
            } catch ( IOException ) {
                return false;
            } catch ( InvalidDataException ) {
                return false;
            }
            return false;
        }

    }
}