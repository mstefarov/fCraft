using System;
using System.Xml.Linq;

namespace fCraft {
    public class RealisticMapGen : IMapGenerator {
        public static RealisticMapGen Instance { get; private set; }
        RealisticMapGen() {}

        static RealisticMapGen() {
            Instance = new RealisticMapGen();
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