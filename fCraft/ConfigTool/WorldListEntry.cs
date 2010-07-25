using System;
using fCraft;


namespace ConfigTool {
    class WorldListEntry {
        public const string DefaultClassOption = "(everyone)";
        public WorldListEntry() { }
        public WorldListEntry( WorldListEntry original ) {
            Name = original.Name + "_";
            Description = original.Description;
            Hidden = original.Hidden;
            AccessPermission = original.AccessPermission;
            BuildPermission = original.BuildPermission;
            Backup = original.Backup;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public bool Hidden { get; set; }

        internal PlayerClass accessClass;
        public string AccessPermission {
            get {
                if( accessClass != null ) {
                    return accessClass.ToComboBoxOption();
                } else {
                    return DefaultClassOption;
                }
            }
            set {
                foreach( PlayerClass pc in ClassList.classesByIndex ) {
                    if( pc.ToComboBoxOption() == value ) {
                        accessClass = pc;
                        return;
                    }
                }
                accessClass = null;
            }
        }

        internal PlayerClass buildClass;
        public string BuildPermission {
            get {
                if( buildClass != null ) {
                    return buildClass.ToComboBoxOption();
                } else {
                    return DefaultClassOption;
                }
            }
            set {
                foreach( PlayerClass pc in ClassList.classesByIndex ) {
                    if( pc.ToComboBoxOption() == value ) {
                        buildClass = pc;
                        return;
                    }
                }
                buildClass = null;
            }
        }

        public string Backup { get; set; }
    }
}