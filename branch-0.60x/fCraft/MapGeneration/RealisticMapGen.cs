﻿using System;
using System.Xml.Linq;

namespace fCraft {
    public class RealisticMapGen : MapGenerator {
        public static RealisticMapGen Instance { get; private set; }
        RealisticMapGen() {}

        static RealisticMapGen() {
            Instance = new RealisticMapGen {
                Name = "Realistic",
                Version = new Version( 2, 1 ),
                Presets = Enum.GetNames( typeof( RealisticMapGenTemplate ) )
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

            MapGenTheme theme = MapGenTheme.Grass;
            RealisticMapGenTemplate template = RealisticMapGenTemplate.Flat;

            string templateName = cmd.Next();
            if( templateName == null ) {
                player.Message( "SetGen: Realistic MapGen requires both a theme and a template. " +
                                "See &H/Help SetGen Realistic&S or check wiki.fCraft.net for details" );
                return null;
            }

            // parse theme
            bool swapThemeAndTemplate = false;
            if( EnumUtil.TryParse( themeName, out theme, true ) ) {} else if( EnumUtil.TryParse( templateName, out theme, true ) ) {
                swapThemeAndTemplate = true;

            } else {
                player.Message( "SetGen: Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                    themeName,
                    Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() );
                return null;
            }

            // parse template
            if( swapThemeAndTemplate ) {
                if( !EnumUtil.TryParse( themeName, out template, true ) ) {
                    MessageTemplateList( themeName, player );
                    return null;
                }
            } else {
                if( !EnumUtil.TryParse( templateName, out template, true ) ) {
                    MessageTemplateList( templateName, player );
                    return null;
                }
            }

            RealisticMapGenParameters param = CreateParameters( template );
            param.Theme = new RealisticMapGenTheme( theme );
            return param;
        }


        static void MessageTemplateList( string templateName, Player player ) {
            player.Message( "SetGen: Unrecognized template \"{0}\". Available terrain types: {1}",
                templateName,
                Enum.GetNames( typeof( RealisticMapGenTemplate ) ).JoinToString() );
        }


        public override MapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) {
                throw new ArgumentNullException( "presetName" );
            }
            RealisticMapGenTemplate template;
            if( EnumUtil.TryParse( presetName, out template, true ) ) {
                return CreateParameters( template );
            } else {
                return null;
            }
        }


        public static RealisticMapGenParameters CreateParameters( RealisticMapGenTemplate template ) {
            switch( template ) {
                case RealisticMapGenTemplate.Archipelago:
                    return new RealisticMapGenParameters {
                        MaxHeight = 8,
                        MaxDepth = 20,
                        FeatureScale = 3,
                        Roughness = .46f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .85f
                    };

                case RealisticMapGenTemplate.Atoll:
                    return new RealisticMapGenParameters {
                        Theme = new RealisticMapGenTheme( MapGenTheme.Desert ),
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

                case RealisticMapGenTemplate.Bay:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Dunes:
                    return new RealisticMapGenParameters {
                        AddTrees = false,
                        AddWater = false,
                        Theme = new RealisticMapGenTheme( MapGenTheme.Desert ),
                        MaxHeight = 12,
                        MaxDepth = 7,
                        FeatureScale = 2,
                        DetailScale = 3,
                        Roughness = .44f,
                        MarbledHeightmap = true,
                        InvertHeightmap = true
                    };

                case RealisticMapGenTemplate.Hills:
                    return new RealisticMapGenParameters {
                        AddWater = false,
                        MaxHeight = 8,
                        MaxDepth = 8,
                        FeatureScale = 2,
                        TreeSpacingMin = 7,
                        TreeSpacingMax = 13
                    };

                case RealisticMapGenTemplate.Ice:
                    return new RealisticMapGenParameters {
                        AddTrees = false,
                        Theme = new RealisticMapGenTheme( MapGenTheme.Arctic ),
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

                case RealisticMapGenTemplate.Island:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Lake:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Mountains:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Defaults:
                    return new RealisticMapGenParameters();

                case RealisticMapGenTemplate.River:
                    return new RealisticMapGenParameters {
                        MaxHeight = 22,
                        MaxDepth = 8,
                        FeatureScale = 0,
                        DetailScale = 6,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .31f
                    };

                case RealisticMapGenTemplate.Streams:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Peninsula:
                    return new RealisticMapGenParameters {
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

                case RealisticMapGenTemplate.Flat:
                    return new RealisticMapGenParameters {
                        MaxHeight = 0,
                        MaxDepth = 0,
                        MaxHeightVariation = 0,
                        AddWater = false,
                        DetailScale = 0,
                        FeatureScale = 0,
                        AddCliffs = false
                    };

                default:
                    throw new ArgumentOutOfRangeException( "template" );
            }
        }
    }
}