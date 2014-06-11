using System.IO;
using fNbt;

namespace fCraft.MapConversion {
    /// <summary> Schematic conversion implementation, for exporting fCraft maps to MCEdit and WorldEdit
    /// with Classic materials. For schematics with converted materials, use MapModernSchematic. </summary>
    public class MapSchematic : IMapExporter {
        public virtual string ServerName {
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
            NbtCompound rootTag = new NbtCompound(compTagName) {
                new NbtShort("Width", (short)mapToSave.Width),
                new NbtShort("Height", (short)mapToSave.Height),
                new NbtShort("Length", (short)mapToSave.Length),
                new NbtString("Materials", "Classic"),
                new NbtByteArray("Blocks", mapToSave.Blocks),
                new NbtByteArray("Data", new byte[mapToSave.Volume]) // empty
            };
            DoConversion(rootTag);
            NbtFile file = new NbtFile(rootTag);
            file.SaveToFile(path, NbtCompression.GZip);
        }


        protected virtual void DoConversion(NbtCompound rootTag) {}
    }
}
