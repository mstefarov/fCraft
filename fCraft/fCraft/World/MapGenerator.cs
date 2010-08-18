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
        public int seed, maxHeight, maxDepth;

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
        double roughness, gBigSize, smoothingOver, smoothingUnder, midpoint, sidesMin, sidesMax;
        Random rand = new Random();
        Map map;
        Player player;
        string fileName;
        int groundThickness = 5, seaFloorThickness = 3;

        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface, bCliff;
        MapGenType type;
        MapGenTheme theme;

        public MapGenerator( Map _map, Player _player, string _fileName, MapGenType _type, MapGenTheme _theme ) {
            map = _map;
            player = _player;
            fileName = _fileName;
            type = _type;
            theme = _theme;
        }


        public void SetParams( double _roughness, double _smoothingOver, double _smoothingUnder, double _midpoint, double _sidesMin, double _sidesMax ) {
            roughness = _roughness;
            smoothingOver = _smoothingOver;
            smoothingUnder = _smoothingUnder;
            midpoint = _midpoint;
            sidesMin = _sidesMin;
            sidesMax = _sidesMax;
        }


        void ApplyType() {
            switch( type ) {
                case MapGenType.Hills:
                    SetParams( 1, 1, 1.5, 0, 0.52, 0.6 );
                    break;
                case MapGenType.Mountains:
                    SetParams( 4, 1, 0.4, 0.1, 0.5, 0.7 );
                    break;
                case MapGenType.Cliffs:
                    SetParams( 3, 1.2, 0.6, 0, 0.55, 0.7 );
                    break;
                case MapGenType.Lake:
                    SetParams( 1, 0.6, 0.9, -0.3, 0.53, 0.6 );
                    break;
                case MapGenType.Island:
                    SetParams( 1, 0.6, 0.9, 0.3, 0.30, 0.45 );
                    break;
                case MapGenType.Coast:
                    SetParams( 1.5, 0.75, 1, 0, 0.4, 0.63 );
                    break;
                case MapGenType.River:
                    SetParams( 3, 1, 1, -.1, 0, 1 );
                    break;
            }
        }

        void ApplyTheme() {
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


        public void Generate() {
            ApplyType();
            ApplyTheme();

            roughness = 0.5;

            float[,] heightmap = GenerateHeightmap( map.widthX, map.widthY );
            float[,] blendmap = null;

            if( type == MapGenType.River ) {
                double min = double.MaxValue, max = double.MinValue;
                for( int x = 0; x < map.widthX; x++ ) {
                    for( int y = 0; y < map.widthY; y++ ) {
                        min = Math.Min( min, heightmap[x, y] );
                        max = Math.Max( max, heightmap[x, y] );
                    }
                }
                for( int x = 0; x < map.widthX; x++ ) {
                    for( int y = 0; y < map.widthY; y++ ) {
                        heightmap[x, y] = (float)(Math.Abs( (heightmap[x, y] - min) / (max - min) * 2 - 1 ) * .3 + .4);
                    }
                }
            } else if( type == MapGenType.Cliffs ) {
                groundThickness = Math.Max( 1, groundThickness / 2 );
                float[,] heightmap1 = heightmap;
                SetParams( 3, 1.2, 0.6, -.05, 0.4, 0.55 );
                float[,] heightmap2 = GenerateHeightmap( map.widthX, map.widthY );
                SetParams( 4, 1, 1, 0, 0.5, 0.5 );
                blendmap = GenerateHeightmap( map.widthX, map.widthY );
                double min = double.MaxValue, max = double.MinValue;
                for( int x = 0; x < map.widthX; x++ ) {
                    for( int y = 0; y < map.widthY; y++ ) {
                        min = Math.Min( min, blendmap[x, y] );
                        max = Math.Max( max, blendmap[x, y] );
                    }
                }
                double steepness = Math.Max( map.widthX, map.widthY ) / 5;
                for( int x = 0; x < map.widthX; x++ ) {
                    for( int y = 0; y < map.widthY; y++ ) {
                        blendmap[x, y] = (float)Math.Min( 1, Math.Max( 0, (heightmap[x, y] - min) / (max - min) * steepness * 2 - steepness ) );
                    }
                }
                for( int x = 0; x < map.widthX; x++ ) {
                    for( int y = 0; y < map.widthY; y++ ) {
                        heightmap[x, y] = heightmap1[x, y] * blendmap[x, y] + heightmap2[x, y] * (1 - blendmap[x, y]);
                    }
                }
            }

            double level;
            int ilevel, iwater;
            Feedback( "Filling..." );
            iwater = map.height / 2;

            // TODO: slope estimation

            for( int x = 0; x < map.widthX; x++ ) {
                for( int y = 0; y < map.widthY; y++ ) {
                    level = heightmap[x, y];
                    ilevel = (int)(level * map.height);
                    if( ilevel > iwater ) {
                        ilevel = (int)(((level - 0.5) * smoothingOver + 0.5) * map.height);
                        if( blendmap != null && blendmap[x, y] > .25 && blendmap[x, y] < .75 ) {
                            map.SetBlock( x, y, ilevel, bCliff );
                        } else {
                            map.SetBlock( x, y, ilevel, bGroundSurface );
                        }
                        for( int i = ilevel - 1; i >= 0; i-- ) {
                            if( ilevel - i < groundThickness ) {
                                if( blendmap != null && blendmap[x, y] > .01 && blendmap[x, y] < .99 ) {
                                    map.SetBlock( x, y, i, bCliff );
                                } else {
                                    map.SetBlock( x, y, i, bGround );
                                }
                            } else {
                                map.SetBlock( x, y, i, bBedrock );
                            }
                        }
                    } else {
                        ilevel = (int)(((level - 0.5) * smoothingUnder + 0.5) * map.height);
                        if( iwater - ilevel > 3 ) {
                            map.SetBlock( x, y, iwater, bDeepWaterSurface );
                        } else {
                            map.SetBlock( x, y, iwater, bWaterSurface );
                        }
                        for( int i = iwater; i > ilevel; i-- ) {
                            map.SetBlock( x, y, i, bWater );
                        }
                        for( int i = ilevel; i >= 0; i-- ) {
                            if( ilevel - i < seaFloorThickness ) {
                                map.SetBlock( x, y, i, bSeaFloor );
                            } else {
                                map.SetBlock( x, y, i, bBedrock );
                            }
                        }
                    }
                }
            }
            if( theme == MapGenTheme.Forest ) {
                GenerateTrees( map );
            }

            map.ResetSpawn();

            Feedback( "Generation done." );
        }


        public static void GenerationTask( object task ) {
            MapGenerator gen = (MapGenerator)task;
            gen.Generate();
            gen.map.Save( gen.fileName );
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }


        void Feedback( string message ) {
            if( player != null ) {
                player.Message( "Map generation: {0}", message );
            }
        }


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


        public static void GenerateTrees( Map map ) {
            int MinHeight = 4;
            int MaxHeight = 6;
            int MinTrunkPadding = 6;
            int MaxTrunkPadding = 11;
            int BorderPadding = 4;
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