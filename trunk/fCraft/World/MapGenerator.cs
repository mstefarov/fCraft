// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.ComponentModel;


namespace fCraft {

    public enum MapGenTheme {
        Arctic,
        Desert,
        Forest,
        Hell,
        Swamp
    }

    public enum MapGenTemplate {
        Archipelago,
        Atoll,
        Bay,
        Default,
        Dunes,
        Flat,
        Hills,
        Ice,
        Island,
        Lake,
        Mountains,
        Peninsula,
        River,
        Streams
    }


    public sealed class MapGenerator {
        MapGeneratorArgs args;
        Random rand;
        Noise noise;
        float[,] heightmap, blendmap, slopemap;

        const int WaterCoveragePasses = 10;
        const float CliffsideBlockThreshold = 0.01f;

        // theme-dependent vars
        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface, bCliff;
        int groundThickness = 5, seaFloorThickness = 3;


        public MapGenerator( MapGeneratorArgs _args ) {
            args = _args;
            args.Validate();

            if( !args.customWaterLevel ) {
                args.waterLevel = (args.dimH - 1) / 2;
            }

            rand = new Random( args.seed );
            noise = new Noise( args.seed, NoiseInterpolationMode.Bicubic );
            ApplyTheme( args.theme );
            EstimateComplexity();
        }


        public Map Generate() {
            GenerateHeightmap();
            return GenerateMap();
        }


        public static void GenerationTask( object task ) {
            MapGenerator gen = (MapGenerator)task;
            gen.Generate();
            //gen.map.Save( gen.fileName );
            Server.RequestGC();
        }


        public static void GenerateFlatgrass( Map map ) {
            for( int i = 0; i < map.widthX; i++ ) {
                for( int j = 0; j < map.widthY; j++ ) {
                    for( int k = 0; k < map.height / 2 - 1; k++ ) {
                        if( k < map.height / 2 - 5 ) {
                            map.SetBlock( i, j, k, Block.Stone );
                        } else {
                            map.SetBlock( i, j, k, Block.Dirt );
                        }
                    }
                    map.SetBlock( i, j, map.height / 2 - 1, Block.Grass );
                }
            }
        }


        #region Progress Reporting
        public ProgressChangedEventHandler ProgressCallback;


        int progressTotalEstimate = 0, progressRunningTotal = 0;


        void EstimateComplexity() {
            // heightmap creation
            progressTotalEstimate = 10;
            if( args.useBias ) progressTotalEstimate += 2;
            if( args.layeredHeightmap ) progressTotalEstimate += 10;
            if( args.marbledHeightmap ) progressTotalEstimate++;
            if( args.invertHeightmap ) progressTotalEstimate++;

            // heightmap processing
            if( args.matchWaterCoverage ) progressTotalEstimate += 2;
            if( args.belowFuncExponent != 1 || args.aboveFuncExponent != 1 ) progressTotalEstimate += 5;
            if( args.cliffSmoothing ) progressTotalEstimate += 2;
            progressTotalEstimate += 2; // slope
            if( args.maxHeightVariation > 0 || args.maxDepthVariation > 0 ) progressTotalEstimate += 5;

            // filling
            progressTotalEstimate += 15;

            // post processing
            if( args.addCaves ) progressTotalEstimate += 5;
            if( args.addOre ) progressTotalEstimate += 3;
            if( args.addBeaches ) progressTotalEstimate += 10;
            if( args.addTrees ) progressTotalEstimate += 5;
        }


        void ReportProgress( int relativeIncrease, string message ) {
            if( ProgressCallback != null ) {
                ProgressCallback( this, new ProgressChangedEventArgs( (int)(100 * progressRunningTotal / progressTotalEstimate), message ) );
            }
            progressRunningTotal += relativeIncrease;
        }

        #endregion


        #region Heightmap Processing

