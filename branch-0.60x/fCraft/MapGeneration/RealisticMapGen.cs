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
            return new RealisticMapGenParameters( this );
        }


        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            return new RealisticMapGenParameters( this, XElement.Parse( serializedParameters ) );
        }


        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            // todo: /Gen parameter parsing
            return GetDefaultParameters();
        }


        public static RealisticMapGenParameters MakeTemplate( MapGenTemplate template ) {
            switch( template ) {
                case MapGenTemplate.Archipelago:
                    return new RealisticMapGenParameters( Instance ) {
                        MaxHeight = 8,
                        MaxDepth = 20,
                        FeatureScale = 3,
                        Roughness = .46f,
                        MatchWaterCoverage = true,
                        WaterCoverage = .85f
                    };

                case MapGenTemplate.Atoll:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Bay:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Dunes:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Hills:
                    return new RealisticMapGenParameters( Instance ) {
                        AddWater = false,
                        MaxHeight = 8,
                        MaxDepth = 8,
                        FeatureScale = 2,
                        TreeSpacingMin = 7,
                        TreeSpacingMax = 13
                    };

                case MapGenTemplate.Ice:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Island:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Lake:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Mountains:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Random:
                    return new RealisticMapGenParameters( Instance );

                case MapGenTemplate.River:
                    return new RealisticMapGenParameters( Instance ) {
                        MaxHeight = 22,
                        MaxDepth = 8,
                        FeatureScale = 0,
                        DetailScale = 6,
                        MarbledHeightmap = true,
                        MatchWaterCoverage = true,
                        WaterCoverage = .31f
                    };

                case MapGenTemplate.Streams:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Peninsula:
                    return new RealisticMapGenParameters( Instance ) {
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

                case MapGenTemplate.Flat:
                    return new RealisticMapGenParameters( Instance ) {
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