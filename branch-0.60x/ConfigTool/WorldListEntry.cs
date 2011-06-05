using System;
using System.IO;
using System.Xml.Linq;
using fCraft;
using fCraft.MapConversion;

namespace ConfigTool {
    /// <summary>
    /// A wrapper for per-World metadata, designed to be usable with SortableBindingList.
    /// All these properties map directly to the UI controls.
    /// </summary>
    sealed class WorldListEntry : ICloneable {
        public const string DefaultRankOption = "(everyone)";
        const string MapFileExtension = ".fcm";

        internal bool LoadingFailed { get; private set; }


        public WorldListEntry() { }


        public WorldListEntry( WorldListEntry original ) {
            name = original.Name;
            Hidden = original.Hidden;
            Backup = original.Backup;
            accessSecurity = new SecurityController( original.accessSecurity );
            buildSecurity = new SecurityController( original.buildSecurity );
        }


        public WorldListEntry( XElement el ) {
            XAttribute temp;

            if( (temp = el.Attribute( "name" )) == null ) {
                throw new FormatException( "WorldListEntity: Cannot parse XML: Unnamed worlds are not allowed." );
            }
            if( !World.IsValidName( temp.Value ) ) {
                throw new FormatException( "WorldListEntity: Cannot parse XML: Invalid world name skipped \"" + temp.Value + "\"." );
            }
            name = temp.Value;

            if( (temp = el.Attribute( "hidden" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                bool hidden;
                if( Boolean.TryParse( temp.Value, out hidden ) ) {
                    Hidden = hidden;
                } else {
                    throw new FormatException( "WorldListEntity: Cannot parse XML: Invalid value for \"hidden\" attribute." );
                }
            } else {
                Hidden = false;
            }

            if( (temp = el.Attribute( "backup" )) != null && !String.IsNullOrEmpty( temp.Value ) ) { // TODO: Make per-world backup settings actually work
                if( Array.IndexOf( World.BackupEnum, temp.Value ) != -1 ) {
                    Backup = temp.Value;
                } else {
                    throw new FormatException( "WorldListEntity: Cannot parse XML: Invalid value for \"backup\" attribute." );
                }
            } else {
                Backup = World.BackupEnum[5];
            }

            if( el.Element( "accessSecurity" ) != null ) {
                accessSecurity = new SecurityController( el.Element( "accessSecurity" ) );
            } else if( (temp = el.Attribute( "access" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                accessSecurity.MinRank = RankManager.ParseRank( temp.Value );
            }

            if( el.Element( "buildSecurity" ) != null ) {
                buildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
            } else if( (temp = el.Attribute( "build" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                buildSecurity.MinRank = RankManager.ParseRank( temp.Value );
            }
        }


        #region List Properties

        string name;
        public string Name {
            get {
                return name;
            }
            set {
                if( !World.IsValidName( value ) ) {
                    throw new FormatException( "Invalid world name" );
                } else if( value != name && ConfigUI.IsWorldNameTaken( value ) ) {
                    throw new FormatException( "Duplicate world names are not allowed." );
                } else {
                    string oldName = name;
                    name = value;
                    string oldFileName = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                    string newFileName = Path.Combine( Paths.MapPath, name + ".fcm" );
                    if( File.Exists( oldFileName ) ) {
                        Paths.ForceRename( oldFileName, newFileName );
                    }
                    ConfigUI.HandleWorldRename( oldName, name );
                }
            }
        }


        Map cachedMapHeader;
        public string Description {
            get {
                if( cachedMapHeader == null && !LoadingFailed ) {
                    string fullFileName = Path.Combine( Paths.MapPath, name + ".fcm" );
                    LoadingFailed = !MapUtility.TryLoadHeader( fullFileName, out cachedMapHeader );
                }

                if( LoadingFailed ) {
                    return "(cannot load file)";
                } else {
                    return String.Format( "{0} × {1} × {2}",
                                          cachedMapHeader.WidthX,
                                          cachedMapHeader.WidthY,
                                          cachedMapHeader.Height );
                }
            }
        }


        public bool Hidden { get; set; }


        readonly SecurityController accessSecurity = new SecurityController();
        string accessRankString;
        public string AccessPermission {
            get {
                if( accessSecurity.NoRankRestriction ) {
                    return DefaultRankOption;
                } else {
                    return accessSecurity.MinRank.ToComboBoxOption();
                }
            }
            set {
                foreach( Rank rank in RankManager.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        accessSecurity.MinRank = rank;
                        accessRankString = rank.GetFullName();
                        return;
                    }
                }
                accessSecurity.MinRank = null;
                accessRankString = "";
            }
        }


        readonly SecurityController buildSecurity = new SecurityController();
        string buildRankString;
        public string BuildPermission {
            get {
                if( buildSecurity.NoRankRestriction ) {
                    return DefaultRankOption;
                } else {
                    return buildSecurity.MinRank.ToComboBoxOption();
                }
            }
            set {
                foreach( Rank rank in RankManager.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        buildSecurity.MinRank = rank;
                        buildRankString = rank.GetFullName();
                        return;
                    }
                }
                buildSecurity.MinRank = null;
                buildRankString = null;
            }
        }


        public string Backup { get; set; }

        #endregion


        internal XElement Serialize() {
            XElement element = new XElement( "World" );
            element.Add( new XAttribute( "name", Name ) );
            element.Add( new XAttribute( "hidden", Hidden ) );
            element.Add( new XAttribute( "backup", Backup ) );
            element.Add( accessSecurity.Serialize( "accessSecurity" ) );
            element.Add( buildSecurity.Serialize( "buildSecurity" ) );
            return element;
        }


        public void ReparseRanks() {
            accessSecurity.MinRank = RankManager.ParseRank( accessRankString );
            buildSecurity.MinRank = RankManager.ParseRank( buildRankString );
        }


        public string FileName {
            get { return Name + MapFileExtension; }
        }


        public string FullFileName {
            get { return Path.Combine( Paths.MapPath, Name + MapFileExtension ); }
        }


        #region ICloneable Members

        public object Clone() {
            return new WorldListEntry( this );
        }

        #endregion
    }
}