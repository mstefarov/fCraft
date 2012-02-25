using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Class that describes a plugin. </summary>
    public class PluginDescriptor {
        /// <summary> Name of the this plugin. </summary>
        [NotNull]
        public string Name { get; set; }

        /// <summary> Name of the person or organisation who created this plugin. </summary>
        [NotNull]
        public string Author { get; set; }

        /// <summary> A short paragraph or sentence descibing what this plugin does. </summary>
        [NotNull]
        public string Description { get; set; }

        /// <summary> Version of this plugin. </summary>
        [NotNull]
        public Version Version { get; set; }

        /// <summary> Set of supported fCraft server versions. </summary>
        [NotNull]
        public Version[] CompatibleServerVersions { get; set; }

        /// <summary> Filename (relative to .fpi) of the plugin. </summary>
        [NotNull]
        public string PluginDescriptorFileName { get; set; }

        /// <summary> Filename (relative to .fpi) of the plugin. </summary>
        [NotNull]
        public string PluginFileName { get; set; }

        /// <summary> Type within the PluginFile to instantiate (if applicable). </summary>
        [CanBeNull]
        public string PluginTypeName { get; set; }

        /// <summary> Type of loader (CIL or Python) to use for loading. </summary>
        public PluginLoaderType LoaderType { get; set; }


        /// <summary> Default element name for XML serialization. </summary>
        public const string XmlRootName = "fPlugin";

        public XElement Serialize() {
            return Serialize( XmlRootName );
        }

        public XElement Serialize( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            XElement root = new XElement( tagName );
            root.Add( new XElement( "name", Name ) );
            root.Add( new XElement( "author", Author ) );
            root.Add( new XElement( "description", Description ) );
            root.Add( new XElement( "version", Version ) );
            foreach( Version version in CompatibleServerVersions ) {
                root.Add( new XElement( "compatibleVersion", version ) );
            }
            root.Add( new XElement( "pluginFileName", PluginFileName ) );
            if( PluginTypeName != null ) {
                root.Add( new XElement( "pluginTypeName", PluginTypeName ) );
            }
            root.Add( new XElement( "loaderType", LoaderType ) );
            return root;
        }


        public PluginDescriptor( XElement el ) {
            XElement nameEl = el.Element( "name" );
            if( nameEl == null || String.IsNullOrEmpty( nameEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No name specified." );
            }
            Name = nameEl.Value;
            if( PluginManager.IsValidPluginName( Name ) ) {
                throw new SerializationException( "PluginDescriptor: Unacceptible plugin." );
            }

            XElement authorEl = el.Element( "author" );
            if( authorEl == null || String.IsNullOrEmpty( authorEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No author specified." );
            }
            Author = authorEl.Value;

            XElement descEl = el.Element( "description" );
            if( descEl == null || String.IsNullOrEmpty( descEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No description specified." );
            }
            Description = descEl.Value;

            XElement versionEl = el.Element( "version" );
            if( versionEl == null || String.IsNullOrEmpty( versionEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No version specified." );
            }
            Version = Version.Parse( versionEl.Value );

            List<Version> compatVersions = new List<Version>(); 
            foreach( XElement compatVersionEl in el.Elements( "compatibleVersion" ) ) {
                if( String.IsNullOrEmpty( compatVersionEl.Value ) ) {
                    throw new SerializationException( "PluginDescriptor: Empty compatibleVersion tag." );
                }
                compatVersions.Add( Version.Parse( compatVersionEl.Value ) );
            }
            if( compatVersions.Count == 0 ) {
                throw new SerializationException( "PluginDescriptor: Plugin not marked as compatible with any fCraft versions." );
            }
            CompatibleServerVersions = compatVersions.ToArray();

            XElement pluginFileNameEl = el.Element( "pluginFileName" );
            if( pluginFileNameEl == null || String.IsNullOrEmpty( pluginFileNameEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No pluginFileName specified." );
            }
            PluginFileName = pluginFileNameEl.Value;

            XElement pluginTypeNameEl = el.Element( "pluginTypeName" );
            if( pluginTypeNameEl == null || String.IsNullOrEmpty( pluginTypeNameEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No pluginTypeName specified." );
            }
            PluginTypeName = pluginTypeNameEl.Value;

            XElement loaderTypeEl = el.Element( "loaderType" );
            if( loaderTypeEl == null || String.IsNullOrEmpty( loaderTypeEl.Value ) ) {
                throw new SerializationException( "PluginDescriptor: No loaderType specified." );
            }
            PluginLoaderType loaderType;
            if( !EnumUtil.TryParse( loaderTypeEl.Value, out loaderType, true ) ) {
                throw new SerializationException( "PluginDescriptor: Unrecognized loaderType specified." );
            }
            LoaderType = loaderType;
        }
    }
}