        void GenerateHeightmap() {
            ReportProgress( 10, "Heightmap: Priming" );
            heightmap = new float[args.dimX, args.dimY];

            noise.PerlinNoiseMap( heightmap, args.featureScale, args.detailScale, args.roughness, 0, 0 );

            if( args.useBias && !args.delayBias ) {
                ReportProgress( 2, "Heightmap: Biasing" );
                Noise.Normalize( heightmap );
                ApplyBias();
            }

            Noise.Normalize( heightmap );

            if( args.layeredHeightmap ) {
                ReportProgress( 10, "Heightmap: Layering" );

                // needs a new Noise object to randomize second map
                float[,] heightmap2 = new float[args.dimX, args.dimY];
                new Noise( rand.Next(), NoiseInterpolationMode.Bicubic ).PerlinNoiseMap( heightmap2, 0, args.detailScale, args.roughness, 0, 0 );
                Noise.Normalize( heightmap2 );

                // make a blendmap
                blendmap = new float[args.dimX, args.dimY];
                int blendmapDetailSize = (int)Math.Log( (double)Math.Max( args.dimX, args.dimY ), 2 ) - 2;
                new Noise( rand.Next(), NoiseInterpolationMode.Cosine ).PerlinNoiseMap( blendmap, 3, blendmapDetailSize, 0.5f, 0, 0 );
                Noise.Normalize( blendmap );
                float cliffSteepness = Math.Max( args.dimX, args.dimY ) / 6f;
                Noise.ScaleAndClip( blendmap, cliffSteepness );

                Noise.Blend( heightmap, heightmap2, blendmap );
            }

            if( args.marbledHeightmap ) {
                ReportProgress( 1, "Heightmap: Marbling" );
                Noise.Marble( heightmap );
            }

            if( args.invertHeightmap ) {
                ReportProgress( 1, "Heightmap: Inverting" );
                Noise.Invert( heightmap );
            }

            if( args.useBias && args.delayBias ) {
                ReportProgress( 2, "Heightmap: Biasing" );
                Noise.Normalize( heightmap );
                ApplyBias();
            }
            Noise.Normalize( heightmap );
        }


        void ApplyBias() {
            // set corners and midpoint
            float[] corners = new float[4];
            int c = 0;
            for( int i = 0; i < args.raisedCorners; i++ ) {
                corners[c++] = args.bias;
            }
            for( int i = 0; i < args.loweredCorners; i++ ) {
                corners[c++] = -args.bias;
            }
            float midpoint = (args.midPoint * args.bias);

            // shuffle corners
            corners = corners.OrderBy( r => rand.Next() ).ToArray();

            // overlay the bias
            Noise.ApplyBias( heightmap, corners[0], corners[1], corners[2], corners[3], midpoint );
        }


        // assumes normalzied heightmap
        public static float MatchWaterCoverage( float[,] heightmap, float desiredWaterCoverage ) {
            if( desiredWaterCoverage == 0 ) return 0;
            if( desiredWaterCoverage == 1 ) return 1;
            float waterLevel = 0.5f;
            for( int i = 0; i < WaterCoveragePasses; i++ ) {
                if( CalculateWaterCoverage( heightmap, waterLevel ) > desiredWaterCoverage ) {
                    waterLevel = waterLevel - 1 / (float)(4 << i);
                } else {
                    waterLevel = waterLevel + 1 / (float)(4 << i);
                }
            }
            return waterLevel;
        }


