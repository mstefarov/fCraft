using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace fCraft {
    class FlatMapGenerator : IMapGenerator {
        public static FlatMapGenerator Instance { get; private set; }
        FlatMapGenerator() {}

        static FlatMapGenerator() {
            Instance = new FlatMapGenerator();
        }

        public string Name {
            get { return "Flat"; }
        }

        public Version Version {
            get { return new Version( 1, 0 ); }
        }

        public IMapGeneratorParameters GetDefaultParameters() {
            return new FlatMapGeneratorParameters();
        }

        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            return new FlatMapGeneratorParameters( XElement.Parse( serializedParameters ) );
        }

        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                player.Message( "Flat map generator has no parameters." );
            }
            return new FlatMapGeneratorParameters();
        }
    }


    class FlatMapGeneratorParameters : IMapGeneratorParameters {
        public int GroundLevelOffset { get; set; }
        public int SurfaceThickness { get; set; }
        public int SoilThickness { get; set; }
        public int BedrockThickness { get; set; }

        public Block AirBlock { get; set; }
        public Block SurfaceBlock { get; set; }
        public Block ShallowBlock { get; set; }
        public Block DeepBlock { get; set; }
        public Block BedrockBlock { get; set; }

        public string SummaryString { get; private set; }

        public IMapGenerator Generator {
            get { return FlatMapGenerator.Instance; }
        }


        public FlatMapGeneratorParameters() {
            SurfaceThickness = 1;
            SoilThickness = 5;
            BedrockThickness = 1;
            AirBlock = Block.Air;
            SurfaceBlock = Block.Grass;
            ShallowBlock = Block.Dirt;
            DeepBlock = Block.Stone;
            BedrockBlock = Block.Admincrete;
            SummaryString = "Flatgrass";
        }


        public FlatMapGeneratorParameters( XElement el )
            : this() {
            XElement xElement = el.Element( "GroundLevelOffset" );
            if( xElement != null ) GroundLevelOffset = Int32.Parse( xElement.Value );
            xElement = el.Element( "SurfaceThickness" );
            if( xElement != null ) SurfaceThickness = Int32.Parse( xElement.Value );
            xElement = el.Element( "SoilThickness" );
            if( xElement != null ) SoilThickness = Int32.Parse( xElement.Value );
            xElement = el.Element( "BedrockThickness" );
            if( xElement != null ) BedrockThickness = Int32.Parse( xElement.Value );

            Block block;
            xElement = el.Element( "AirBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                AirBlock = block;
            }
            xElement = el.Element( "SurfaceBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                SurfaceBlock = block;
            }
            xElement = el.Element( "ShallowBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                ShallowBlock = block;
            }
            xElement = el.Element( "DeepBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                DeepBlock = block;
            }
            xElement = el.Element( "BedrockBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                BedrockBlock = block;
            }
            xElement = el.Element( "SummaryString" );
            if( xElement != null ) SummaryString = xElement.Value;
        }


        public string Save() {
            XElement el = new XElement( "FlatMapGeneratorParameters" );
            el.Add( new XElement( "Version", Generator.Version.ToString() ) );
            el.Add( new XElement( "GroundLevelOffset", GroundLevelOffset ) );
            el.Add( new XElement( "SurfaceThickness", SurfaceThickness ) );
            el.Add( new XElement( "SoilThickness", SoilThickness ) );
            el.Add( new XElement( "BedrockThickness", BedrockThickness ) );
            el.Add( new XElement( "AirBlock", AirBlock ) );
            el.Add( new XElement( "SurfaceBlock", SurfaceBlock ) );
            el.Add( new XElement( "ShallowBlock", ShallowBlock ) );
            el.Add( new XElement( "DeepBlock", DeepBlock ) );
            el.Add( new XElement( "BedrockBlock", BedrockBlock ) );
            el.Add( new XElement( "SummaryString", SummaryString ) );
            return el.ToString();
        }


        public IMapGeneratorState CreateGenerator( int width, int length, int height ) {
            return new FlatMapGeneratorState( this, width, length, height );
        }


        public object Clone() {
            return new FlatMapGeneratorParameters {
                GroundLevelOffset = GroundLevelOffset,
                SurfaceThickness = SurfaceThickness,
                SoilThickness = SoilThickness,
                BedrockThickness = BedrockThickness,
                AirBlock = AirBlock,
                SurfaceBlock = SurfaceBlock,
                ShallowBlock = ShallowBlock,
                DeepBlock = DeepBlock,
                BedrockBlock = BedrockBlock,
                SummaryString = SummaryString
            };
        }
    }


    class FlatMapGeneratorState : IMapGeneratorState {
        public FlatMapGeneratorState( FlatMapGeneratorParameters parameters, int width, int length, int height ) {
            Parameters = parameters;
            MapWidth = width;
            MapLength = length;
            MapHeight = height;
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

        public int MapWidth { get; private set; }
        public int MapLength { get; private set; }
        public int MapHeight { get; private set; }

        public Map Result { get; private set; }
        public event ProgressChangedEventHandler ProgressChanged;


        public Map Generate() {
            if( Finished ) return Result;
            try {
                StatusString = "Generating...";
                FlatMapGeneratorParameters p = (FlatMapGeneratorParameters)Parameters;

                int layer = MapWidth*MapLength;

                Map map = new Map( null, MapWidth, MapLength, MapHeight, true );
                int offset = 0;
                if( p.BedrockThickness > 0 ) {
                    if( Canceled ) return null;
                    int bedrockBlocks = layer*p.BedrockThickness;
                    map.Blocks.MemSet( (byte)p.BedrockBlock, 0, bedrockBlocks );
                    offset += bedrockBlocks;
                }

                if( Canceled ) return null;
                int rockBlocks = layer*(MapHeight/2 + p.GroundLevelOffset -
                                        p.BedrockThickness - p.SoilThickness - p.SurfaceThickness);
                map.Blocks.MemSet( (byte)p.DeepBlock, offset, rockBlocks );
                offset += rockBlocks;

                if( p.SoilThickness > 0 ) {
                    if( Canceled ) return null;
                    int soilBlocks = layer*p.SoilThickness;
                    map.Blocks.MemSet( (byte)p.ShallowBlock, offset, soilBlocks );
                    offset += soilBlocks;
                }

                if( p.SurfaceThickness > 0 ) {
                    if( Canceled ) return null;
                    int surfaceBlocks = layer*p.SurfaceThickness;
                    map.Blocks.MemSet( (byte)p.SurfaceBlock, offset, surfaceBlocks );
                    offset += surfaceBlocks;
                }

                if( p.AirBlock != Block.Air ) {
                    if( Canceled ) return null;
                    map.Blocks.MemSet( (byte)p.AirBlock, offset, map.Blocks.Length - offset );
                }

                if( Canceled ) return null;
                Result = map;
                return map;
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
