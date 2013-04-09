using System;

namespace fCraft {
    public class RealisticMapGenerator : IMapGenerator {
        public string Name {
            get { return "Realistic"; }
        }

        public Version Version {
            get { return new Version( 2, 1 ); }
        }


        public IMapGeneratorParameters GetDefaultParameters() {
            return new MapGeneratorArgs( this );
        }

        public IMapGeneratorParameters CreateParameters( string args ) {
            // todo: de-serialization
            return GetDefaultParameters();
        }

        public IMapGeneratorParameters CreateParameters( CommandReader args ) {
            // todo: /Gen parameter parsing
            return GetDefaultParameters();
        }
    }
}