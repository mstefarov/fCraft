using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace fCraft {
    public class FloatingIslandMapGen : IMapGenerator {
        public static FloatingIslandMapGen Instance { get; private set; }
        FloatingIslandMapGen() {}

        static FloatingIslandMapGen() {
            Instance = new FloatingIslandMapGen();
        }

        public string Name {
            get { return "Floating Island"; }
        }

        public Version Version {
            get { return new Version( 1, 0 ); }
        }

        public IMapGeneratorParameters GetDefaultParameters() {
            return new FloatingIslandMapGenParameters();
        }

        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            throw new NotImplementedException();
        }

        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            throw new NotImplementedException();
        }
    }


    public class FloatingIslandMapGenParameters : IMapGeneratorParameters {
        [Browsable( false )]
        public int MapWidth { get; set; }
        [Browsable( false )]
        public int MapLength { get; set; }
        [Browsable( false )]
        public int MapHeight { get; set; }

        [Browsable( false )]
        public IMapGenerator Generator {
            get { return FloatingIslandMapGen.Instance; }
        }


        public FloatingIslandMapGenParameters() {
        }


        public FloatingIslandMapGenParameters( XElement el )
            : this() {
            throw new NotImplementedException();
        }


        public string Save() {
            throw new NotImplementedException();
        }


        public IMapGeneratorState CreateGenerator() {
            return new FloatingIslandMapGenState( this );
        }


        public object Clone() {
            throw new NotImplementedException();
        }
    }


    class FloatingIslandMapGenState : IMapGeneratorState {
        public FloatingIslandMapGenState( FloatingIslandMapGenParameters parameters ) {
            Parameters = parameters;
        }

        public IMapGeneratorParameters Parameters { get; private set; }
        public bool Canceled { get; private set; }
        public bool Finished { get; private set; }
        public int Progress { get; private set; }
        public string StatusString { get; private set; }

        public bool ReportsProgress {
            get { return false; }
        }

        public bool SupportsCancellation {
            get { return true; }
        }

        public Map Result { get; private set; }
        public event ProgressChangedEventHandler ProgressChanged;


        public Map Generate() {
            if( Finished ) return Result;
            try {
                StatusString = "Generating...";
                // TODO
                return Result;
            } finally {
                Finished = true;
                StatusString = (Canceled ? "Canceled" : "Finished");
            }
        }


        public void CancelAsync() {
            Canceled = true;
        }
    }
}