using System;
using System.Xml.Linq;

namespace fCraft {
    /// <summary> Basic empty map generator. Basically a fancy wrapper for Map constructor.
    /// This is a singleton class -- use EmptyMapGen.Instance. </summary>
    public sealed class EmptyMapGen : MapGenerator {
        public static EmptyMapGen Instance { get; private set; }

        static EmptyMapGen() {
            Instance = new EmptyMapGen {
                Name = "Empty"
            };
        }

        public override MapGeneratorParameters CreateDefaultParameters() {
            return new EmptyMapGenParams();
        }

        public override MapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return CreateDefaultParameters();
        }

        public override MapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                player.Message( "Empty map generator does not take any parameters." );
                return null;
            } else {
                return CreateDefaultParameters();
            }
        }

        public override MapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) {
                throw new ArgumentNullException( "presetName" );
            } else if( presetName.Equals( Presets[0], StringComparison.OrdinalIgnoreCase ) ) {
                return CreateDefaultParameters();
            } else {
                return null;
            }
        }
    }


    class EmptyMapGenParams : MapGeneratorParameters {
        public EmptyMapGenParams() {
            Generator = EmptyMapGen.Instance;
        }

        public override MapGeneratorState CreateGenerator() {
            return new EmptyMapGenState( this );
        }
    }


    class EmptyMapGenState : MapGeneratorState {
        public EmptyMapGenState( MapGeneratorParameters genParams ) {
            Parameters = genParams;
        }

        public override Map Generate() {
            return new Map( null, Parameters.MapWidth, Parameters.MapLength, Parameters.MapHeight, true );
        }
    }
}