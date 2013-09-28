// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    /// <summary> Provides functionality for generating map files. </summary>
    sealed class RealisticMapGenState : MapGeneratorState {
        readonly RealisticMapGenParameters genParams;
        readonly Random rand;
        readonly Noise noise;
        float[,] heightmap,
                 blendmap,
                 slopemap;
        RealisticMapGenBlockTheme theme;

        const float CliffsideBlockThreshold = 0.01f;


        public RealisticMapGenState( [NotNull] RealisticMapGenParameters genParameters ) {
            if( genParameters == null ) throw new ArgumentNullException( "genParameters" );
            genParams = genParameters;
            genParams.Validate();
            Parameters = genParameters;

            if( !genParams.CustomWaterLevel ) {
                genParams.WaterLevel = (genParams.MapHeight - 1) / 2;
            }

            rand = new Random( genParams.Seed );
            noise = new Noise( genParams.Seed, NoiseInterpolationMode.Bicubic );
            EstimateComplexity();

            ReportsProgress = true;
            SupportsCancellation = true;
        }


        public override Map Generate() {
            if( Finished ) return Result;
            try {
                GenerateHeightmap();
                if( Canceled ) return null;
                Map map = GenerateMap();
                if( Canceled ) return null;
                Result = map;
                return Result;
            } finally {
                ReportProgress( 100, Canceled ? "Canceled" : "Finished" );
                Finished = true;
            }
        }


        /// <summary> Makes an admincrete barrier, 1 block thick, around the lower half of the map. </summary>
        public static void MakeFloodBarrier( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            for( int x = 0; x < map.Width; x++ ) {
                for( int y = 0; y < map.Length; y++ ) {
                    map.SetBlock( x, y, 0, Block.Admincrete );
                }
            }

            for( int x = 0; x < map.Width; x++ ) {
                for( int z = 0; z < map.Height / 2; z++ ) {
                    map.SetBlock( x, 0, z, Block.Admincrete );
                    map.SetBlock( x, map.Length - 1, z, Block.Admincrete );
                }
            }

            for( int y = 0; y < map.Length; y++ ) {
                for( int z = 0; z < map.Height / 2; z++ ) {
                    map.SetBlock( 0, y, z, Block.Admincrete );
                    map.SetBlock( map.Width - 1, y, z, Block.Admincrete );
                }
            }
        }


        #region Progress Reporting
        int progressTotalEstimate,
            progressRunningTotal;


        void EstimateComplexity() {
            // heightmap creation
            progressTotalEstimate = 10;
            if( genParams.UseBias ) progressTotalEstimate += 2;
            if( genParams.LayeredHeightmap ) progressTotalEstimate += 10;
            if( genParams.MarbledHeightmap ) progressTotalEstimate++;
            if( genParams.InvertHeightmap ) progressTotalEstimate++;

            // heightmap processing
            if( genParams.MatchWaterCoverage ) progressTotalEstimate += 2;
            if( genParams.BelowFuncExponent != 1 || genParams.AboveFuncExponent != 1 ) progressTotalEstimate += 5;
            if( genParams.CliffSmoothing ) progressTotalEstimate += 2;
            progressTotalEstimate += 2; // slope
            if( genParams.MaxHeightVariation > 0 || genParams.MaxDepthVariation > 0 ) progressTotalEstimate += 5;

            // filling
            progressTotalEstimate += 15;

            // post processing
            if( genParams.AddCaves ) progressTotalEstimate += 5;
            if( genParams.AddOre ) progressTotalEstimate += 3;
            if( genParams.AddBeaches ) progressTotalEstimate += 5;
            if( genParams.AddTrees ) progressTotalEstimate += 5;
        }


        void ReportRelativeProgress( int relativeIncrease, [NotNull] string message ) {
            progressRunningTotal += relativeIncrease;
            ReportProgress( (100*progressRunningTotal/progressTotalEstimate), message );
        }

        #endregion


        #region Heightmap Processing

        void GenerateHeightmap() {
            ReportRelativeProgress( 10, "Heightmap: Priming" );
            heightmap = new float[genParams.MapWidth, genParams.MapLength];

            noise.PerlinNoise( heightmap, genParams.FeatureScale, genParams.DetailScale, genParams.Roughness, 0, 0 );

            if( genParams.UseBias && !genParams.DelayBias ) {
                ReportRelativeProgress( 2, "Heightmap: Biasing" );
                Noise.Normalize( heightmap );
                ApplyBias();
            }

            Noise.Normalize( heightmap );

            if( genParams.LayeredHeightmap ) {
                ReportRelativeProgress( 10, "Heightmap: Layering" );

                // needs a new Noise object to randomize second map
                float[,] heightmap2 = new float[genParams.MapWidth, genParams.MapLength];
                new Noise( rand.Next(), NoiseInterpolationMode.Bicubic ).PerlinNoise( heightmap2, 0, genParams.DetailScale, genParams.Roughness, 0, 0 );
                Noise.Normalize( heightmap2 );

                // make a blendmap
                blendmap = new float[genParams.MapWidth, genParams.MapLength];
                int blendmapDetailSize = (int)Math.Log( Math.Max( genParams.MapWidth, genParams.MapLength ), 2 ) - 2;
                new Noise( rand.Next(), NoiseInterpolationMode.Cosine ).PerlinNoise( blendmap, 3, blendmapDetailSize, 0.5f, 0, 0 );
                Noise.Normalize( blendmap );
                float cliffSteepness = Math.Max( genParams.MapWidth, genParams.MapLength ) / 6f;
                Noise.ScaleAndClip( blendmap, cliffSteepness );

                Noise.Blend( heightmap, heightmap2, blendmap );
            }

            if( genParams.MarbledHeightmap ) {
                ReportRelativeProgress( 1, "Heightmap: Marbling" );
                Noise.Marble( heightmap );
            }

            if( genParams.InvertHeightmap ) {
                ReportRelativeProgress( 1, "Heightmap: Inverting" );
                Noise.Invert( heightmap );
            }

            if( genParams.UseBias && genParams.DelayBias ) {
                ReportRelativeProgress( 2, "Heightmap: Biasing" );
                Noise.Normalize( heightmap );
                ApplyBias();
            }
            Noise.Normalize( heightmap );
        }


        void ApplyBias() {
            // set corners and midpoint
            float[] corners = new float[4];
            int c = 0;
            for( int i = 0; i < genParams.RaisedCorners; i++ ) {
                corners[c++] = genParams.Bias;
            }
            for( int i = 0; i < genParams.LoweredCorners; i++ ) {
                corners[c++] = -genParams.Bias;
            }
            float midpoint = (genParams.MidPoint * genParams.Bias);

            // shuffle corners
            corners = corners.OrderBy( r => rand.Next() ).ToArray();

            // overlay the bias
            Noise.ApplyBias( heightmap, corners[0], corners[1], corners[2], corners[3], midpoint );
        }

        #endregion


        #region Map Processing

        int maxHeightScaled;
        int maxDepthScaled;
        int snowAltitudeScaled;

        [NotNull]
        Map GenerateMap() {
            Map map = new Map( null, genParams.MapWidth, genParams.MapLength, genParams.MapHeight, true );
            theme = genParams.Theme;

            // scale features vertically based on map height
            double verticalScale = ( genParams.MapHeight/96.0 )/2 + 0.5;
            maxHeightScaled = (int)Math.Round( genParams.MaxHeight*verticalScale );
            maxDepthScaled = (int)Math.Round( genParams.MaxDepth*verticalScale );
            snowAltitudeScaled = (int)Math.Round( genParams.SnowAltitude*verticalScale );

            // Match water coverage
            float desiredWaterLevel = .5f;
            if( genParams.MatchWaterCoverage ) {
                ReportRelativeProgress( 2, "Heightmap Processing: Matching water coverage" );
                // find a number between 0 and 1 ("desiredWaterLevel") for the heightmap such that
                // the fraction of heightmap coordinates ("blocks") that are below this threshold ("underwater")
                // match the specified WaterCoverage
                desiredWaterLevel = Noise.FindThreshold( heightmap, genParams.WaterCoverage );
            }


            // Calculate above/below water multipliers
            float aboveWaterMultiplier = 0;
            if( desiredWaterLevel < 1 ) {
                aboveWaterMultiplier = ( maxHeightScaled/( 1 - desiredWaterLevel ) );
            }


            // Apply power functions to above/below water parts of the heightmap
            if( Math.Abs( genParams.BelowFuncExponent - 1 ) > float.Epsilon ||
                Math.Abs( genParams.AboveFuncExponent - 1 ) > float.Epsilon ) {
                ReportRelativeProgress( 5, "Heightmap Processing: Adjusting slope" );
                for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                    for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        if( heightmap[x, y] < desiredWaterLevel ) {
                            float normalizedDepth = 1 - heightmap[x, y]/desiredWaterLevel;
                            heightmap[x, y] = desiredWaterLevel -
                                              (float)Math.Pow( normalizedDepth, genParams.BelowFuncExponent )*
                                              desiredWaterLevel;
                        } else {
                            float normalizedHeight = ( heightmap[x, y] - desiredWaterLevel )/( 1 - desiredWaterLevel );
                            heightmap[x, y] = desiredWaterLevel +
                                              (float)Math.Pow( normalizedHeight, genParams.AboveFuncExponent )*
                                              ( 1 - desiredWaterLevel );
                        }
                    }
                }
            }

            // Calculate the slope
            if( genParams.CliffSmoothing ) {
                ReportRelativeProgress( 2, "Heightmap Processing: Smoothing" );
                slopemap = Noise.CalculateSlope( Noise.GaussianBlur5X5( heightmap ) );
            } else {
                slopemap = Noise.CalculateSlope( heightmap );
            }

            // Randomize max height/depth
            float[,] altMap = null;
            if( genParams.MaxHeightVariation != 0 || genParams.MaxDepthVariation != 0 ) {
                ReportRelativeProgress( 5, "Heightmap Processing: Randomizing" );
                altMap = new float[map.Width, map.Length];
                int blendMapDetailSize = (int)Math.Log( Math.Max( genParams.MapWidth, genParams.MapLength ), 2 ) - 2;
                new Noise( rand.Next(), NoiseInterpolationMode.Cosine )
                    .PerlinNoise( altMap,
                                  Math.Min( blendMapDetailSize, 3 ),
                                  blendMapDetailSize,
                                  0.5f,
                                  0,
                                  0 );
                Noise.Normalize( altMap, -1, 1 );
            }

            int snowStartThreshold = snowAltitudeScaled - genParams.SnowTransition;
            int snowThreshold = snowAltitudeScaled;

            ReportRelativeProgress( 10, "Filling" );
            if( theme.AirBlock != Block.Air ) {
                map.Blocks.MemSet( (byte)theme.AirBlock );
            }
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    int level;
                    float slope;
                    if( heightmap[x, y] < desiredWaterLevel ) {
                        // for blocks below "sea level"
                        float depth = maxDepthScaled;
                        if( altMap != null ) {
                            depth += altMap[x, y]*genParams.MaxDepthVariation;
                        }
                        slope = slopemap[x, y]*depth;
                        level = genParams.WaterLevel -
                                (int)
                                Math.Round(
                                    Math.Pow( 1 - heightmap[x, y]/desiredWaterLevel, genParams.BelowFuncExponent )*depth );

                        if( genParams.AddWater ) {
                            if( genParams.WaterLevel - level > 3 ) {
                                map.SetBlock( x, y, genParams.WaterLevel, theme.DeepWaterSurfaceBlock );
                            } else {
                                map.SetBlock( x, y, genParams.WaterLevel, theme.WaterSurfaceBlock );
                            }
                            for( int i = genParams.WaterLevel; i > level; i-- ) {
                                map.SetBlock( x, y, i, theme.WaterBlock );
                            }
                            for( int i = level; i >= 0; i-- ) {
                                if( level - i < theme.SeaFloorThickness ) {
                                    map.SetBlock( x, y, i, theme.SeaFloorBlock );
                                } else {
                                    map.SetBlock( x, y, i, theme.BedrockBlock );
                                }
                            }
                        } else {
                            if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                                map.SetBlock( x, y, level, theme.CliffBlock );
                            } else {
                                if( slope < genParams.CliffThreshold ) {
                                    map.SetBlock( x, y, level, theme.GroundSurfaceBlock );
                                } else {
                                    map.SetBlock( x, y, level, theme.CliffBlock );
                                }
                            }

                            for( int i = level - 1; i >= 0; i-- ) {
                                if( level - i < theme.GroundThickness ) {
                                    if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold &&
                                        blendmap[x, y] < ( 1 - CliffsideBlockThreshold ) ) {
                                        map.SetBlock( x, y, i, theme.CliffBlock );
                                    } else {
                                        if( slope < genParams.CliffThreshold ) {
                                            map.SetBlock( x, y, i, theme.GroundBlock );
                                        } else {
                                            map.SetBlock( x, y, i, theme.CliffBlock );
                                        }
                                    }
                                } else {
                                    map.SetBlock( x, y, i, theme.BedrockBlock );
                                }
                            }
                        }
                    } else {
                        // for blocks above "sea level"
                        float height;
                        if( altMap != null ) {
                            height = maxHeightScaled + altMap[x, y]*genParams.MaxHeightVariation;
                        } else {
                            height = maxHeightScaled;
                        }
                        slope = slopemap[x, y]*height;
                        if( height != 0 ) {
                            level = genParams.WaterLevel +
                                    (int)
                                    Math.Round(
                                        Math.Pow( heightmap[x, y] - desiredWaterLevel, genParams.AboveFuncExponent )*
                                        aboveWaterMultiplier/maxHeightScaled*height );
                        } else {
                            level = genParams.WaterLevel;
                        }

                        bool snow = genParams.AddSnow &&
                                    ( level > snowThreshold ||
                                      ( level > snowStartThreshold &&
                                        rand.NextDouble() <
                                        ( level - snowStartThreshold )/(double)( snowThreshold - snowStartThreshold ) ) );

                        if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                            map.SetBlock( x, y, level, theme.CliffBlock );
                        } else {
                            if( slope < genParams.CliffThreshold ) {
                                map.SetBlock( x, y, level, ( snow ? theme.SnowBlock : theme.GroundSurfaceBlock ) );
                            } else {
                                map.SetBlock( x, y, level, theme.CliffBlock );
                            }
                        }

                        for( int i = level - 1; i >= 0; i-- ) {
                            if( level - i < theme.GroundThickness ) {
                                if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold &&
                                    blendmap[x, y] < ( 1 - CliffsideBlockThreshold ) ) {
                                    map.SetBlock( x, y, i, theme.CliffBlock );
                                } else {
                                    if( slope < genParams.CliffThreshold ) {
                                        if( snow ) {
                                            map.SetBlock( x, y, i, theme.SnowBlock );
                                        } else {
                                            map.SetBlock( x, y, i, theme.GroundBlock );
                                        }
                                    } else {
                                        map.SetBlock( x, y, i, theme.CliffBlock );
                                    }
                                }
                            } else {
                                map.SetBlock( x, y, i, theme.BedrockBlock );
                            }
                        }
                    }
                }
            }

            if( genParams.AddCaves || genParams.AddOre ) {
                AddCaves( map );
            }

            if( genParams.AddBeaches ) {
                ReportRelativeProgress( 5, "Processing: Adding beaches" );
                AddBeaches( map );
            }

            if( genParams.AddTrees ) {
                ReportRelativeProgress( 5, "Processing: Planting trees" );
                if( genParams.AddGiantTrees ) {
                    Map outMap = new Map( null, map.Width, map.Length, map.Height, false ) {
                        Blocks = (byte[])map.Blocks.Clone()
                    };
                    var foresterArgs = new ForesterArgs {
                        Map = map,
                        Rand = rand,
                        TreeCount =
                            (int)
                            ( map.Width*map.Length*4/( 1024f*( genParams.TreeSpacingMax + genParams.TreeSpacingMin )/2 ) ),
                        Operation = Forester.ForesterOperation.Add,
                        PlantOn = theme.GroundSurfaceBlock
                    };
                    foresterArgs.BlockPlacing += ( sender, e ) => outMap.SetBlock( e.Coordinate, e.Block );
                    Forester.Generate( foresterArgs );
                    map = outMap;
                }
                GenerateTrees( map );
            }

            if( genParams.AddFloodBarrier ) {
                MakeFloodBarrier( map );
            }
            return map;
        }

        #region Caves

        // Cave generation method from Omen 0.70, used with osici's permission
        static void AddSingleCave( [NotNull] Random rand, [NotNull] Map map, byte bedrockType, byte fillingType,
                                   int length, double maxDiameter ) {
            if( rand == null ) throw new ArgumentNullException( "rand" );
            if( map == null ) throw new ArgumentNullException( "map" );
            int startX = rand.Next( 0, map.Width );
            int startY = rand.Next( 0, map.Length );
            int startZ = rand.Next( 0, map.Height );

            int k1;
            for( k1 = 0;
                 map.Blocks[startX + map.Width*map.Length*( map.Height - 1 - startZ ) + map.Width*startY] != bedrockType &&
                 k1 < 10000;
                 k1++ ) {
                startX = rand.Next( 0, map.Width );
                startY = rand.Next( 0, map.Length );
                startZ = rand.Next( 0, map.Height );
            }

            if( k1 >= 10000 )
                return;

            int x = startX;
            int y = startY;
            int z = startZ;

            for( int k2 = 0; k2 < length; k2++ ) {
                int diameter = (int)( maxDiameter*rand.NextDouble()*map.Width );
                if( diameter < 1 ) diameter = 2;
                int radius = diameter/2;
                if( radius == 0 ) radius = 1;
                x += (int)( 0.7*( rand.NextDouble() - 0.5D )*diameter );
                y += (int)( 0.7*( rand.NextDouble() - 0.5D )*diameter );
                z += (int)( 0.7*( rand.NextDouble() - 0.5D )*diameter );

                for( int j3 = 0; j3 < diameter; j3++ ) {
                    for( int k3 = 0; k3 < diameter; k3++ ) {
                        for( int l3 = 0; l3 < diameter; l3++ ) {
                            if( ( j3 - radius )*( j3 - radius ) + ( k3 - radius )*( k3 - radius ) +
                                ( l3 - radius )*( l3 - radius ) >= radius*radius ||
                                x + j3 >= map.Width || z + k3 >= map.Height || y + l3 >= map.Length ||
                                x + j3 < 0 || z + k3 < 0 || y + l3 < 0 ) {
                                continue;
                            }

                            int index = x + j3 + map.Width*map.Length*( map.Height - 1 - ( z + k3 ) ) +
                                        map.Width*( y + l3 );

                            if( map.Blocks[index] == bedrockType ) {
                                map.Blocks[index] = fillingType;
                            }
                            if( ( fillingType == 10 || fillingType == 11 || fillingType == 8 || fillingType == 9 ) &&
                                z + k3 < startZ ) {
                                map.Blocks[index] = 0;
                            }
                        }
                    }
                }
            }
        }


        static void AddSingleVein( [NotNull] Random rand, [NotNull] Map map, byte bedrockType, byte fillingType, int k,
                                   double maxDiameter, int l ) {
            AddSingleVein( rand, map, bedrockType, fillingType, k, maxDiameter, l, 10 );
        }


        static void AddSingleVein( [NotNull] Random rand, [NotNull] Map map, byte bedrockType, byte fillingType, int k,
                                   double maxDiameter, int l, int i1 ) {
            if( rand == null ) throw new ArgumentNullException( "rand" );
            if( map == null ) throw new ArgumentNullException( "map" );
            int j1 = rand.Next( 0, map.Width );
            int k1 = rand.Next( 0, map.Height );
            int l1 = rand.Next( 0, map.Length );

            double thirteenOverK = 1/(double)k;

            for( int i2 = 0; i2 < i1; i2++ ) {
                int j2 = j1 + (int)( .5*( rand.NextDouble() - .5 )*map.Width );
                int k2 = k1 + (int)( .5*( rand.NextDouble() - .5 )*map.Height );
                int l2 = l1 + (int)( .5*( rand.NextDouble() - .5 )*map.Length );
                for( int l3 = 0; l3 < k; l3++ ) {
                    int diameter = (int)( maxDiameter*rand.NextDouble()*map.Width );
                    if( diameter < 1 ) diameter = 2;
                    int radius = diameter/2;
                    if( radius == 0 ) radius = 1;
                    int i3 =
                        (int)( ( 1 - thirteenOverK )*j1 + thirteenOverK*j2 + ( l*radius )*( rand.NextDouble() - .5 ) );
                    int j3 =
                        (int)( ( 1 - thirteenOverK )*k1 + thirteenOverK*k2 + ( l*radius )*( rand.NextDouble() - .5 ) );
                    int k3 =
                        (int)( ( 1 - thirteenOverK )*l1 + thirteenOverK*l2 + ( l*radius )*( rand.NextDouble() - .5 ) );
                    for( int k4 = 0; k4 < diameter; k4++ ) {
                        for( int l4 = 0; l4 < diameter; l4++ ) {
                            for( int i5 = 0; i5 < diameter; i5++ ) {
                                if( ( k4 - radius )*( k4 - radius ) + ( l4 - radius )*( l4 - radius ) +
                                    ( i5 - radius )*( i5 - radius ) < radius*radius &&
                                    i3 + k4 < map.Width && j3 + l4 < map.Height && k3 + i5 < map.Length &&
                                    i3 + k4 >= 0 && j3 + l4 >= 0 && k3 + i5 >= 0 ) {

                                    int index = i3 + k4 + map.Width*map.Length*( map.Height - 1 - ( j3 + l4 ) ) +
                                                map.Width*( k3 + i5 );

                                    if( map.Blocks[index] == bedrockType ) {
                                        map.Blocks[index] = fillingType;
                                    }
                                }
                            }
                        }
                    }
                }
                j1 = j2;
                k1 = k2;
                l1 = l2;
            }
        }


        static void SealLiquids( [NotNull] Map map, byte sealantType ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            for( int x = 1; x < map.Width - 1; x++ ) {
                for( int z = 1; z < map.Height; z++ ) {
                    for( int y = 1; y < map.Length - 1; y++ ) {
                        int index = map.Index( x, y, z );
                        if( (map.Blocks[index] == 10 || map.Blocks[index] == 11 || map.Blocks[index] == 8 || map.Blocks[index] == 9) &&
                            (map.GetBlock( x - 1, y, z ) == Block.Air || map.GetBlock( x + 1, y, z ) == Block.Air ||
                            map.GetBlock( x, y - 1, z ) == Block.Air || map.GetBlock( x, y + 1, z ) == Block.Air ||
                            map.GetBlock( x, y, z - 1 ) == Block.Air) ) {
                            map.Blocks[index] = sealantType;
                        }
                    }
                }
            }
        }


        void AddCaves( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            if( genParams.AddCaves ) {
                ReportRelativeProgress( 5, "Processing: Adding caves" );
                for( int i1 = 0; i1 < 36*genParams.CaveDensity; i1++ )
                    AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.Air, 30, 0.05*genParams.CaveSize );

                for( int j1 = 0; j1 < 9*genParams.CaveDensity; j1++ )
                    AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.Air, 500, 0.015 * genParams.CaveSize, 1 );

                for( int k1 = 0; k1 < 30*genParams.CaveDensity; k1++ )
                    AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.Air, 300, 0.03 * genParams.CaveSize, 1, 20 );

                if( genParams.AddCaveLava ) {
                    for( int i = 0; i < 8*genParams.CaveDensity; i++ ) {
                        AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.Lava, 30, 0.05 * genParams.CaveSize );
                    }
                    for( int j = 0; j < 3*genParams.CaveDensity; j++ ) {
                        AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.Lava, 1000, 0.015 * genParams.CaveSize, 1 );
                    }
                }

                if( genParams.AddCaveWater ) {
                    for( int k = 0; k < 8*genParams.CaveDensity; k++ ) {
                        AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.Water, 30, 0.05 * genParams.CaveSize );
                    }
                    for( int l = 0; l < 3*genParams.CaveDensity; l++ ) {
                        AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.Water, 1000, 0.015 * genParams.CaveSize, 1 );
                    }
                }

                SealLiquids( map, (byte)theme.BedrockBlock );
            }


            if( genParams.AddOre ) {
                ReportRelativeProgress( 3, "Processing: Adding ore" );
                for( int l1 = 0; l1 < 12*genParams.CaveDensity; l1++ ) {
                    AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.Coal, 500, 0.03 );
                }

                for( int i2 = 0; i2 < 32*genParams.CaveDensity; i2++ ) {
                    AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.Coal, 200, 0.015, 1 );
                    AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.IronOre, 500, 0.02 );
                }

                for( int k2 = 0; k2 < 8*genParams.CaveDensity; k2++ ) {
                    AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.IronOre, 200, 0.015, 1 );
                    AddSingleVein( rand, map, (byte)theme.BedrockBlock, (byte)Block.GoldOre, 200, 0.0145, 1 );
                }

                for( int l2 = 0; l2 < 20*genParams.CaveDensity; l2++ ) {
                    AddSingleCave( rand, map, (byte)theme.BedrockBlock, (byte)Block.GoldOre, 400, 0.0175 );
                }
            }
        }

        #endregion


        void AddBeaches( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            int beachExtentSqr = (genParams.BeachExtent + 1) * (genParams.BeachExtent + 1);
            for( int x = 0; x < map.Width; x++ ) {
                for( int y = 0; y < map.Length; y++ ) {
                    for( int z = genParams.WaterLevel; z <= genParams.WaterLevel + genParams.BeachHeight; z++ ) {
                        if( map.GetBlock( x, y, z ) != theme.GroundSurfaceBlock ) continue;
                        bool found = false;
                        for( int dx = -genParams.BeachExtent; !found && dx <= genParams.BeachExtent; dx++ ) {
                            for( int dy = -genParams.BeachExtent; !found && dy <= genParams.BeachExtent; dy++ ) {
                                for( int dz = -genParams.BeachHeight; dz <= 0; dz++ ) {
                                    if( dx * dx + dy * dy + dz * dz > beachExtentSqr ) continue;
                                    int xx = x + dx;
                                    int yy = y + dy;
                                    int zz = z + dz;
                                    if( xx < 0 || xx >= map.Width || yy < 0 || yy >= map.Length || zz < 0 ||
                                        zz >= map.Height ) continue;
                                    Block block = map.GetBlock( xx, yy, zz );
                                    if( block == theme.WaterBlock || block == theme.WaterSurfaceBlock ) {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if( found ) {
                            map.SetBlock( x, y, z, theme.SeaFloorBlock );
                            if( z > 0 && map.GetBlock( x, y, z - 1 ) == theme.GroundBlock ) {
                                map.SetBlock( x, y, z - 1, theme.SeaFloorBlock );
                            }
                        }
                    }
                }
            }
        }


        void GenerateTrees( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            int minHeight = genParams.TreeHeightMin;
            int maxHeight = genParams.TreeHeightMax;
            int minTrunkPadding = genParams.TreeSpacingMin;
            int maxTrunkPadding = genParams.TreeSpacingMax;
            const int topLayers = 2;
            const double odds = 0.618;

            Random rn = new Random( genParams.Seed );

            short[][] shadows = map.ComputeHeightmap();

            for( int x = 0; x < map.Width; x += rn.Next( minTrunkPadding, maxTrunkPadding + 1 ) ) {
                for( int y = 0; y < map.Length; y += rn.Next( minTrunkPadding, maxTrunkPadding + 1 ) ) {
                    int nx = x + rn.Next( -(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1 );
                    int ny = y + rn.Next( -(minTrunkPadding / 2), (maxTrunkPadding / 2) + 1 );
                    if( nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Length ) continue;
                    int nz = shadows[nx][ny];

                    if( (map.GetBlock( nx, ny, nz ) == theme.GroundSurfaceBlock) && slopemap[nx, ny] < .5 ) {
                        // Pick a random height for the tree between Min and Max,
                        // discarding this tree if it would breach the top of the map
                        int nh;
                        if( (nh = rn.Next( minHeight, maxHeight + 1 )) + nz + nh / 2 > map.Height )
                            continue;

                        // Generate the trunk of the tree
                        for( int z = 1; z <= nh; z++ )
                            map.SetBlock( nx, ny, nz + z, Block.Log );

                        for( int i = -1; i < nh / 2; i++ ) {
                            // Should we draw thin (2x2) or thicker (4x4) foliage
                            int radius = (i >= (nh / 2) - topLayers) ? 1 : 2;
                            // Draw the foliage
                            for( int xoff = -radius; xoff < radius + 1; xoff++ ) {
                                for( int yoff = -radius; yoff < radius + 1; yoff++ ) {
                                    // Drop random leaves from the edges
                                    if( rn.NextDouble() > odds && Math.Abs( xoff ) == Math.Abs( yoff ) && Math.Abs( xoff ) == radius )
                                        continue;
                                    // By default only replace an existing block if its air
                                    if( map.GetBlock( nx + xoff, ny + yoff, nz + nh + i ) == Block.Air )
                                        map.SetBlock( nx + xoff, ny + yoff, nz + nh + i, Block.Leaves );
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}