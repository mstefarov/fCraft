// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;


namespace fCraft {

    // TODO: themes
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
        River
    }

    public class MapGenerator {
        double roughness, gBigSize, smoothingOver, smoothingUnder, midpoint, sidesMin, sidesMax;
        Random rand = new Random();
        Map map;
        Player player;
        string fileName;
        int groundThickness = 5, seaFloorThickness = 3;

        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface;
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
                    break;
                case MapGenTheme.Hell:
                    bWaterSurface = Block.Lava;
                    bDeepWaterSurface = Block.Lava;
                    bGroundSurface = Block.Rocks;
                    bWater = Block.Lava;
                    bGround = Block.Stone;
                    bSeaFloor = Block.Obsidian;
                    bBedrock = Block.Stone;
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
                    break;
                case MapGenTheme.Rocky:
                    bWaterSurface = Block.Water;
                    bDeepWaterSurface = Block.Water;
                    bGroundSurface = Block.Rocks;
                    bWater = Block.Water;
                    bGround = Block.Stone;
                    bSeaFloor = Block.Rocks;
                    bBedrock = Block.Stone;
                    break;
            }
        }


        public void Generate() {
            ApplyType();
            ApplyTheme();

            double[,] heightmap = GenerateHeightmap( map.widthX, map.widthY );

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
                        heightmap[x, y] = Math.Abs( (heightmap[x, y] - min) / (max - min) * 2 - 1 ) * .3 + .4;
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
                        map.SetBlock( x, y, ilevel, bGroundSurface );
                        for( int i = ilevel - 1; i > 0; i-- ) {
                            if( ilevel - i < groundThickness ) {
                                map.SetBlock( x, y, i, bGround );
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
                        for( int i = ilevel; i > 0; i-- ) {
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
                player.Message( "Map generation: " + message );
            }
        }


        double[,] GenerateHeightmap( int iWidth, int iHeight ) {
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

            gBigSize = iWidth + iHeight;
            DivideGrid( ref points, 0, 0, iWidth, iHeight, sidesMin + sides[0], sidesMin + sides[1], sidesMin + sides[2], sidesMin + sides[3], true );
            return points;
        }


        void DivideGrid( ref double[,] points, double x, double y, int width, int height, double c1, double c2, double c3, double c4, bool isTop ) {
            double Edge1, Edge2, Edge3, Edge4, Middle;

            int newWidth = width / 2;
            int newHeight = height / 2;

            if( width > 1 || height > 1 ) {
                if( isTop ) {
                    Middle = ((c1 + c2 + c3 + c4) / 4) + midpoint; // Randomly displace the midpoint!
                } else {
                    Middle = ((c1 + c2 + c3 + c4) / 4) + Displace( newWidth + newHeight ); // Randomly displace the midpoint!
                }
                Edge1 = ((c1 + c2) / 2); //Calculate the edges by averaging the two corners of each edge.
                Edge2 = ((c2 + c3) / 2);
                Edge3 = ((c3 + c4) / 2);
                Edge4 = ((c4 + c1) / 2);
                // Make sure that the midpoint doesn't accidentally "randomly displaced" past the boundaries!
                Middle = Rectify( Middle );
                Edge1 = Rectify( Edge1 );
                Edge2 = Rectify( Edge2 );
                Edge3 = Rectify( Edge3 );
                Edge4 = Rectify( Edge4 );
                // Do the operation over again for each of the four new grids.
                DivideGrid( ref points, x, y, newWidth, newHeight, c1, Edge1, Middle, Edge4, false );
                DivideGrid( ref points, x + newWidth, y, width - newWidth, newHeight, Edge1, c2, Edge2, Middle, false );
                if( isTop ) Feedback( "Heightmap: 50%" );
                DivideGrid( ref points, x + newWidth, y + newHeight, width - newWidth, height - newHeight, Middle, Edge2, c3, Edge3, false );
                DivideGrid( ref points, x, y + newHeight, newWidth, height - newHeight, Edge4, Middle, Edge3, c4, false );
                if( isTop ) Feedback( "Heightmap: 100%" );
            } else {
                // This is the "base case," where each grid piece is less than the size of a pixel.
                // The four corners of the grid piece will be averaged and drawn as a single pixel.
                double c = (c1 + c2 + c3 + c4) / 4;

                points[(int)(x), (int)(y)] = c;
                if( width == 2 ) {
                    points[(int)(x + 1), (int)(y)] = c;
                }
                if( height == 2 ) {
                    points[(int)(x), (int)(y + 1)] = c;
                }
                if( (width == 2) && (height == 2) ) {
                    points[(int)(x + 1), (int)(y + 1)] = c;
                }
            }
        }


        double Rectify( double iNum ) {
            if( iNum < 0 ) {
                iNum = 0;
            } else if( iNum > 1.0 ) {
                iNum = 1.0;
            }
            return iNum;
        }


        double Displace( double SmallSize ) {
            double Max = SmallSize / gBigSize * roughness;
            return (rand.NextDouble() - 0.5) * Max;
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
            int MinTrunkPadding = 5;
            int MaxTrunkPadding = 10;
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
    }
}