using System;
using System.IO;
using System.Xml.Linq;
using fCraft;


namespace ConfigTool {
    /// <summary>
    /// A wrapper for per-World metadata, designed to be usable with SortableBindingList.
    /// All these properties map directly to the UI controls.
    /// </summary>
    sealed class WorldListEntry {
        public const string DefaultRankOption = "(everyone)";
        Map cachedMapHeader;
        internal bool loadingFailed;

        public WorldListEntry() { }

        public WorldListEntry( WorldListEntry original ) {
            name = original.Name;
            Hidden = original.Hidden;
            AccessPermission = original.AccessPermission;
            BuildPermission = original.BuildPermission;
            Backup = original.Backup;
        }

        public WorldListEntry( XElement el ) {
            XAttribute temp;

            if( (temp = el.Attribute( "name" )) == null ) {
                throw new FormatException( "WorldListEntity: Cannot parse XML: Unnamed worlds are not allowed." );
            }
            if( !Player.IsValidName( temp.Value ) ) {
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
                if( Array.IndexOf<string>( World.BackupEnum, temp.Value ) != -1 ) {
                    Backup = temp.Value;
                } else {
                    throw new FormatException( "WorldListEntity: Cannot parse XML: Invalid value for \"backup\" attribute." );
                }
            } else {
                Backup = World.BackupEnum[5];
            }

            // TODO: Support parsing SecurityController

            if( el.Element( "accessSecurity" ) != null ) {
                accessSecurity = new SecurityController( el.Element( "accessSecurity" ) );
            }else if( (temp = el.Attribute( "access" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                accessSecurity.minRank = RankList.ParseRank( temp.Value );
                if( accessSecurity.minRank == null ) {
                    Logger.Log( "WorldListEntity: Unrecognized rank specified for \"access\" permission. Permission reset to default (everyone).", LogType.Warning );
                }
            }

            if( el.Element( "buildSecurity" ) != null ) {
                buildSecurity = new SecurityController( el.Element( "buildSecurity" ) );
            }else if( (temp = el.Attribute( "build" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                buildSecurity.minRank = RankList.ParseRank( temp.Value );
                if( buildSecurity.minRank == null ) {
                    Logger.Log( "WorldListEntity: Unrecognized rank specified for \"build\" permission. Permission reset to default (everyone).", LogType.Warning );
                }
            }
        }


        internal string name;
        public string Name {
            get {
                return name;
            }
            set {
                if( !Player.IsValidName( value ) ) {
                    throw new FormatException( "Invalid world name" );
                } else if( value != name && ConfigUI.IsWorldNameTaken( value ) ) {
                    throw new FormatException( "Duplicate world names are not allowed." );
                } else {
                    string oldName = name;
                    name = value;
                    string oldFileName = Path.Combine( Paths.MapPath, oldName + ".fcm" );
                    string newFileName = Path.Combine( Paths.MapPath, name + ".fcm" );
                    if( File.Exists( oldFileName ) ) {
                        File.Move( oldFileName, newFileName );
                    }
                    ConfigUI.HandleWorldRename( oldName, name );
                }
            }
        }

        public string Description {
            get {
                if( cachedMapHeader == null && !loadingFailed ) {
                    cachedMapHeader = Map.LoadHeaderOnly( Path.Combine( Paths.MapPath, name + ".fcm" ) );
                    if( cachedMapHeader == null ) {
                        loadingFailed = true;
                    }
                }
                if( loadingFailed ) {
                    return "(cannot load file)";
                } else {
                    return String.Format( "{0} × {1} × {2}", cachedMapHeader.widthX, cachedMapHeader.widthY, cachedMapHeader.height );
                }
            }
        }

        public bool Hidden { get; set; }

        SecurityController accessSecurity = new SecurityController();
        string accessRankString;
        public string AccessPermission {
            get {
                if( accessSecurity.minRank != null ) {
                    return accessSecurity.minRank.ToComboBoxOption();
                } else {
                    return DefaultRankOption;
                }
            }
            set {
                foreach( Rank rank in RankList.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        accessSecurity.minRank = rank;
                        accessRankString = rank.ToString();
                        return;
                    }
                }
                accessSecurity.minRank = null;
            }
        }
        
        SecurityController buildSecurity = new SecurityController();
        string buildRankString;
        public string BuildPermission {
            get {
                if( buildSecurity.minRank != null ) {
                    return buildSecurity.minRank.ToComboBoxOption();
                } else {
                    return DefaultRankOption;
                }
            }
            set {
                foreach( Rank rank in RankList.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        buildSecurity.minRank = rank;
                        buildRankString = rank.ToString();
                        return;
                    }
                }
                buildSecurity.minRank = null;
            }
        }

        public string Backup { get; set; }

        internal XElement Serialize() {
            XElement element = new XElement( "World" );
            element.Add( new XAttribute( "name", Name ) );
            element.Add( new XAttribute( "hidden", Hidden ) );
            element.Add( new XAttribute( "backup", Backup ) );
            element.Add( accessSecurity.Serialize("accessSecurity") );
            element.Add( buildSecurity.Serialize( "buildSecurity" ) );
            return element;
        }

        public void ReparseRanks() {
            accessSecurity.minRank = RankList.ParseRank( accessRankString );
            buildSecurity.minRank = RankList.ParseRank( buildRankString );
        }
    }
}