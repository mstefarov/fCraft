using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public class FlatMapGen : IMapGenerator {
        public static FlatMapGen Instance { get; private set; }
        FlatMapGen() {}

        static FlatMapGen() {
            Instance = new FlatMapGen();

            List<string> presetList = new List<string> {
                "Default (Flatgrass)",
                "Empty",
                "Ocean"
            };
            foreach( string themeName in Enum.GetNames( typeof( MapGenTheme ) ) ) {
                if( themeName != MapGenTheme.Forest.ToString() ) {
                    presetList.Add( themeName );
                }
            }
            PresetList = presetList.ToArray();
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

        public IMapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return new FlatMapGenParameters( serializedParameters );
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


        #region Presets

        public IMapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) {
                throw new ArgumentNullException( "presetName" );
            }
            if( presetName == PresetList[0] ) { // Flatgrass (default)
                return GetDefaultParameters();

            } else if( presetName == PresetList[1] ) { // Empty
                return new FlatMapGenParameters {
                    SurfaceThickness = 0,
                    SoilThickness = 0,
                    BedrockThickness = 0,
                    DeepBlock = Block.Air
                };

            } else if( presetName == PresetList[2] ) { // Ocean
                return new FlatMapGenParameters {
                    SurfaceThickness = 0,
                    SoilThickness = 0,
                    BedrockThickness = 0,
                    DeepBlock = Block.Water
                };

            } else {
                MapGenTheme theme;
                if( EnumUtil.TryParse( presetName, out theme, true ) ) {
                    FlatMapGenParameters genParams = new FlatMapGenParameters();
                    genParams.ApplyTheme( theme );
                    return genParams;
                } else {
                    throw new ArgumentOutOfRangeException( "presetName", "Unrecognized preset name." );
                }
            }
        }

        static readonly string[] PresetList;
        public string[] Presets {
            get { return PresetList; }
        }


        [NotNull]
        public static IMapGeneratorState MakeFlatgrass( int width, int length, int height ) {
            return MakePreset( width, length, height, 0 );
        }


        [NotNull]
        public static IMapGeneratorState MakeEmpty( int width, int length, int height ) {
            return MakePreset( width, length, height, 1 );
        }


        [NotNull]
        public static IMapGeneratorState MakeOcean( int width, int length, int height ) {
            return MakePreset( width, length, height, 2 );
        }


        static IMapGeneratorState MakePreset( int width, int length, int height, int index ) {
            IMapGeneratorParameters preset = Instance.CreateParameters( PresetList[index] );
            preset.MapWidth = width;
            preset.MapLength = length;
            preset.MapHeight = height;
            return preset.CreateGenerator();
        }

        #endregion
    }


    class FlatMapGenParameters : IMapGeneratorParameters {
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


        public FlatMapGenParameters() {
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


        public IMapGeneratorState CreateGenerator() {
            return new FlatMapGenState( this );
        }


        #region Serialization and Cloning

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


        public void Save( XElement el ) {
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

        #endregion
    }


    class FlatMapGenState : IMapGeneratorState {
        public FlatMapGenState( FlatMapGenParameters parameters ) {
            Parameters = parameters;
            StatusString = "Ready";
        }

        public IMapGeneratorParameters Parameters { get; private set; }
        public bool Canceled { get; private set; }
        public bool Finished { get; private set; }
        public int Progress { get; private set; }
        public string StatusString { get; private set; }
        public bool ReportsProgress { get; private set; }
        public bool SupportsCancellation { get; private set; }
        public Map Result { get; private set; }
        public event ProgressChangedEventHandler ProgressChanged;


        public Map Generate() {
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


        public void CancelAsync() {
            Canceled = true;
        }
    }
}