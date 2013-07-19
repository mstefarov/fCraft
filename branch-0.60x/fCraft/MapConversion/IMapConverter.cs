// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> A map converter (either importer, exporter, or both). </summary>
    public interface IMapConverter {
        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        [NotNull]
        string ServerName { get; }

        /// <summary> Whether this converter supports importing/loading from the format. </summary>
        bool SupportsImport { get; }

        /// <summary> Whether this converter supports exporting/saving to the format. </summary>
        bool SupportsExport { get; }

        /// <summary> File extension associated with this file.
        /// Throws NotSupportedException if this is a directory-based format. </summary>
        string FileExtension { get; }

        /// <summary> Returns the map storage type (file-based or directory-based). </summary>
        MapStorageType StorageType { get; }

        /// <summary> Returns the format name. </summary>
        MapFormat Format { get; }
    }


    /// <summary> IMapConverter that provides functionality for identifying and saving maps from files. </summary>
    public interface IMapImporter : IMapConverter {
        /// <summary> Returns true if the file name (or directory name) matches this format's expectations. </summary>
        bool ClaimsName( [NotNull] string path );

        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        bool Claims( [NotNull] string path );

        /// <summary> Attempts to load map dimensions from specified location.
        /// Throws MapFormatException on failure. </summary>
        [NotNull]
        Map LoadHeader( [NotNull] string path );

        /// <summary> Fully loads map from specified location.
        /// Throws MapFormatException on failure. </summary>
        [NotNull]
        Map Load( [NotNull] string path );
    }


    /// <summary> IMapConverter that provides functionality for saving maps to files. </summary>
    public interface IMapExporter : IMapConverter {
        /// <summary> Saves given map at the given location. </summary>
        void Save( [NotNull] Map mapToSave, [NotNull] string path );
    }
}
