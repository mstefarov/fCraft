using System.IO;
using fNbt;

namespace fCraft.MapConversion {
    /// <summary> Schematic conversion implementation, for exporting fCraft maps to MCEdit and WorldEdit
    /// with Classic materials. For schematics with converted materials, use MapModernSchematic. </summary>
    internal class MapSchematic : IMapExporter {
        public virtual string ServerName {
            get { return "Schematic"; }
        }

        public string FileExtension {
            get { return "schematic"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public virtual MapFormat Format {
            get { return MapFormat.Schematic; }
        }


        public void Save(Map mapToSave, string path) {
            NbtCompound rootTag = new NbtCompound("Schematic") {
                new NbtShort("Width", (short)mapToSave.Width),
                new NbtShort("Height", (short)mapToSave.Height),
                new NbtShort("Length", (short)mapToSave.Length),
                new NbtString("Materials", "Classic"),
                new NbtByteArray("Blocks", mapToSave.Blocks),

                // set to 0 unless converted in overloaded DoConversion
                new NbtByteArray("Data", new byte[mapToSave.Volume]),

                // these two lists are empty, but required for compatibility
                new NbtList("Entities", NbtTagType.Compound),
                new NbtList("TileEntities", NbtTagType.Compound),
            };
            DoConversion(rootTag);
            NbtFile file = new NbtFile(rootTag);
            file.SaveToFile(path, NbtCompression.GZip);
            File.WriteAllText("debug.txt", file.RootTag.ToString("    "));
        }


        protected virtual void DoConversion(NbtCompound rootTag) {}
    }
}
