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
using System.Collections.Generic;

namespace mcc {
    // <remark>
    // Within this program, "Width" will always refer to the X plane,
    // "Depth" will always refer to the Z plane, and "Height" will always
    // refer to the Y plane.
    // 
    //      +y |   / -z  
    //         |  /      
    //         | /
    //  -x     |/     +x
    // ------------------
    //        /|
    //       / |    
    //      /  |
    //  +z /   | -y
    // </remark>
    public class Map {
#region Fields
        /// <summary>
        /// The width of the map (min to max along X)
        /// </summary>
        public ushort Width;
        /// <summary>
        /// The depth of the map (min to max along Z)
        /// </summary>
        public ushort Depth;
        /// <summary>
        /// The height of the map (min to max along Y)
        /// </summary>
        public ushort Height;
        /// <summary>
        /// Spawn position along the X axis
        /// </summary>
        public ushort SpawnX;
        /// <summary>
        /// Spawn position along the Z axis
        /// </summary>
        public ushort SpawnZ;
        /// <summary>
        /// Spawn position along the Y axis
        /// </summary>
        public ushort SpawnY;
        /// <summary>
        /// Spawn rotation along the X axis
        /// </summary>
        public byte SpawnRotation;
        /// <summary>
        /// Spawn rotation along the Y axis
        /// </summary>
        public byte SpawnPitch;
        /// <summary>
        /// A byte array containing the maps block data
        /// </summary>
        public byte[] MapData;
        
        /// <summary>
        /// Contains logging and debugging information
        /// </summary>
        public List<string> ProcessLog = new List<string>();
#endregion
        
#region Utilities
        /// <summary>
        /// The spawn rotation along the X axis in degrees
        /// </summary>
        public double SpawnRotationInDegrees {
            get {
                return SpawnRotation * 1.41176470588235;
            }
            set {
                SpawnRotation = Convert.ToByte( value / 255 );
            }
        }
        /// <summary>
        /// The spawn rotation along the Y axis in degrees
        /// </summary>
        public double SpawnPitchInDegrees {
            get {
                return SpawnPitch * 1.41176470588235;
            }
            set {
                SpawnPitch = Convert.ToByte( value / 255 );
            }
        }
        /// <summary>
        /// The total number of blocks on the map.
        /// </summary>
        public int BlockCount {
            get {
                return Width * Depth * Height;
            }
        }
#endregion
        
#region Indexors
        /// <summary>
        /// The block 
        /// </summary>
        /// <param name="index">
        /// A <see cref="System.Int32"/>
        /// </param>
        public byte this[int index] {
            get {
                return MapData[index];
            }
            set {
                MapData[index] = value;
            }
        }
#endregion
    }
}
