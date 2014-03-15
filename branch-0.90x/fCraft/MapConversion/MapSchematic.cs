using System.IO;
using fNbt;

namespace fCraft.MapConversion {
    /// <summary> Schematic conversion implementation, for exporting fCraft maps to MCEdit and WorldEdit. </summary>
    public class MapSchematic : IMapExporter {
        public string ServerName {
            get { return "Schematic"; }
        }

        public string FileExtension {
            get { return "schematic"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }
        public MapFormat Format {
            get { return MapFormat.Schematic; }
        }

        public void Save(Map mapToSave, string path) {
            string compTagName = Path.GetFileName(path);
            // TODO: convert CPE types to standard types on export
            NbtCompound rootTag = new NbtCompound(compTagName) {
                new NbtShort("Width",(short)mapToSave.Width),
                new NbtShort("Height",(short)mapToSave.Height),
                new NbtShort("Length",(short)mapToSave.Length),
                new NbtString("Materials","Classic"),
                new NbtByteArray("Blocks",mapToSave.Blocks),
                new NbtByteArray("Data",new byte[mapToSave.Volume]) // empty
            };
            NbtFile file = new NbtFile(rootTag);
            file.SaveToFile(path, NbtCompression.GZip);
        }
    }
}
