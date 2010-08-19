// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;


namespace fCraft {

    public enum MapGenTheme {
        Arctic,
        Desert,
        Forest,
        Hell,
        Normal,
        Rocky
    }

    public enum MapGenType {
        Mountains,
        Hills,
        Lake,
        Island,
        Coast,
        River,
        Cliffs
    }


    public sealed class MapGeneratorArgs {
        public MapGenTheme theme;
        public int seed, dimX, dimY, dimH, maxHeight, maxDepth, waterLevel;
        public bool useAbsoluteHeight;

        public bool matchWaterCoverage;
        public float waterCoverage;
        public bool useBias;
        public float cornerBiasMin, cornerBiasMax, midpointBias;

        public int detailSize;
        public float roughness;
        public bool layeredHeightmap, marbled;

        public bool placeTrees;
        public int treeSpacingMin, treeSpacingMax, treeHeightMin, treeHeightMax;
    }


    public sealed class MapGenerator {
        MapGeneratorArgs args;
        Random rand;
        Noise noise;
        float[,] heightmap, blendmap;

        // theme-dependent vars
        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface, bCliff;
        int groundThickness = 5, seaFloorThickness = 3;


        public MapGenerator( MapGeneratorArgs _args ) {
            args = _args;
            rand = new Random( args.seed );
            noise = new Noise( rand );
            ApplyTheme( args.theme );
        }


        public void GenerateHeightmap() {
            // TODO: bias
            heightmap = noise.PerlinMap( args.dimX, args.dimY, args.detailSize, args.roughness );
            Noise.Normalize( heightmap );

            if( args.layeredHeightmap ) {
                // needs a new Noise object to randomize second map
                float[,] heightmap2 = new Noise( rand ).PerlinMap( args.dimX, args.dimY, args.detailSize, args.roughness );
                Noise.Normalize( heightmap2 );
                blendmap = new Noise( rand ).PerlinMap( args.dimX, args.dimY, args.detailSize, args.roughness );
                Noise.Normalize( blendmap );
                Noise.Blend( heightmap, heightmap2, blendmap );
            }

            if( args.marbled ) {
                Noise.Marble( heightmap );
            }
        }


        // assumes normalzied heightmap
        public static float MatchWaterCoverage( float[,] heightmap, float desiredWaterCoverage ) {
            if( desiredWaterCoverage == 0 ) return 0;
            if( desiredWaterCoverage == 1 ) return 1;
            float waterLevel = 0.5f;
            for( int i = 0; i < 8; i++ ) {
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


        public Map Generate() {
            GenerateHeightmap();
            return GenerateMap();
        }

        public Map GenerateMap() {
            Map map = new Map( null, args.dimX, args.dimY, args.dimH );
            args.waterLevel = (map.height-1)/2;

            float desiredWaterLevel = .5f;
            if( args.matchWaterCoverage ) {
                desiredWaterLevel = MatchWaterCoverage( heightmap, args.waterCoverage );
            }

            float underWaterMultiplier = args.maxDepth / desiredWaterLevel;
            float aboveWaterMultiplier = args.maxHeight / (1 - desiredWaterLevel);

            int level;
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( heightmap[x, y] < desiredWaterLevel ) {
                        level = args.waterLevel - (int)Math.Round( (1 - heightmap[x, y] / desiredWaterLevel) * args.maxDepth );

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
                        level = args.waterLevel + (int)Math.Round( (heightmap[x, y] - desiredWaterLevel) * aboveWaterMultiplier );

                        if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                            map.SetBlock( x, y, level, bCliff );
                        } else {
                            map.SetBlock( x, y, level, bGroundSurface );
                        }
                        for( int i = level - 1; i >= 0; i-- ) {
                            if( level - i < groundThickness ) {
                                if( blendmap != null && blendmap[x, y] > .01 && blendmap[x, y] < .99 ) {
                                    map.SetBlock( x, y, i, bCliff );
                                } else {
                                    map.SetBlock( x, y, i, bGround );
                                }
                            } else {
                                map.SetBlock( x, y, i, bBedrock );
                            }
                        }
                    }
                }
            }

            if( args.placeTrees ) {
                GenerateTrees( map );
            }

