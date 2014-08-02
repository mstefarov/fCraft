using System.IO;

namespace fCraft.MapConversion {
    internal class MapMCF : MapMCSharp {
        internal MapMCF() {
            ServerName = "MCForge-Redux";
            FileExtension = "mcf";
            Format = MapFormat.MCF;
        }


        protected override void LoadBlockData(Map map, Stream stream) {
            for( int i = 0; i < map.Volume; i++ ) {
                map.Blocks[i] = (byte)stream.ReadByte();
                int msb = stream.ReadByte();
                if( msb == 1 ) {
                    map.Blocks[i] = ReduxExtraMapping[map.Blocks[i]];
                }
            }
            map.ConvertBlockTypes(MCSharpMapping);
        }


        protected override void SaveBlockData(Map mapToSave, Stream stream) {
            for( int i = 0; i < mapToSave.Volume; i++ ) {
                stream.WriteByte(mapToSave.Blocks[i]);
                stream.WriteByte(0);
            }
        }


        static readonly byte[] ReduxExtraMapping = new byte[256];


        static MapMCF() {
            ReduxExtraMapping[1] = (byte)Block.Orange; // finiteLavaFaucet
            ReduxExtraMapping[2] = (byte)Block.Red; // redflag
            ReduxExtraMapping[3] = (byte)Block.Blue; // blueflag
            ReduxExtraMapping[4] = (byte)Block.Black; // mine
            ReduxExtraMapping[5] = (byte)Block.BrownMushroom; // trap
        }
    }
}
