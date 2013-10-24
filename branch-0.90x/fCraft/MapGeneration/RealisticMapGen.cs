// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    /// <summary> Map generator that creates realistic-looking landscapes. </summary>
    public class RealisticMapGen : MapGenerator {
        public static RealisticMapGen Instance { get; private set; }

        RealisticMapGen() {}

        static RealisticMapGen() {
            Instance = new RealisticMapGen {
                Name = "Realistic",
                Version = new Version( 2, 1 ),
                Presets = Enum.GetNames( typeof( RealisticMapGenTerrainType ) ),
                Help = "&S\"Realistic\" map generator:\n" +
                       "Creates realistic looking landscapes. " +
                       "Default settings produce a random forested landscape. " +
                       "You can specify two parameters, in either order: a terrain type, and a block theme. " +
                       "Terrain types are: " + Enum.GetNames( typeof( RealisticMapGenTerrainType ) ).JoinToString() +
                       ". Block themes are: " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() +
                       ". For example: &H/SetGen Realistic Forest River&S. More options coming soon."
            };
        }


        public override MapGeneratorParameters CreateDefaultParameters() {
            return new RealisticMapGenParameters();
        }


        public override MapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return new RealisticMapGenParameters( serializedParameters );
        }


        public override MapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            string themeName = cmd.Next();
            if( themeName == null ) {
                return CreateDefaultParameters();
            }

            MapGenTheme theme;
            RealisticMapGenTerrainType terrainType;

            string templateName = cmd.Next();
            if( templateName == null ) {
                player.Message( "SetGen: Realistic MapGen requires both a theme and a terrainType. " +
                                "See &H/Help SetGen Realistic&S or check wiki.fCraft.net for details" );
                return null;
            }

            // parse theme
            bool swapThemeAndTemplate;
            if( EnumUtil.TryParse( themeName, out theme, true ) ) {
                swapThemeAndTemplate = false;
            } else if( EnumUtil.TryParse( templateName, out theme, true ) ) {
                swapThemeAndTemplate = true;
            } else {
                player.Message( "SetGen: Unrecognized theme \"{0}\". Available themes are: {1}",
                                themeName,
                                Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                return null;
            }

            // parse terrainType
            if( swapThemeAndTemplate && !EnumUtil.TryParse( themeName, out terrainType, true ) ) {
                MessageTemplateList( themeName, player );
                return null;
            } else if( !EnumUtil.TryParse( templateName, out terrainType, true ) ) {
                MessageTemplateList( templateName, player );
                return null;
            }

            // TODO: optional parameters for preset customization
            return CreateParameters( terrainType, theme );
        }


        static void MessageTemplateList( [NotNull] string templateName, [NotNull] Player player ) {
            if( templateName == null ) throw new ArgumentNullException( "templateName" );
            if( player == null ) throw new ArgumentNullException( "player" );
            player.Message( "SetGen: Unrecognized terrainType \"{0}\". Available terrain types: {1}",
                            templateName,
                            Enum.GetNames( typeof( RealisticMapGenTerrainType ) ).JoinToString() );
        }


        public override MapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) throw new ArgumentNullException( "presetName" );
            RealisticMapGenTerrainType terrainType;
            if( EnumUtil.TryParse( presetName, out terrainType, true ) ) {
                return CreateParameters( terrainType, MapGenTheme.Forest );
            } else {
                return null;
            }
        }


        public static RealisticMapGenParameters CreateParameters( RealisticMapGenTerrainType terrainType,
                                                                  MapGenTheme theme ) {
            RealisticMapGenParameters genParams;
            switch( terrainType ) {
                case RealisticMapGenTerrainType.Archipelago:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 8,
                        MaxDepth = 20,
                        FeatureScale = 3,
                        Roughness = .46f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .85f
                    };
                    break;

                case RealisticMapGenTerrainType.Atoll:
                    genParams = new RealisticMapGenParameters {
                        Theme = new RealisticMapGenBlockTheme( MapGenTheme.Desert ),
                        MaxHeight = 2,
                        MaxDepth = 39,
                        UseBias = true,
                        Bias = .9f,
                        MidPoint = 1,
                        LoweredCorners = 4,
                        FeatureScale = 2,
                        DetailScale = 5,
                        MarbledHeightmap = true,
                        InvertHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .95f
                    };
                    break;

                case RealisticMapGenTerrainType.Bay:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 22,
                        MaxDepth = 12,
                        UseBias = true,
                        Bias = 1,
                        MidPoint = -1,
                        RaisedCorners = 3,
                        LoweredCorners = 1,
                        TreeSpacingMax = 12,
                        TreeSpacingMin = 6,
                        MarbledHeightmap = true,
                        DelayBias = true
                    };
                    break;

                case RealisticMapGenTerrainType.Dunes:
                    genParams = new RealisticMapGenParameters {
                        AddTrees = false,
                        AddWater = false,
                        Theme = new RealisticMapGenBlockTheme( MapGenTheme.Desert ),
                        MaxHeight = 12,
                        MaxDepth = 7,
                        FeatureScale = 2,
                        DetailScale = 3,
                        Roughness = .44f,
                        MarbledHeightmap = true,
                        InvertHeightmap = true
                    };
                    break;

                case RealisticMapGenTerrainType.Hills:
                    genParams = new RealisticMapGenParameters {
                        AddWater = false,
                        MaxHeight = 8,
                        MaxDepth = 8,
                        FeatureScale = 2,
                        TreeSpacingMin = 7,
                        TreeSpacingMax = 13
                    };
                    break;

                case RealisticMapGenTerrainType.Ice:
                    genParams = new RealisticMapGenParameters {
                        Theme = new RealisticMapGenBlockTheme( MapGenTheme.Arctic ),
                        AddTrees = false,
                        MaxHeight = 2,
                        MaxDepth = 2032,
                        FeatureScale = 2,
                        DetailScale = 7,
                        Roughness = .64f,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .3f,
                        MaxHeightVariation = 0
                    };
                    break;

                case RealisticMapGenTerrainType.Island:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 16,
                        MaxDepth = 39,
                        UseBias = true,
                        Bias = .7f,
                        MidPoint = 1,
                        LoweredCorners = 4,
                        FeatureScale = 3,
                        DetailScale = 7,
                        MarbledHeightmap = true,
                        DelayBias = true,
                        AddBeaches = true,
                        Roughness = 0.45f
                    };
                    break;

                case RealisticMapGenTerrainType.Lake:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 14,
                        MaxDepth = 20,
                        UseBias = true,
                        Bias = .65f,
                        MidPoint = -1,
                        RaisedCorners = 4,
                        FeatureScale = 2,
                        Roughness = .56f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .3f
                    };
                    break;

                case RealisticMapGenTerrainType.Mountains:
                    genParams = new RealisticMapGenParameters {
                        AddWater = false,
                        MaxHeight = 40,
                        MaxDepth = 10,
                        FeatureScale = 1,
                        DetailScale = 7,
                        MarbledHeightmap = true,
                        AddSnow = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .5f,
                        Roughness = .55f,
                        CliffThreshold = .9f
                    };
                    break;

                case RealisticMapGenTerrainType.Defaults:
                    genParams = new RealisticMapGenParameters();
                    break;

                case RealisticMapGenTerrainType.River:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 22,
                        MaxDepth = 8,
                        FeatureScale = 0,
                        DetailScale = 6,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .31f
                    };
                    break;

                case RealisticMapGenTerrainType.Streams:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 5,
                        MaxDepth = 4,
                        FeatureScale = 2,
                        DetailScale = 7,
                        Roughness = .55f,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .25f,
                        TreeSpacingMin = 8,
                        TreeSpacingMax = 14
                    };
                    break;

                case RealisticMapGenTerrainType.Peninsula:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 22,
                        MaxDepth = 12,
                        UseBias = true,
                        Bias = .5f,
                        MidPoint = -1,
                        RaisedCorners = 3,
                        LoweredCorners = 1,
                        TreeSpacingMax = 12,
                        TreeSpacingMin = 6,
                        InvertHeightmap = true,
                        WaterCoverage = .5f
                    };
                    break;

                case RealisticMapGenTerrainType.Flat:
                    genParams = new RealisticMapGenParameters {
                        MaxHeight = 0,
                        MaxDepth = 0,
                        MaxHeightVariation = 0,
                        AddWater = false,
                        DetailScale = 0,
                        FeatureScale = 0,
                        AddCliffs = false
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException( "terrainType" );
            }

            genParams.Theme = new RealisticMapGenBlockTheme( theme );
            switch( theme ) {
                case MapGenTheme.Arctic:
                case MapGenTheme.Desert:
                case MapGenTheme.Grass:
                case MapGenTheme.Hell:
                    genParams.AddTrees = false;
                    break;
            }
            return genParams;
        }
    }
}