            map.ResetSpawn();
            return map;
        }

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
                    bWater = Block.Air;
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
                    bCliff = Block.Rocks;
                    break;
                case MapGenTheme.Forest:
                case MapGenTheme.Normal:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Grass;
                    bWater = Block.Water;
                    bGround = Block.Dirt;
                    bSeaFloor = Block.Sand;
                    bBedrock = Block.Stone;
                    bCliff = Block.Rocks;
                    break;
                case MapGenTheme.Rocky:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Rocks;
                    bWater = Block.Water;
                    bGround = Block.Stone;
                    bSeaFloor = Block.Rocks;
                    bBedrock = Block.Stone;
                    bCliff = Block.Stone;
                    break;
            }
        }

        public static void GenerationTask( object task ) {
            MapGenerator gen = (MapGenerator)task;
            gen.Generate();
            //gen.map.Save( gen.fileName );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
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
            map.ResetSpawn();
        }


        public void GenerateTrees( Map map ) {
            int MinHeight = args.treeHeightMin;
            int MaxHeight = args.treeHeightMax;
            int MinTrunkPadding = args.treeSpacingMin;
            int MaxTrunkPadding = args.treeSpacingMax;
            int BorderPadding = MinTrunkPadding;
            int TopLayers = 2;
            double Odds = 0.618;
            bool OnlyAir = true;

            Random rn = new Random();
            int nx, ny, nz, nh;
            int radius;

            map.CalculateShadows();

            for( int x = BorderPadding; x < map.widthX - BorderPadding; x += rn.Next( MinTrunkPadding, MaxTrunkPadding ) ) {
                for( int y = BorderPadding; y < map.widthY - BorderPadding; y += rn.Next( MinTrunkPadding, MaxTrunkPadding ) ) {
                    nx = x + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) );
                    ny = y + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) );
                    nz = map.shadows[nx, ny];

                    if( map.GetBlock( nx, ny, nz ) == (byte)Block.Grass ) {
                        // Pick a random height for the tree between Min and Max,
                        // discarding this tree if it would breach the top of the map
                        if( (nh = rn.Next( MinHeight, MaxHeight )) + nz + nh / 2 > map.height )
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



        public static MapGeneratorArgs MakePreset( MapGenType preset ) {
            switch( preset ) {
                case MapGenType.Cliffs:
                    return new MapGeneratorArgs {
                        cornerBiasMax = 0.2f,
                        cornerBiasMin = 0.05f,
                        detailSize = 1,
                        layeredHeightmap = true,
                        midpointBias = 0,
                        roughness = .6f,
                        useBias = true
                    };
                case MapGenType.Coast:
                    return new MapGeneratorArgs {
                        cornerBiasMax = -0.1f,
                        cornerBiasMin = 0.13f,
                        detailSize = 1,
                        matchWaterCoverage = true,
                        midpointBias = 0,
                        roughness = .5f,
                        useBias = true,
                        waterCoverage = .6f
                    };
                case MapGenType.Hills:
                    return new MapGeneratorArgs {
                        detailSize = 2,
                        matchWaterCoverage = true,
                        roughness = .45f,
                        waterCoverage = .6f
                    };
                case MapGenType.Island:
                    return new MapGeneratorArgs {
                        cornerBiasMax = -0.05f,
                        cornerBiasMin = -0.2f,
                        detailSize = 2,
                        matchWaterCoverage = true,
                        midpointBias = .3f,
                        roughness = .5f,
                        useBias = true,
                        waterCoverage = .75f
                    };
                case MapGenType.Lake:
                    return new MapGeneratorArgs {
                        cornerBiasMax = .03f,
                        cornerBiasMin = .1f,
                        detailSize = 2,
                        matchWaterCoverage = true,
                        midpointBias = -.3f,
                        roughness = .5f,
                        useBias = true,
                        waterCoverage = .25f
                    };
                case MapGenType.Mountains:
                    return new MapGeneratorArgs {
                        detailSize = 1,
                        layeredHeightmap = true,
                        matchWaterCoverage = true,
                        roughness = .6f,
                        waterCoverage = .6f
                    };
                case MapGenType.River:
                    return new MapGeneratorArgs {
                        detailSize = 1,
                        marbled = true,
                        roughness = .5f,
                    };
            }
            return null; // can never happen
        }
    }
}

    /*

        float[,] GenerateHeightmap( int iWidth, int iHeight ) {
            Noise theNoise = new Noise( rand );
            int octaves = (int)Math.Log( Math.Max( iWidth, iHeight ), 2 );
            float[,] map = theNoise.PerlinMap( iWidth, iHeight, octaves, (float)roughness );
            Noise.Normalize( map, (float)sidesMin, (float)sidesMax );
            return map;

            double[,] points = new double[iWidth + 1, iHeight + 1];

            double sideDelta = (sidesMax - sidesMin);
            double[] sides = new double[4];
            if( type == MapGenType.River ) {
                sides[0] = rand.NextDouble() * .5;
                sides[1] = rand.NextDouble() * .5;
                sides[2] = rand.NextDouble() * .5 + .5;
                sides[3] = rand.NextDouble() * .5 + .5;
                sides = sides.OrderBy( r => rand.Next() ).ToArray();
            } else {
                sides[0] = rand.NextDouble() * sideDelta;
                sides[1] = rand.NextDouble() * sideDelta;
                sides[2] = rand.NextDouble() * sideDelta;
                while( (sides[0] < sideDelta / 2 && sides[1] < sideDelta / 2 && sides[2] < sideDelta / 2 && sides[3] < sideDelta / 2) ||
                    (sides[0] > sideDelta / 2 && sides[1] > sideDelta / 2 && sides[2] > sideDelta / 2 && sides[3] > sideDelta / 2) ) {
                    sides[3] = rand.NextDouble() * sideDelta;
                }
            }
        }*/
