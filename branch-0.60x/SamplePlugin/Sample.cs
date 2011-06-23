using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fPlugin.Sample {
    public class Sample : IPlugin {
        public string Name {
            get { return "Sample"; }
        }

        public string Description {
            get { return "An example plugin that does not do anything."; }
        }

        static readonly Uri website = new Uri( "http://www.fcraft.net/" );
        public Uri Website {
            get { return website; }
        }

        static readonly Version version = new Version( 1, 0 );
        public Version Version {
            get { return version;  }
        }


        public bool Load() {
            Logger.Log( "Sample plugin loaded!", LogType.SystemActivity );
            return true;
        }
    }
}
