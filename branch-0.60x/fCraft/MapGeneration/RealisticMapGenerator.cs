using System;
using System.Xml.Linq;

namespace fCraft {
    public class RealisticMapGenerator : IMapGenerator {
        public static RealisticMapGenerator Instance { get; private set; }
        RealisticMapGenerator() {}

        static RealisticMapGenerator() {
            Instance = new RealisticMapGenerator();
        }


        public string Name {
            get { return "Realistic"; }
        }

        public Version Version {
            get { return new Version( 2, 1 ); }
        }


        public IMapGeneratorParameters GetDefaultParameters() {
            return new MapGeneratorArgs( this );
        }


        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            return new MapGeneratorArgs( this, XElement.Parse( serializedParameters ) );
        }


        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            // todo: /Gen parameter parsing
            return GetDefaultParameters();
        }
    }
}