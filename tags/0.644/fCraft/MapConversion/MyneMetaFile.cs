// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> INI parser used by MapMyne. </summary>
    sealed class MyneMetaFile {
        const string Separator = "=";
        readonly Dictionary<string, Dictionary<string, string>> contents = new Dictionary<string, Dictionary<string, string>>();

        public string this[[NotNull] string section, [NotNull] string key] {
            get {
                if( section == null ) throw new ArgumentNullException( "section" );
                if( key == null ) throw new ArgumentNullException( "key" );
                return contents[section][key];
            }
            set {
                if( section == null ) throw new ArgumentNullException( "section" );
                if( key == null ) throw new ArgumentNullException( "key" );
                if( value == null ) throw new ArgumentNullException( "value" );
                if( !contents.ContainsKey( section ) ) {
                    contents[section] = new Dictionary<string, string>();
                }
                contents[section][key] = value;
            }
        }

        public MyneMetaFile( [NotNull] Stream fileStream ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );
            StreamReader reader = new StreamReader( fileStream );
            Dictionary<string, string> section = null;
            string lastKey = null;
            while( true ) {
                string line = reader.ReadLine();
                if( line == null ) break;

                if( line.StartsWith( "#" ) ) {
                    lastKey = null;
                } else if( line.StartsWith( "[" ) ) {
                    lastKey = null;
                    string sectionName = line.Substring( 1, line.IndexOf( ']' ) - 1 ).Trim().ToLower();
                    section = new Dictionary<string, string>();
                    contents.Add( sectionName, section );
                } else if( line.StartsWith( "\t" ) ) {
                    if( lastKey != null ) {
                        section[lastKey] += Environment.NewLine + line.Substring( 1 );
                    }
                } else if( line.Contains( Separator ) && section != null ) {
                    string keyName = line.Substring( 0, line.IndexOf( Separator, StringComparison.Ordinal ) ).TrimEnd().ToLower();
                    string valueName = line.Substring( line.IndexOf( Separator, StringComparison.Ordinal ) + 1 ).TrimStart();
                    section.Add( keyName, valueName );
                    lastKey = keyName;
                } else {
                    lastKey = null;
                }
            }
        }


        public bool ContainsSection( [NotNull] string section ) {
            if( section == null ) throw new ArgumentNullException( "section" );
            return contents.ContainsKey( section.ToLower() );
        }

        public bool Contains( [NotNull] string section, [NotNull] params string[] keys ) {
            if( section == null ) throw new ArgumentNullException( "section" );
            if( keys == null ) throw new ArgumentNullException( "keys" );
            if( contents.ContainsKey( section.ToLower() ) ) {
                return keys.All( key => contents[section.ToLower()].ContainsKey( key.ToLower() ) );
            } else {
                return false;
            }
        }

        public bool IsEmpty {
            get {
                return ( contents.Count == 0 );
            }
        }
    }
}
