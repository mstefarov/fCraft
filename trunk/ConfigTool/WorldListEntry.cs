using System;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using fCraft;


namespace ConfigTool {
    sealed class WorldListEntry {
        public const string DefaultClassOption = "(everyone)";
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

            if( (temp = el.Attribute( "access" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                accessRank = RankList.ParseRank( temp.Value );
                if( accessRank == null ) {
                    Logger.Log( "WorldListEntity: Unrecognized class specified for \"access\" permission. Permission reset to default (everyone).", LogType.Warning );
                }
            }

            if( (temp = el.Attribute( "build" )) != null && !String.IsNullOrEmpty( temp.Value ) ) {
                buildRank = RankList.ParseRank( temp.Value );
                if( buildRank == null ) {
                    Logger.Log( "WorldListEntity: Unrecognized class specified for \"build\" permission. Permission reset to default (everyone).", LogType.Warning );
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
                    string oldFileName = Path.Combine( "maps", oldName + ".fcm" );
                    string newFileName = Path.Combine( "maps", name + ".fcm" );
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
                    cachedMapHeader = Map.LoadHeaderOnly( Path.Combine( "maps", name + ".fcm" ) );
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

        string accessRankString;
        internal Rank accessRank;
        public string AccessPermission {
            get {
                if( accessRank != null ) {
                    return accessRank.ToComboBoxOption();
                } else {
                    return DefaultClassOption;
                }
            }
            set {
                foreach( Rank rank in RankList.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        accessRank = rank;
                        accessRankString = rank.ToString();
                        return;
                    }
                }
                accessRank = null;
            }
        }

        string buildRankString;
        internal Rank buildRank;
        public string BuildPermission {
            get {
                if( buildRank != null ) {
                    return buildRank.ToComboBoxOption();
                } else {
                    return DefaultClassOption;
                }
            }
            set {
                foreach( Rank rank in RankList.Ranks ) {
                    if( rank.ToComboBoxOption() == value ) {
                        buildRank = rank;
                        buildRankString = rank.ToString();
                        return;
                    }
                }
                buildRank = null;
            }
        }

        public string Backup { get; set; }

        internal XElement Serialize() {
            XElement element = new XElement( "World" );
            element.Add( new XAttribute( "name", Name ) );
            element.Add( new XAttribute( "hidden", Hidden ) );
            element.Add( new XAttribute( "backup", Backup ) );
            if( accessRank != null ) element.Add( new XAttribute( "access", accessRank ) );
            if( buildRank != null ) element.Add( new XAttribute( "build", buildRank ) );
            return element;
        }

        public void ReparseRanks() {
            accessRank = RankList.ParseRank( accessRankString );
            buildRank = RankList.ParseRank( buildRankString );
        }
    }
}