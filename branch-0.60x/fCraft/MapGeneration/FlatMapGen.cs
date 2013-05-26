using System;
using System.ComponentModel;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public class FlatMapGen : IMapGenerator {
        public static FlatMapGen Instance { get; private set; }
        FlatMapGen() {}

        static FlatMapGen() {
            Instance = new FlatMapGen();
        }

        public string Name {
            get { return "Flat"; }
        }

        public Version Version {
            get { return new Version( 1, 0 ); }
        }

        public IMapGeneratorParameters GetDefaultParameters() {
            return new FlatMapGenParameters();
        }

        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            return new FlatMapGenParameters( XElement.Parse( serializedParameters ) );
        }

        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            FlatMapGenParameters newParams = new FlatMapGenParameters();
            string themeName = cmd.Next();
            if( themeName != null ) {
                MapGenTheme theme;
                if( EnumUtil.TryParse( themeName, out theme, true ) ) {
                    newParams.ApplyTheme( theme );
                } else {
                    player.Message( "Gen: Flat: \"{0}\" is not a recognized theme name. Available themes are: {1}",
                                    themeName,
                                    Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                    return null;
                }
            }
            return newParams;
        }


        [NotNull]
        public static IMapGeneratorState MakeFlatgrass( int width, int length, int height ) {
            return new FlatMapGenParameters {
                MapWidth = width,
                MapLength = length,
                MapHeight = height
            }.CreateGenerator();
        }


        [NotNull]
        public static IMapGeneratorState MakeEmpty( int width, int length, int height ) {
            return new FlatMapGenParameters {
                MapWidth = width,
                MapLength = length,
                MapHeight = height,
                SurfaceThickness = 0,
                SoilThickness = 0,
                BedrockThickness = 0,
                DeepBlock = Block.Air
            }.CreateGenerator();
        }


        [NotNull]
        public static IMapGeneratorState MakeOcean( int width, int length, int height ) {
            return new FlatMapGenParameters {
                MapWidth = width,
                MapLength = length,
                MapHeight = height,
                SurfaceThickness = 0,
                SoilThickness = 0,
                BedrockThickness = 0,
                DeepBlock = Block.Water
            }.CreateGenerator();
        }
    }


    public class FlatMapGenParameters : IMapGeneratorParameters {
        [Browsable( false )]
        public int MapWidth { get; set; }
        [Browsable( false )]
        public int MapLength { get; set; }
        [Browsable( false )]
        public int MapHeight { get; set; }

        [Browsable( false )]
        public IMapGenerator Generator {
            get { return FlatMapGen.Instance; }
        }

        [Category( "Layers" )]
        [Description( "Number of blocks (positive or negative) by which the ground level of the map " +
                      "should be offset. Positive values make it higher, negative values make it lower." )]
        [DefaultValue(0)]
        public int GroundLevelOffset { get; set; }

        [Category( "Layers" )]
        [Description( "Thickness, in blocks, of the surface layer of blocks." )]
        [DefaultValue( 1 )]
        public int SurfaceThickness { get; set; }

        [Category( "Layers" )]
        [Description( "Thickness, in blocks, of the shallow/soil layer, right under the surface." )]
        [DefaultValue( 5 )]
        public int SoilThickness { get; set; }

        [Category( "Layers" )]
        [Description( "Thickness, in blocks, of the bottom-most layer blocks (under the deep block)." )]
        [DefaultValue( 1 )]
        public int BedrockThickness { get; set; }

        [Category( "Blocks" )]
        [Description( "Block to use for the upper half of the map, above the surface." )]
        [DefaultValue( Block.Air )]
        public Block AirBlock { get; set; }

        [Category( "Blocks" )]
        [Description( "Block to use for the surface layer." )]
        [DefaultValue( Block.Grass )]
        public Block SurfaceBlock { get; set; }

        [Category( "Blocks" )]
        [Description( "Block to use for the shallow/soil layer, right under the surface." )]
        [DefaultValue( Block.Dirt )]
        public Block ShallowBlock { get; set; }

        [Category( "Blocks" )]
        [Description( "Block to use for the deep/main layer, between shallow/soil and bedrock layers." )]
        [DefaultValue( Block.Stone )]
        public Block DeepBlock { get; set; }

        [Category( "Blocks" )]
        [Description( "Block to use for the bottom-most layer of the map, under the deep/main layer." )]
        [DefaultValue( Block.Admincrete )]
        public Block BedrockBlock { get; set; }


        MapGenTheme lastChosenTheme;
        [Category( "Preset" )]
        public MapGenTheme Preset {
            get { return lastChosenTheme; }
            set { ApplyTheme( value );  }
        }


        public FlatMapGenParameters() {
            ApplyTheme( MapGenTheme.Forest );
        }


        public void ApplyTheme( MapGenTheme theme ) {
            lastChosenTheme = theme;

            // base defaults ("forest")
            SurfaceThickness = 1;
            SoilThickness = 5;
            BedrockThickness = 1;
            AirBlock = Block.Air;
            SurfaceBlock = Block.Grass;
            ShallowBlock = Block.Dirt;
            DeepBlock = Block.Stone;
            BedrockBlock = Block.Admincrete;

            switch( theme ) {
                case MapGenTheme.Arctic:
                    DeepBlock = Block.White;
                    SurfaceThickness = 0;
                    SoilThickness = 0;
                    break;
                case MapGenTheme.Desert:
                    DeepBlock = Block.Sand;
                    SurfaceThickness = 0;
                    SoilThickness = 0;
                    break;
                case MapGenTheme.Hell:
                    DeepBlock = Block.Obsidian;
                    SurfaceThickness = 0;
                    SoilThickness = 0;
                    break;
                case MapGenTheme.Swamp:
                    SurfaceBlock = Block.Dirt;
                    break;
            }
        }


        public FlatMapGenParameters( XElement el )
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
        }


        public string Save() {
            XElement el = new XElement( "FlatMapGenParameters" );
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
            return el.ToString();
        }


        public IMapGeneratorState CreateGenerator() {
            return new FlatMapGenState( this );
        }


        public object Clone() {
            return new FlatMapGenParameters {
                GroundLevelOffset = GroundLevelOffset,
                SurfaceThickness = SurfaceThickness,
                SoilThickness = SoilThickness,
                BedrockThickness = BedrockThickness,
                AirBlock = AirBlock,
                SurfaceBlock = SurfaceBlock,
                ShallowBlock = ShallowBlock,
                DeepBlock = DeepBlock,
                BedrockBlock = BedrockBlock
            };
        }
    }


    class FlatMapGenState : IMapGeneratorState {
        public FlatMapGenState( FlatMapGenParameters parameters ) {
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
                FlatMapGenParameters p = (FlatMapGenParameters)Parameters;

                int layer = Parameters.MapWidth*Parameters.MapLength;

                Map map = new Map( null, Parameters.MapWidth, Parameters.MapLength, Parameters.MapHeight, true );
                int offset = 0;
                if( p.BedrockThickness > 0 ) {
                    if( Canceled ) return null;
                    int bedrockBlocks = layer*p.BedrockThickness;
                    map.Blocks.MemSet( (byte)p.BedrockBlock, 0, bedrockBlocks );
                    offset += bedrockBlocks;
                }

                if( Canceled ) return null;
                int rockBlocks = layer*(Parameters.MapHeight/2 + p.GroundLevelOffset -
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