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
                throw new Exception( "WorldListEntity: Cannot parse XML: Unnamed worlds are not allowed." );
            }
            if( !Player.IsValidName( temp.Value ) ) {
                throw new Exception( "WorldListEntity: Cannot parse XML: Invalid world name skipped \"" + temp.Value + "\"." );
            }
            name = temp.Value;

            if( (temp = el.Attribute( "hidden" )) != null ) {
                bool hidden;
                if( bool.TryParse( temp.Value, out hidden ) ) {
                    Hidden = hidden;
                } else {
                    throw new Exception( "WorldListEntity: Cannot parse XML: Invalid value for \"hidden\" attribute." );
                }
            } else {
                Hidden = false;
            }

            if( (temp = el.Attribute( "backup" )) != null ) {
                if( Array.IndexOf<string>( World.BackupEnum, temp.Value ) != -1 ) {
                    Backup = temp.Value;
                } else {
                    throw new Exception( "WorldListEntity: Cannot parse XML: Invalid value for \"backup\" attribute." );
                }
            } else {
                Backup = World.BackupEnum[5];
            }

            if( (temp = el.Attribute( "access" )) != null ) {
                accessClass = RankList.ParseRank( temp.Value );
                if( accessClass == null ) {
                    throw new Exception( "WorldListEntity: Cannot parse XML: Unrecognized class specified for \"access\" permission." );
                }
            }

            if( (temp = el.Attribute( "build" )) != null ) {
                buildRank = RankList.ParseRank( temp.Value );
                if( buildRank == null ) {
                    throw new Exception( "WorldListEntity: Cannot parse XML: Unrecognized class specified for \"build\" permission." );
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
                    if( File.Exists( "maps/" + name + ".fcm" ) && value != name ) {
                        File.Move( "maps/" + name + ".fcm", value + ".fcm" );
                    }
                    ConfigUI.HandleWorldRename( oldName, value );
                }
            }
        }

        public string Description {
            get {
                if( cachedMapHeader == null && !loadingFailed ) {
                    cachedMapHeader = Map.LoadHeaderOnly( "maps/" + name + ".fcm" );
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

        internal Rank accessClass;
        public string AccessPermission {
            get {
                if( accessClass != null ) {
                    return accessClass.ToComboBoxOption();
                } else {
                    return DefaultClassOption;
                }
            }
            set {
                foreach( Rank pc in RankList.ranksByIndex ) {
                    if( pc.ToComboBoxOption() == value ) {
                        accessClass = pc;
                        return;
                    }
                }
                accessClass = null;
            }
        }

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
                foreach( Rank rank in RankList.ranksByIndex ) {
                    if( rank.ToComboBoxOption() == value ) {
                        buildRank = rank;
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
            if( accessClass != null ) element.Add( new XAttribute( "access", accessClass ) );
            if( buildRank != null ) element.Add( new XAttribute( "build", buildRank ) );
            return element;
        }
    }
}