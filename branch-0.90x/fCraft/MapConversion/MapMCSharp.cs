// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// Initial support contributed by Tyler Kennedy <tk@tkte.ch>

using System;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> MCSharp map conversion implementation, for converting MCSharp map format into fCraft's default map format. </summary>
    internal class MapMCSharp : IMapImporter, IMapExporter {
        internal MapMCSharp() {
            ServerName = "MCSharp, MCLawl, MCForge, FemtoCraft";
            FileExtension = "lvl";
            StorageType = MapStorageType.SingleFile;
            Format = MapFormat.MCSharp;
        }


        public string ServerName { get; protected set; }
        public string FileExtension { get; protected set; }
        public MapStorageType StorageType { get; protected set; }
        public MapFormat Format { get; protected set; }


        public bool ClaimsName(string fileName) {
            if( fileName == null ) throw new ArgumentNullException("fileName");
            return fileName.EndsWith("." + FileExtension, StringComparison.OrdinalIgnoreCase);
        }


        public bool Claims(string fileName) {
            if( fileName == null ) throw new ArgumentNullException("fileName");
            try {
                using( FileStream mapStream = File.OpenRead(fileName) ) {
                    using( GZipStream gs = new GZipStream(mapStream, CompressionMode.Decompress) ) {
                        BinaryReader bs = new BinaryReader(gs);
                        return (bs.ReadUInt16() == 0x752);
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader(string fileName) {
            if( fileName == null ) throw new ArgumentNullException("fileName");
            using( FileStream mapStream = File.OpenRead(fileName) ) {
                using( GZipStream gs = new GZipStream(mapStream, CompressionMode.Decompress) ) {
                    return LoadHeaderInternal(gs);
                }
            }
        }


        [NotNull]
        static Map LoadHeaderInternal([NotNull] Stream stream) {
            if( stream == null ) throw new ArgumentNullException("stream");
            BinaryReader bs = new BinaryReader(stream);

            // Read in the magic number
            if( bs.ReadUInt16() != 0x752 ) {
                throw new MapFormatException();
            }

            // Read in the map dimensions
            int width = bs.ReadInt16();
            int length = bs.ReadInt16();
            int height = bs.ReadInt16();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map(null, width, length, height, false);
            // ReSharper restore UseObjectOrCollectionInitializer

            // Read in the spawn location
            map.Spawn = new Position(
                (short)(bs.ReadInt16()*32),
                (short)(bs.ReadInt16()*32),
                (short)(bs.ReadInt16()*32),
                bs.ReadByte(),
                bs.ReadByte());

            stream.ReadByte();
            stream.ReadByte();
            return map;
        }


        public Map Load(string fileName) {
            if( fileName == null ) throw new ArgumentNullException("fileName");
            using( FileStream mapStream = File.OpenRead(fileName) ) {
                using( GZipStream gs = new GZipStream(mapStream, CompressionMode.Decompress) ) {
                    Map map = LoadHeaderInternal(gs);
                    // Read in the map data
                    LoadBlockData(map, gs);
                    return map;
                }
            }
        }


        public void Save(Map mapToSave, string fileName) {
            if( mapToSave == null ) throw new ArgumentNullException("mapToSave");
            if( fileName == null ) throw new ArgumentNullException("fileName");
            using( FileStream mapStream = File.Create(fileName) ) {
                using( GZipStream gs = new GZipStream(mapStream, CompressionMode.Compress) ) {
                    BinaryWriter bs = new BinaryWriter(gs);

                    // Write the magic number
                    bs.Write((ushort)0x752);

                    // Write the map dimensions
                    bs.Write((short)mapToSave.Width);
                    bs.Write((short)mapToSave.Length);
                    bs.Write((short)mapToSave.Height);

                    // Write the spawn location
                    bs.Write((short)(mapToSave.Spawn.X/32));
                    bs.Write((short)(mapToSave.Spawn.Z/32));
                    bs.Write((short)(mapToSave.Spawn.Y/32));

                    //Write the spawn orientation
                    bs.Write(mapToSave.Spawn.R);
                    bs.Write(mapToSave.Spawn.L);

                    // Write the VisitPermission and BuildPermission bytes
                    bs.Write((byte)0);
                    bs.Write((byte)0);
                    bs.Close();

                    // Write the map data
                    SaveBlockData(mapToSave, gs);
                }
            }
        }


        protected virtual void LoadBlockData([NotNull] Map map, [NotNull] Stream gs) {
            map.Blocks = new byte[map.Volume];
            BufferUtil.ReadAll(gs, map.Blocks);
            map.ConvertBlockTypes(MCSharpMapping);
        }


        protected virtual void SaveBlockData([NotNull] Map mapToSave, [NotNull] Stream stream) {
            stream.Write(mapToSave.Blocks, 0, mapToSave.Blocks.Length);
        }


        protected static readonly byte[] MCSharpMapping = new byte[256];


        static MapMCSharp() {
            MCSharpMapping[100] = (byte)Block.Glass; // op_glass
            MCSharpMapping[101] = (byte)Block.Obsidian; // opsidian
            MCSharpMapping[102] = (byte)Block.Bricks; // op_brick
            MCSharpMapping[103] = (byte)Block.Stone; // op_stone
            MCSharpMapping[104] = (byte)Block.Cobblestone; // op_cobblestone
            // 105 = op_air
            MCSharpMapping[106] = (byte)Block.Water; // op_water

            // 107-109 unused
            MCSharpMapping[110] = (byte)Block.Wood; // wood_float
            MCSharpMapping[111] = (byte)Block.Log; // door
            MCSharpMapping[112] = (byte)Block.Lava; // lava_fast
            MCSharpMapping[113] = (byte)Block.Obsidian; // door2
            MCSharpMapping[114] = (byte)Block.Glass; // door3
            MCSharpMapping[115] = (byte)Block.Stone; // door4
            MCSharpMapping[116] = (byte)Block.Leaves; // door5
            MCSharpMapping[117] = (byte)Block.Sand; // door6
            MCSharpMapping[118] = (byte)Block.Wood; // door7
            MCSharpMapping[119] = (byte)Block.Green; // door8
            MCSharpMapping[120] = (byte)Block.TNT; // door9
            MCSharpMapping[121] = (byte)Block.Slab; // door10

            MCSharpMapping[122] = (byte)Block.Log; // tdoor
            MCSharpMapping[123] = (byte)Block.Obsidian; // tdoor2
            MCSharpMapping[124] = (byte)Block.Glass; // tdoor3
            MCSharpMapping[125] = (byte)Block.Stone; // tdoor4
            MCSharpMapping[126] = (byte)Block.Leaves; // tdoor5
            MCSharpMapping[127] = (byte)Block.Sand; // tdoor6
            MCSharpMapping[128] = (byte)Block.Wood; // tdoor7
            MCSharpMapping[129] = (byte)Block.Green; // tdoor8

            MCSharpMapping[130] = (byte)Block.White; // MsgWhite
            MCSharpMapping[131] = (byte)Block.Black; // MsgBlack
            MCSharpMapping[132] = (byte)Block.Air; // MsgAir
            MCSharpMapping[133] = (byte)Block.Water; // MsgWater
            MCSharpMapping[134] = (byte)Block.Lava; // MsgLava

            MCSharpMapping[135] = (byte)Block.TNT; // tdoor9
            MCSharpMapping[136] = (byte)Block.Slab; // tdoor10
            MCSharpMapping[137] = (byte)Block.Air; // tdoor11
            MCSharpMapping[138] = (byte)Block.Water; // tdoor12
            MCSharpMapping[139] = (byte)Block.Lava; // tdoor13

            MCSharpMapping[140] = (byte)Block.Water; // WaterDown
            MCSharpMapping[141] = (byte)Block.Lava; // LavaDown
            MCSharpMapping[143] = (byte)Block.Aqua; // WaterFaucet
            MCSharpMapping[144] = (byte)Block.Orange; // LavaFaucet

            // 143 unused
            MCSharpMapping[145] = (byte)Block.Water; // finiteWater
            MCSharpMapping[146] = (byte)Block.Lava; // finiteLava
            MCSharpMapping[147] = (byte)Block.Cyan; // finiteFaucet

            MCSharpMapping[148] = (byte)Block.Log; // odoor1
            MCSharpMapping[149] = (byte)Block.Obsidian; // odoor2
            MCSharpMapping[150] = (byte)Block.Glass; // odoor3
            MCSharpMapping[151] = (byte)Block.Stone; // odoor4
            MCSharpMapping[152] = (byte)Block.Leaves; // odoor5
            MCSharpMapping[153] = (byte)Block.Sand; // odoor6
            MCSharpMapping[154] = (byte)Block.Wood; // odoor7
            MCSharpMapping[155] = (byte)Block.Green; // odoor8
            MCSharpMapping[156] = (byte)Block.TNT; // odoor9
            MCSharpMapping[157] = (byte)Block.Slab; // odoor10
            MCSharpMapping[158] = (byte)Block.Lava; // odoor11
            MCSharpMapping[159] = (byte)Block.Water; // odoor12

            MCSharpMapping[160] = (byte)Block.Air; // air_portal
            MCSharpMapping[161] = (byte)Block.Water; // water_portal
            MCSharpMapping[162] = (byte)Block.Lava; // lava_portal

            // 163 unused
            MCSharpMapping[164] = (byte)Block.Air; // air_door
            MCSharpMapping[165] = (byte)Block.Air; // air_switch
            MCSharpMapping[166] = (byte)Block.Water; // water_door
            MCSharpMapping[167] = (byte)Block.Lava; // lava_door

            // 168-174 = odoor*_air
            MCSharpMapping[175] = (byte)Block.Cyan; // blue_portal
            MCSharpMapping[176] = (byte)Block.Orange; // orange_portal
            // 177-181 = odoor*_air

            MCSharpMapping[182] = (byte)Block.TNT; // smalltnt
            MCSharpMapping[183] = (byte)Block.TNT; // bigtnt
            MCSharpMapping[184] = (byte)Block.Lava; // tntexplosion
            MCSharpMapping[185] = (byte)Block.Lava; // fire

            // 186 unused
            MCSharpMapping[187] = (byte)Block.Glass; // rocketstart
            MCSharpMapping[188] = (byte)Block.Gold; // rockethead
            MCSharpMapping[189] = (byte)Block.Iron; // firework

            MCSharpMapping[190] = (byte)Block.Lava; // deathlava
            MCSharpMapping[191] = (byte)Block.Water; // deathwater
            MCSharpMapping[192] = (byte)Block.Air; // deathair
            MCSharpMapping[193] = (byte)Block.Water; // activedeathwater
            MCSharpMapping[194] = (byte)Block.Lava; // activedeathlava

            MCSharpMapping[195] = (byte)Block.Lava; // magma
            MCSharpMapping[196] = (byte)Block.Water; // geyser

            // 197-210 = air
            MCSharpMapping[211] = (byte)Block.Red; // door8_air
            MCSharpMapping[212] = (byte)Block.Lava; // door9_air
            // 213-229 = air

            MCSharpMapping[230] = (byte)Block.Aqua; // train
            MCSharpMapping[231] = (byte)Block.TNT; // creeper
            MCSharpMapping[232] = (byte)Block.MossyCobble; // zombiebody
            MCSharpMapping[233] = (byte)Block.Lime; // zombiehead

            // 234 unused
            MCSharpMapping[235] = (byte)Block.White; // birdwhite
            MCSharpMapping[236] = (byte)Block.Black; // birdblack
            MCSharpMapping[237] = (byte)Block.Lava; // birdlava
            MCSharpMapping[238] = (byte)Block.Red; // birdred
            MCSharpMapping[239] = (byte)Block.Water; // birdwater
            MCSharpMapping[240] = (byte)Block.Blue; // birdblue
            MCSharpMapping[242] = (byte)Block.Lava; // birdkill

            MCSharpMapping[245] = (byte)Block.Gold; // fishgold
            MCSharpMapping[246] = (byte)Block.Sponge; // fishsponge
            MCSharpMapping[247] = (byte)Block.Gray; // fishshark
            MCSharpMapping[248] = (byte)Block.Red; // fishsalmon
            MCSharpMapping[249] = (byte)Block.Blue; // fishbetta
        }
    }
}
