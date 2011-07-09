// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft.MapConversion {
    public interface IMapConverter {
        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        string ServerName { get; }
        

        /// <summary> Returns the map storage type (file-based or directory-based). </summary>
        MapStorageType StorageType { get; }


        /// <summary> Returns the format name. </summary>
        MapFormat Format { get; }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        bool ClaimsName( string path );


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        bool Claims( string path );


        /// <summary> Attempts to load map dimensions from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        Map LoadHeader( string path );


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        Map Load( string path );


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        bool Save( Map mapToSave, string path );
    }
}
