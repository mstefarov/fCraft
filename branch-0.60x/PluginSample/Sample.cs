using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fPlugin.Sample {
    public class Sample : SimplePlugin {
        public Sample() {
            Name = "SamplePlugin";
            Description = "This is a sample fCraft plugin.";
            Version = new Version( 1, 0 );
            Website = new Uri( "http://www.fcraft.net/" );
            MinFCraftVersion = new Version( 6, 0 );
            MaxFCraftVersion = new Version( 7, 0 );

            AddDependency( "SamplePlugin", Version, Version, true );
        }

        protected override void OnLoad( bool dynamic ){
        }

        protected override void OnUnload( bool dynamic ) {
        }
    }
}