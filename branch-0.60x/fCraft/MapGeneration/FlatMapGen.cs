using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> MapGenerator that creates a flat, featureless, layered map. </summary>
    public class FlatMapGen : MapGenerator {
        public static FlatMapGen Instance { get; private set; }
        protected FlatMapGen() {}

        static FlatMapGen() {
            List<string> presetList = new List<string> {
                "Default (Flatgrass)",
                "Ocean"
            };
            foreach( string themeName in Enum.GetNames( typeof( MapGenTheme ) ) ) {
                if( themeName != MapGenTheme.Forest.ToString() ) {
                    presetList.Add( themeName );
                }
            }

            Instance = new FlatMapGen {
                Name = "Flat",
                Version = new Version( 1, 0 ),
                Presets = presetList.ToArray()
            };
        }


        public override MapGeneratorParameters GetDefaultParameters() {
            return new FlatMapGenParameters( this );
        }

        public override MapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return new FlatMapGenParameters( this, serializedParameters );
        }

        public override MapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            FlatMapGenParameters newParams = new FlatMapGenParameters( this );
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

        public override MapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) {
                throw new ArgumentNullException( "presetName" );
            }
            if( presetName.Equals(Presets[0], StringComparison.OrdinalIgnoreCase) ) { // Flatgrass (default)
                return GetDefaultParameters();

            } else if( presetName.Equals( Presets[1], StringComparison.OrdinalIgnoreCase ) ) {
                return new FlatMapGenParameters( this ) {
                    SurfaceThickness = 0,
                    SoilThickness = 0,
                    BedrockThickness = 0,
                    DeepBlock = Block.Water
                };
            }else{
            MapGenTheme theme;
                if( EnumUtil.TryParse( presetName, out theme, true ) ) {
                    FlatMapGenParameters genParams = new FlatMapGenParameters(this);
                    genParams.ApplyTheme( theme );
                    return genParams;
                } else {
                    throw new ArgumentOutOfRangeException( "presetName", "Unrecognized preset name." );
                }
            }
        }


        [NotNull]
        public static MapGeneratorState MakeFlatgrass( int width, int length, int height ) {
            MapGeneratorParameters preset = Instance.GetDefaultParameters();
            preset.MapWidth = width;
            preset.MapLength = length;
            preset.MapHeight = height;
            return preset.CreateGenerator();
        }
    }


    class FlatMapGenParameters : MapGeneratorParameters {
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


        public FlatMapGenParameters( FlatMapGen generator ) {
            Generator = generator;
            ApplyTheme( MapGenTheme.Forest );
        }


        public void ApplyTheme( MapGenTheme theme ) {
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


        public override MapGeneratorState CreateGenerator() {
            return new FlatMapGenState( this );
        }


        public FlatMapGenParameters( FlatMapGen generator, XElement el )
            : this( generator ) {
            base.LoadProperties( el );
        }
    }


    class FlatMapGenState : MapGeneratorState {
        public FlatMapGenState( FlatMapGenParameters parameters ) {
            Parameters = parameters;
            StatusString = "Ready";
            ReportsProgress = false;
            SupportsCancellation = false;
        }


        public override Map Generate() {
            if( Finished ) return Result;
            try {
                StatusString = "Generating";
                FlatMapGenParameters p = (FlatMapGenParameters)Parameters;

                int layer = Parameters.MapWidth*Parameters.MapLength;

                Map map = new Map( null, Parameters.MapWidth, Parameters.MapLength, Parameters.MapHeight, true );

                int offset = 0;
                if( p.BedrockThickness > 0 ) {
                    int bedrockBlocks = layer*p.BedrockThickness;
                    map.Blocks.MemSet( (byte)p.BedrockBlock, 0, bedrockBlocks );
                    offset += bedrockBlocks;
                }

                int rockBlocks = layer*(Parameters.MapHeight/2 + p.GroundLevelOffset -
                                        p.BedrockThickness - p.SoilThickness - p.SurfaceThickness);
                map.Blocks.MemSet( (byte)p.DeepBlock, offset, rockBlocks );
                offset += rockBlocks;

                if( p.SoilThickness > 0 ) {
                    int soilBlocks = layer*p.SoilThickness;
                    map.Blocks.MemSet( (byte)p.ShallowBlock, offset, soilBlocks );
                    offset += soilBlocks;
                }

                if( p.SurfaceThickness > 0 ) {
                    int surfaceBlocks = layer*p.SurfaceThickness;
                    map.Blocks.MemSet( (byte)p.SurfaceBlock, offset, surfaceBlocks );
                    offset += surfaceBlocks;
                }

                if( p.AirBlock != Block.Air ) {
                    map.Blocks.MemSet( (byte)p.AirBlock, offset, map.Blocks.Length - offset );
                }

                Result = map;
                StatusString = "Done";
                return map;
            } finally {
                Finished = true;
            }
        }
    }
}