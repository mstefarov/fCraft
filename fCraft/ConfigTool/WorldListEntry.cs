using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfigTool {
    class WorldListEntry {

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
        public string AccessPermission { get; set; }
        public string BuildPermission { get; set; }
        public string Backup { get; set; }
    }
}