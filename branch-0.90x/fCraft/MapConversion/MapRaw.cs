// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.IO;

namespace fCraft.MapConversion {
    /// <summary> Map exporter that just saves the raw block array, with no metadata. </summary>
    internal sealed class MapRaw : IMapExporter {
        public string ServerName {
            get { return "Raw"; }
        }

        public string FileExtension {
            get { return "raw"; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }

        public MapFormat Format {
            get { return MapFormat.Raw; }
        }


        public void Save(Map mapToSave, string fileName) {
            if (mapToSave == null) throw new ArgumentNullException("mapToSave");
            if (fileName == null) throw new ArgumentNullException("fileName");
            using (FileStream mapStream = File.Create(fileName)) {
                BufferUtil.WriteAll(mapToSave.Blocks, mapStream);
            }
        }
    }
}