        public static float CalculateWaterCoverage( float[,] heightmap, float waterLevel ) {
            int underwaterBlocks = 0;
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( heightmap[x, y] < waterLevel ) underwaterBlocks++;
                }
            }
            return underwaterBlocks / (float)heightmap.Length;
        }


        public static void CalculateSlope( float[,] heightmap, float[,] slopemap ) {
        }

        #endregion


        #region Map Processing

        public Map GenerateMap() {
            Map map = new Map( null, args.dimX, args.dimY, args.dimH );


            // Match water coverage
            float desiredWaterLevel = .5f;
            if( args.matchWaterCoverage ) {
                ReportProgress( 2, "Heightmap Processing: Matching water coverage" );
                desiredWaterLevel = MatchWaterCoverage( heightmap, args.waterCoverage );
            }


            // Calculate above/below water multipliers
            float underWaterMultiplier = 0, aboveWaterMultiplier = 0;
            if( desiredWaterLevel != 0 ) {
                underWaterMultiplier = (float)(args.maxDepth / desiredWaterLevel);
            }
            if( desiredWaterLevel != 1 ) {
                aboveWaterMultiplier = (float)(args.maxHeight / (1 - desiredWaterLevel));
            }


            // Apply power functions to above/below water parts of the heightmap
            if( args.belowFuncExponent != 1 || args.aboveFuncExponent != 1 ) {
                ReportProgress( 5, "Heightmap Processing: Adjusting slope" );
                for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                    for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        if( heightmap[x, y] < desiredWaterLevel ) {
                            float normalizedDepth = 1 - heightmap[x, y] / desiredWaterLevel;
                            heightmap[x, y] = desiredWaterLevel - (float)Math.Pow( normalizedDepth, args.belowFuncExponent ) * desiredWaterLevel;
                        } else {
                            float normalizedHeight = (heightmap[x, y] - desiredWaterLevel) / (1 - desiredWaterLevel);
                            heightmap[x, y] = desiredWaterLevel + (float)Math.Pow( normalizedHeight, args.aboveFuncExponent ) * (1 - desiredWaterLevel);
                        }
                    }
                }
            }

            // Calculate the slope
            if( args.cliffSmoothing ) {
                ReportProgress( 2, "Heightmap Processing: Smoothing" );
                slopemap = Noise.CalculateSlope( Noise.GaussianBlur5x5( heightmap ) );
            } else {
                slopemap = Noise.CalculateSlope( heightmap );
            }

            int level;
            float slope;

            /* draw heightmap visually (DEBUG)
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( heightmap[x, y] < desiredWaterLevel ) {
                        slope = slopemap[x, y] * args.maxDepth;
                        level = args.waterLevel - (int)Math.Round( (desiredWaterLevel - heightmap[x, y]) * underWaterMultiplier );
                    } else {
                        slope = slopemap[x, y] * args.maxHeight;
                        level = args.waterLevel + (int)Math.Round( (heightmap[x, y] - desiredWaterLevel) * aboveWaterMultiplier );
                    }
                    Block block;
                    if( slope < .12 ) {
                        block = Block.Green;
                    } else if( slope < .24 ) {
                        block = Block.Lime;
                    } else if( slope < .36 ) {
                        block = Block.Yellow;
                    } else if( slope < .48 ) {
                        block = Block.Orange;
                    } else if( slope < .6 ) {
                        block = Block.Red;
                    } else {
                        block = Block.Black;
                    }
                    for( int i = level; i >= 0; i-- ) {
                        map.SetBlock( x, y, i, block );
                    }
                }
            }*/


            float[,] altmap = null;
            if( args.maxHeightVariation != 0 || args.maxDepthVariation != 0 ) {
                ReportProgress( 5, "Heightmap Processing: Randomizing" );
                altmap = new float[map.widthX, map.widthY];
                int blendmapDetailSize = (int)Math.Log( (double)Math.Max( args.dimX, args.dimY ), 2 ) - 2;
                new Noise( rand.Next(), NoiseInterpolationMode.Cosine ).PerlinNoiseMap( altmap, 3, blendmapDetailSize, 0.5f, 0, 0 );
                Noise.Normalize( altmap, -1, 1 );
            }

            int snowStartThreshold = args.snowAltitude - args.snowTransition;
            int snowThreshold = args.snowAltitude;

            ReportProgress( 10, "Filling" );
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {

                    if( heightmap[x, y] < desiredWaterLevel ) {
                        float depth = (args.maxDepthVariation != 0 ? (args.maxDepth + altmap[x, y] * args.maxDepthVariation) : args.maxDepth);
                        slope = slopemap[x, y] * depth;
                        level = args.waterLevel - (int)Math.Round( Math.Pow( 1 - heightmap[x, y] / desiredWaterLevel, args.belowFuncExponent ) * depth );

                        if( args.addWater ) {
                            if( args.waterLevel - level > 3 ) {
                                map.SetBlock( x, y, args.waterLevel, bDeepWaterSurface );
                            } else {
                                map.SetBlock( x, y, args.waterLevel, bWaterSurface );
                            }
                            for( int i = args.waterLevel; i > level; i-- ) {
                                map.SetBlock( x, y, i, bWater );
                            }
                            for( int i = level; i >= 0; i-- ) {
                                if( level - i < seaFloorThickness ) {
                                    map.SetBlock( x, y, i, bSeaFloor );
                                } else {
                                    map.SetBlock( x, y, i, bBedrock );
                                }
                            }
                        } else {
                            if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                                map.SetBlock( x, y, level, bCliff );
                            } else {
                                if( slope < args.cliffThreshold ) {
                                    map.SetBlock( x, y, level, bGroundSurface );
                                } else {
                                    map.SetBlock( x, y, level, bCliff );
                                }
                            }

                            for( int i = level - 1; i >= 0; i-- ) {
                                if( level - i < groundThickness ) {
                                    if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold) ) {
                                        map.SetBlock( x, y, i, bCliff );
                                    } else {
                                        if( slope < args.cliffThreshold ) {
                                            map.SetBlock( x, y, i, bGround );
                                        } else {
                                            map.SetBlock( x, y, i, bCliff );
                                        }
                                    }
                                } else {
                                    map.SetBlock( x, y, i, bBedrock );
                                }
                            }
                        }

                    } else {
                        float height = (args.maxHeightVariation != 0 ? (args.maxHeight + altmap[x, y] * args.maxHeightVariation) : args.maxHeight);
                        slope = slopemap[x, y] * height;
                        if( height != 0 ) {
                            level = args.waterLevel + (int)Math.Round( Math.Pow( heightmap[x, y] - desiredWaterLevel, args.aboveFuncExponent ) * aboveWaterMultiplier / args.maxHeight * height );
                        } else {
                            level = args.waterLevel;
                        }

                        bool snow = args.addSnow &&
                                    (level > snowThreshold ||
                                    (level > snowStartThreshold && rand.NextDouble() < (level - snowStartThreshold) / (double)(snowThreshold - snowStartThreshold)));

                        if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                            map.SetBlock( x, y, level, bCliff );
                        } else {
                            if( slope < args.cliffThreshold ) {
                                if( snow ) {
                                    map.SetBlock( x, y, level, Block.White );
                                } else {
                                    map.SetBlock( x, y, level, bGroundSurface );
                                }
                            } else {
                                map.SetBlock( x, y, level, bCliff );
                            }
                        }

                        for( int i = level - 1; i >= 0; i-- ) {
                            if( level - i < groundThickness ) {
                                if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold) ) {
                                    map.SetBlock( x, y, i, bCliff );
                                } else {
                                    if( slope < args.cliffThreshold ) {
                                        if( snow ) {
                                            map.SetBlock( x, y, i, Block.White );
                                        } else {
                                            map.SetBlock( x, y, i, bGround );
                                        }
                                    } else {
                                        map.SetBlock( x, y, i, bCliff );
                                    }
                                }
                            } else {
                                map.SetBlock( x, y, i, bBedrock );
                            }
                        }
                    }
                }
            }

            if( args.addCaves || args.addOre ) {
                AddCaves( map );
            }

            if( args.addBeaches ) {
                ReportProgress( 10, "Processing: Adding beaches" );
                AddBeaches( map );
            }

            if( args.addTrees ) {
                ReportProgress( 5, "Processing: Planting trees" );
                Map outMap = new Map();
                outMap.blocks = (byte[])map.blocks.Clone();
                outMap.widthX = map.widthX;
                outMap.widthY = map.widthY;
                outMap.height = map.height;

                Forester treeGen = new Forester( new Forester.ForesterArgs {
                    inMap = map,
                    outMap = outMap,
                    rand = rand,
                    TREECOUNT = (int)(map.widthX * map.widthY * 4 / (1024f * (args.treeSpacingMax + args.treeSpacingMin) / 2)),
                    OPERATION = Forester.Operation.Add,
                    bGroundSurface = bGroundSurface
                } );
                treeGen.Generate();
                map = outMap;

                GenerateTrees( map );
            }

            ReportProgress( 0, "Generation complete" );
            map.ResetSpawn();
            return map;
        }


        #region Caves

        // Cave generation method from Omen 0.70, used with osici's permission
        static void AddSingleCave( Random rand, Map map, byte bedrockType, byte fillingType, int length, double maxDiameter ) {

            int startX = rand.Next( 0, map.widthX );
            int startY = rand.Next( 0, map.widthY );
            int startH = rand.Next( 0, map.height );

            int k1;
            for( k1 = 0; map.blocks[startX + map.widthX * map.widthY * (map.height - 1 - startH) + map.widthX * startY] != bedrockType && k1 < 10000; k1++ ) {
                startX = rand.Next( 0, map.widthX );
                startY = rand.Next( 0, map.widthY );
                startH = rand.Next( 0, map.height );
            }

            if( k1 >= 10000 )
                return;

            int x = startX;
            int y = startY;
            int h = startH;

            for( int k2 = 0; k2 < length; k2++ ) {
                int diameter = (int)(maxDiameter * rand.NextDouble() * map.widthX);
                if( diameter < 1 ) diameter = 2;
                int radius = diameter / 2;
                if( radius == 0 ) radius = 1;
                x += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);
                y += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);
                h += (int)(0.7 * (rand.NextDouble() - 0.5D) * diameter);

                for( int j3 = 0; j3 < diameter; j3++ ) {
                    for( int k3 = 0; k3 < diameter; k3++ ) {
                        for( int l3 = 0; l3 < diameter; l3++ ) {
                            if( (j3 - radius) * (j3 - radius) + (k3 - radius) * (k3 - radius) + (l3 - radius) * (l3 - radius) >= radius * radius ||
                                x + j3 >= map.widthX || h + k3 >= map.height || y + l3 >= map.widthY ||
                                x + j3 < 0 || h + k3 < 0 || y + l3 < 0 ) {
                                continue;
                            }

                            int index = x + j3 + map.widthX * map.widthY * (map.height - 1 - (h + k3)) + map.widthX * (y + l3);

                            if( map.blocks[index] == bedrockType ) {
                                map.blocks[index] = (byte)fillingType;
                            }
                            if( (fillingType == 10 || fillingType == 11 || fillingType == 8 || fillingType == 9) &&
                                h + k3 < startH ) {
                                map.blocks[index] = 0;
                            }
                        }
                    }
                }
            }
        }

        static void AddSingleVein( Random rand, Map map, byte bedrockType, byte fillingType, int k, double maxDiameter, int l ) {
            AddSingleVein( rand, map, bedrockType, fillingType, k, maxDiameter, l, 10 );
        }

        static void AddSingleVein( Random rand, Map map, byte bedrockType, byte fillingType, int k, double maxDiameter, int l, int i1 ) {

            int j1 = rand.Next( 0, map.widthX );
            int k1 = rand.Next( 0, map.height );
            int l1 = rand.Next( 0, map.widthY );

            double thirteenOverK = 1 / (double)k;

            for( int i2 = 0; i2 < i1; i2++ ) {
                int j2 = j1 + (int)(.5 * (rand.NextDouble() - .5) * (double)map.widthX);
                int k2 = k1 + (int)(.5 * (rand.NextDouble() - .5) * (double)map.height);
                int l2 = l1 + (int)(.5 * (rand.NextDouble() - .5) * (double)map.widthY);
                for( int l3 = 0; l3 < k; l3++ ) {
                    int diameter = (int)(maxDiameter * rand.NextDouble() * map.widthX);
                    if( diameter < 1 ) diameter = 2;
                    int radius = diameter / 2;
                    if( radius == 0 ) radius = 1;
                    int i3 = (int)((1 - thirteenOverK) * (double)j1 + thirteenOverK * (double)j2 + (double)(l * radius) * (rand.NextDouble() - .5));
                    int j3 = (int)((1 - thirteenOverK) * (double)k1 + thirteenOverK * (double)k2 + (double)(l * radius) * (rand.NextDouble() - .5));
                    int k3 = (int)((1 - thirteenOverK) * (double)l1 + thirteenOverK * (double)l2 + (double)(l * radius) * (rand.NextDouble() - .5));
                    for( int k4 = 0; k4 < diameter; k4++ ) {
                        for( int l4 = 0; l4 < diameter; l4++ ) {
                            for( int i5 = 0; i5 < diameter; i5++ ) {
                                if( (k4 - radius) * (k4 - radius) + (l4 - radius) * (l4 - radius) + (i5 - radius) * (i5 - radius) < radius * radius &&
                                    i3 + k4 < map.widthX && j3 + l4 < map.height && k3 + i5 < map.widthY &&
                                    i3 + k4 >= 0 && j3 + l4 >= 0 && k3 + i5 >= 0 ) {

                                    int index = i3 + k4 + map.widthX * map.widthY * (map.height - 1 - (j3 + l4)) + map.widthX * (k3 + i5);

                                    if( map.blocks[index] == bedrockType ) {
                                        map.blocks[index] = fillingType;
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

        static void SealLiquids( Map map, byte sealantType ) {
            for( int x = 1; x < map.widthX - 1; x++ ) {
                for( int h = 1; h < map.height; h++ ) {
                    for( int y = 1; y < map.widthY - 1; y++ ) {
                        int index = map.Index( x, y, h );
                        if( (map.blocks[index] == 10 || map.blocks[index] == 11 || map.blocks[index] == 8 || map.blocks[index] == 9) &&
                            (map.GetBlock( x - 1, y, h ) == 0 || map.GetBlock( x + 1, y, h ) == 0 ||
                            map.GetBlock( x, y - 1, h ) == 0 || map.GetBlock( x, y + 1, h ) == 0 ||
                            map.GetBlock( x, y, h - 1 ) == 0) ) {
                            map.blocks[index] = sealantType;
                        }
                    }
                }
            }
        }

        public void AddCaves( Map map ) {
            Random rand = new Random();

            if( args.addCaves ) {
                ReportProgress( 5, "Processing: Adding caves" );
                for( int i1 = 0; i1 < 36 * args.caveDensity; i1++ )
                    AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.Air, 30, 0.05 * args.caveSize );

                for( int j1 = 0; j1 < 9 * args.caveDensity; j1++ )
                    AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.Air, 500, 0.015 * args.caveSize, 1 );

                for( int k1 = 0; k1 < 30 * args.caveDensity; k1++ )
                    AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.Air, 300, 0.03 * args.caveSize, 1, 20 );


                if( args.addCaveLava ) {
                    for( int i = 0; i < 8 * args.caveDensity; i++ ) {
                        AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.Lava, 30, 0.05 * args.caveSize );
                    }
                    for( int j = 0; j < 3 * args.caveDensity; j++ ) {
                        AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.Lava, 1000, 0.015 * args.caveSize, 1 );
                    }
                }


                if( args.addCaveWater ) {
                    for( int k = 0; k < 8 * args.caveDensity; k++ ) {
                        AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.Water, 30, 0.05 * args.caveSize );
                    }
                    for( int l = 0; l < 3 * args.caveDensity; l++ ) {
                        AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.Water, 1000, 0.015 * args.caveSize, 1 );
                    }
                }

                SealLiquids( map, (byte)bBedrock );
            }


            if( args.addOre ) {
                ReportProgress( 3, "Processing: Adding ore" );
                for( int l1 = 0; l1 < 12 * args.caveDensity; l1++ ) {
                    AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.Coal, 500, 0.03 );
                }

                for( int i2 = 0; i2 < 32 * args.caveDensity; i2++ ) {
                    AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.Coal, 200, 0.015, 1 );
                    AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.IronOre, 500, 0.02 );
                }

                for( int k2 = 0; k2 < 8 * args.caveDensity; k2++ ) {
                    AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.IronOre, 200, 0.015, 1 );
                    AddSingleVein( rand, map, (byte)bBedrock, (byte)Block.GoldOre, 200, 0.0145, 1 );
                }

                for( int l2 = 0; l2 < 20 * args.caveDensity; l2++ ) {
                    AddSingleCave( rand, map, (byte)bBedrock, (byte)Block.GoldOre, 400, 0.0175 );
                }
            }
        }

        #endregion


        void AddBeaches( Map map ) {
            int beachExtentSqr = (args.beachExtent + 1) * (args.beachExtent + 1);
            for( int x = 0; x < map.widthX; x++ ) {
                for( int y = 0; y < map.widthY; y++ ) {
                    int h = map.SearchColumn( x, y, bGroundSurface );
                    if( h < 0 ) continue;
                    bool found = false;
                    for( int dx = -args.beachExtent; !found && dx <= args.beachExtent; dx++ ) {
                        for( int dy = -args.beachExtent; !found && dy <= args.beachExtent; dy++ ) {
                            for( int dh = -args.beachHeight; !found && dh <= 0; dh++ ) {
                                if( dx * dx + dy * dy + dh * dh > beachExtentSqr ) continue;
                                int xx = x + dx;
                                int yy = y + dy;
                                int hh = h + dh;
                                if( xx < 0 || xx >= map.widthX || yy < 0 || yy >= map.widthY || hh < 0 || hh >= map.height ) continue;
                                byte block = map.GetBlock( xx, yy, hh );
                                if( block == (byte)bWater || block == (byte)bWaterSurface ) {
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    if( found ) {
                        map.SetBlock( x, y, h, bSeaFloor );
                        if( h > 0 && map.GetBlock( x, y, h - 1 ) == (byte)bGround ) map.SetBlock( x, y, h - 1, bSeaFloor );
                    }
                }
            }
        }


        public void GenerateTrees( Map map ) {
            int MinHeight = args.treeHeightMin;
            int MaxHeight = args.treeHeightMax;
            int MinTrunkPadding = args.treeSpacingMin;
            int MaxTrunkPadding = args.treeSpacingMax;
            int TopLayers = 2;
            double Odds = 0.618;
            bool OnlyAir = true;

            Random rn = new Random();
            int nx, ny, nz, nh;
            int radius;

            map.CalculateShadows();

            for( int x = 0; x < map.widthX; x += rn.Next( MinTrunkPadding, MaxTrunkPadding + 1 ) ) {
                for( int y = 0; y < map.widthY; y += rn.Next( MinTrunkPadding, MaxTrunkPadding + 1 ) ) {
                    nx = x + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) + 1 );
                    ny = y + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) + 1 );
                    if( nx < 0 || nx >= map.widthX || ny < 0 || ny >= map.widthY ) continue;
                    nz = map.shadows[nx, ny];

                    if( (map.GetBlock( nx, ny, nz ) == (byte)bGroundSurface) && slopemap[nx, ny] < .5 ) {
                        // Pick a random height for the tree between Min and Max,
                        // discarding this tree if it would breach the top of the map
                        if( (nh = rn.Next( MinHeight, MaxHeight + 1 )) + nz + nh / 2 > map.height )
                            continue;

                        // Generate the trunk of the tree
                        for( int z = 1; z <= nh; z++ )
                            map.SetBlock( nx, ny, nz + z, Block.Log );

                        for( int i = -1; i < nh / 2; i++ ) {
                            // Should we draw thin (2x2) or thicker (4x4) foliage
                            radius = (i >= (nh / 2) - TopLayers) ? 1 : 2;
                            // Draw the foliage
                            for( int xoff = -radius; xoff < radius + 1; xoff++ ) {
                                for( int yoff = -radius; yoff < radius + 1; yoff++ ) {
                                    // Drop random leaves from the edges
                                    if( rn.NextDouble() > Odds && Math.Abs( xoff ) == Math.Abs( yoff ) && Math.Abs( xoff ) == radius )
                                        continue;
                                    // By default only replace an existing block if its air
                                    if( OnlyAir != true || map.GetBlock( nx + xoff, ny + yoff, nz + nh + i ) == (byte)Block.Air )
                                        map.SetBlock( nx + xoff, ny + yoff, nz + nh + i, Block.Leaves );
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion


        #region Themes / Templates

        public void ApplyTheme( MapGenTheme theme ) {
            args.theme = theme;
            switch( theme ) {
                case MapGenTheme.Arctic:
                    bWaterSurface = Block.Glass;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.White;
                    bWater = Block.Water;
                    bGround = Block.White;
                    bSeaFloor = Block.White;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    groundThickness = 1;
                    break;
                case MapGenTheme.Desert:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Sand;
                    bWater = Block.Water;
                    bGround = Block.Sand;
                    bSeaFloor = Block.Sand;
                    bBedrock = Block.Stone;
                    bCliff = Block.Gravel;
                    break;
                case MapGenTheme.Hell:
                    bWaterSurface = Block.Lava;
                    bDeepWaterSurface = Block.Lava;
                    bGroundSurface = Block.Obsidian;
                    bWater = Block.Lava;
                    bGround = Block.Stone;
                    bSeaFloor = Block.Obsidian;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
                case MapGenTheme.Forest:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Grass;
                    bWater = Block.Water;
                    bGround = Block.Dirt;
                    bSeaFloor = Block.Sand;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
                case MapGenTheme.Swamp:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Dirt;
                    bWater = Block.Water;
                    bGround = Block.Dirt;
                    bSeaFloor = Block.Leaves;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
            }
        }


        public static MapGeneratorArgs MakeTemplate( MapGenTemplate template ) {
            switch( template ) {
                case MapGenTemplate.Archipelago:
                    return new MapGeneratorArgs {
                        maxHeight = 8,
                        maxDepth = 20,
                        featureScale = 3,
                        roughness = .46f,
                        matchWaterCoverage = true,
                        waterCoverage = .85f
                    };

                case MapGenTemplate.Atoll:
                    return new MapGeneratorArgs {
                        theme = MapGenTheme.Desert,
                        maxHeight = 2,
                        maxDepth = 39,
                        useBias = true,
                        bias = .9f,
                        midPoint = 1,
                        loweredCorners = 4,
                        featureScale = 2,
                        detailScale = 5,
                        marbledHeightmap = true,
                        invertHeightmap = true,
                        matchWaterCoverage = true,
                        waterCoverage = .95f
                    };

                case MapGenTemplate.Bay:
                    return new MapGeneratorArgs {
                        maxHeight = 22,
                        maxDepth = 12,
                        useBias = true,
                        bias = 1,
                        midPoint = -1,
                        raisedCorners = 3,
                        loweredCorners = 1,
                        treeSpacingMax = 12,
                        treeSpacingMin = 6,
                        marbledHeightmap = true,
                        delayBias = true
                    };

                case MapGenTemplate.Default:
                    return new MapGeneratorArgs();

                case MapGenTemplate.Dunes:
                    return new MapGeneratorArgs {
                        addTrees = false,
                        addWater = false,
                        theme = MapGenTheme.Desert,
                        maxHeight = 12,
                        maxDepth = 7,
                        featureScale = 2,
                        detailScale = 3,
                        roughness = .44f,
                        marbledHeightmap = true,
                        invertHeightmap = true
                    };

                case MapGenTemplate.Hills:
                    return new MapGeneratorArgs {
                        addWater = false,
                        maxHeight = 8,
                        maxDepth = 8,
                        featureScale = 2,
                        treeSpacingMin = 7,
                        treeSpacingMax = 13
                    };

                case MapGenTemplate.Ice:
                    return new MapGeneratorArgs {
                        addTrees = false,
                        theme = MapGenTheme.Arctic,
                        maxHeight = 2,
                        maxDepth = 2032,
                        featureScale = 2,
                        detailScale = 7,
                        roughness = .64f,
                        marbledHeightmap = true,
                        matchWaterCoverage = true,
                        waterCoverage = .3f,
                        maxHeightVariation = 0
                    };

                case MapGenTemplate.Island:
                    return new MapGeneratorArgs {
                        maxHeight = 16,
                        maxDepth = 39,
                        useBias = true,
                        bias = .7f,
                        midPoint = 1,
                        loweredCorners = 4,
                        featureScale = 3,
                        detailScale = 7,
                        marbledHeightmap = true,
                        delayBias = true
                    };

                case MapGenTemplate.Lake:
                    return new MapGeneratorArgs {
                        maxHeight = 14,
                        maxDepth = 20,
                        useBias = true,
                        bias = .65f,
                        midPoint = -1,
                        raisedCorners = 4,
                        featureScale = 2,
                        roughness = .56f,
                        matchWaterCoverage = true,
                        waterCoverage = .3f
                    };

                case MapGenTemplate.Mountains:
                    return new MapGeneratorArgs {
                        addWater = false,
                        maxHeight = 40,
                        maxDepth = 10,
                        featureScale = 1,
                        detailScale = 7,
                        marbledHeightmap = true
                    };

                case MapGenTemplate.River:
                    return new MapGeneratorArgs {
                        maxHeight = 22,
                        maxDepth = 8,
                        featureScale = 0,
                        detailScale = 6,
                        marbledHeightmap = true,
                        matchWaterCoverage = true,
                        waterCoverage = .31f
                    };

                case MapGenTemplate.Streams:
                    return new MapGeneratorArgs {
                        maxHeight = 5,
                        maxDepth = 4,
                        featureScale = 2,
                        detailScale = 7,
                        roughness = .55f,
                        marbledHeightmap = true,
                        matchWaterCoverage = true,
                        waterCoverage = .25f,
                        treeSpacingMin = 8,
                        treeSpacingMax = 14
                    };

                case MapGenTemplate.Peninsula:
                    return new MapGeneratorArgs {
                        maxHeight = 22,
                        maxDepth = 12,
                        useBias = true,
                        bias = .5f,
                        midPoint = -1,
                        raisedCorners = 3,
                        loweredCorners = 1,
                        treeSpacingMax = 12,
                        treeSpacingMin = 6,
                        invertHeightmap = true,
                        waterCoverage = .5f
                    };

                case MapGenTemplate.Flat:
                    return new MapGeneratorArgs {
                        maxHeight = 0,
                        maxDepth = 0,
                        maxHeightVariation = 0,
                        addWater = false,
                        detailScale = 0,
                        featureScale = 0,
                        addCliffs = false
                    };
            }
            return null; // can never happen
        }

        #endregion


        #region Erosion

        void Erode( Map map ) {
            float[] volume = new float[map.blocks.Length];
            for( int i = 0; i < map.blocks.Length; i++ ) {
                if( map.blocks[i] != (byte)Block.Air ) {
                    volume[i] = 0.5f;
                }
            }
            ErosionPass( volume, map );
        }


        void ErosionPass( float[] volume, Map map ) {
            for( int x = 0; x < map.widthX; x+=2 ) {
                for( int y = 0; y < map.widthY; y+=2 ) {
                    ErosionDrop( x, y, volume, map );
                }
            }
            for( int x = 1; x < map.widthX; x += 2 ) {
                for( int y = 1; y < map.widthY; y += 2 ) {
                    ErosionDrop( x, y, volume, map );
                }
            }
        }

        //float TerminalVelocity

        void ErosionDrop( int x, int y, float[] volume, Map map ) {
            WaterParticle particle = new WaterParticle() {
                x = x,
                y = y,
                h = map.height,
                mass = .5f,
                deposit = 0,
                velocity = 0
            };
        }


        class WaterParticle {
            public int x, y, h;
            public float velocity, mass, deposit;
        }

        #endregion
    }
}