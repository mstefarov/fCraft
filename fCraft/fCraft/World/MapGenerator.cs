// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {

    // TODO: themes
    enum MapGenTheme {
        Normal,
        Rocky,
        Desert,
        Hell,
        Tundra,
        Arctic
    }

    class MapGenerator {
        double roughness, gBigSize, smoothingOver, smoothingUnder, water, midpoint, sides;
        Random rand;
        Map map;
        Player player;
        string filename;
        bool hollow;

        public MapGenerator( Random _rand, Map _map, Player _player, string _filename, double _roughness, double _smoothingOver, double _smoothingUnder, double _water, double _midpoint, double _sides, bool _hollow ) {
            rand = _rand;
            map = _map;
            player = _player;
            filename = _filename;
            roughness = _roughness;
            smoothingOver = _smoothingOver;
            smoothingUnder = _smoothingUnder;
            midpoint = _midpoint;
            sides = _sides;
            water = _water;
            hollow = _hollow;
        }

        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock;

        public void Generate() {
            double[,] heightmap = GenerateHeightmap( map.widthX, map.widthY );
            double level;
            int ilevel, iwater;
            Feedback( "Filling..." );

            // TODO: slope estimation
            bWaterSurface = Block.Water;
            bGroundSurface = Block.Grass;
            bWater = Block.Water;
            bGround = Block.Dirt;
            bSeaFloor = Block.Sand;
            bBedrock = Block.Stone;

            for( int x = 0; x < map.widthX; x++ ) {
                for( int y = 0; y < map.widthY; y++ ) {
                    level = heightmap[x, y];
                    ilevel = (int)(level * map.height);
                    iwater = (int)(water * map.height);
                    if( ilevel > iwater ) {
                        ilevel = (int)(((level - water) * smoothingOver + water) * map.height);
                        map.SetBlock( x, y, ilevel, bGroundSurface );
                        if( !hollow ) {
                            for( int i = ilevel - 1; i > 0; i-- ) {
                                if( ilevel - i < 5 ) {
                                    map.SetBlock( x, y, i, bGround );
                                } else {
                                    map.SetBlock( x, y, i, bBedrock );
                                }
                            }
                        }
                    } else {
                        ilevel = (int)(((level - water) * smoothingUnder + water) * map.height);
                        map.SetBlock( x, y, iwater, bWaterSurface );
                        if( !hollow ) {
                            for( int i = iwater - 1; i > ilevel; i-- ) {
                                map.SetBlock( x, y, i, bWater );
                            }
                        }
                        map.SetBlock( x, y, ilevel, bSeaFloor );
                        if( !hollow ) {
                            for( int i = ilevel - 1; i > 0; i-- ) {
                                map.SetBlock( x, y, i, bBedrock );
                            }
                        }
                    }
                }
            }
            map.MakeFloodBarrier();
            map.Save( filename );
            Feedback( "Done." );
        }

        public static void GenerationTask( object task ) {
            ((MapGenerator)task).Generate();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Optimized );
        }

        void Feedback( string message ) {
            player.Message( "Map generation: " + message );
        }

        double[,] GenerateHeightmap( int iWidth, int iHeight ) {
            double c1, c2, c3, c4;
            double[,] points = new double[iWidth + 1, iHeight + 1];

            //Assign the four corners of the intial grid random color values
            //These will end up being the colors of the four corners
            c1 = sides + (rand.NextDouble() - 0.5) * 0.05;
            c2 = sides + (rand.NextDouble() - 0.5) * 0.05;
            c3 = sides + (rand.NextDouble() - 0.5) * 0.05;
            c4 = sides + (rand.NextDouble() - 0.5) * 0.05;
            gBigSize = iWidth + iHeight;
            DivideGrid( ref points, 0, 0, iWidth, iHeight, c1, c2, c3, c4, true );
            return points;
        }


        public void DivideGrid( ref double[,] points, double x, double y, int width, int height, double c1, double c2, double c3, double c4, bool isTop ) {
            double Edge1, Edge2, Edge3, Edge4, Middle;

            int newWidth = width / 2;
            int newHeight = height / 2;

            if( width > 1 || height > 1 ) {
                if( isTop ) {
                    Middle = ((c1 + c2 + c3 + c4) / 4) + midpoint;	//Randomly displace the midpoint!
                } else {
                    Middle = ((c1 + c2 + c3 + c4) / 4) + Displace( newWidth + newHeight );	//Randomly displace the midpoint!
                }
                Edge1 = ((c1 + c2) / 2);	//Calculate the edges by averaging the two corners of each edge.
                Edge2 = ((c2 + c3) / 2);
                Edge3 = ((c3 + c4) / 2);
                Edge4 = ((c4 + c1) / 2);//
                //Make sure that the midpoint doesn't accidentally "randomly displaced" past the boundaries!
                Middle = Rectify( Middle );
                Edge1 = Rectify( Edge1 );
                Edge2 = Rectify( Edge2 );
                Edge3 = Rectify( Edge3 );
                Edge4 = Rectify( Edge4 );
                //Do the operation over again for each of the four new grids.
                DivideGrid( ref points, x, y, newWidth, newHeight, c1, Edge1, Middle, Edge4, false );
                DivideGrid( ref points, x + newWidth, y, width - newWidth, newHeight, Edge1, c2, Edge2, Middle, false );
                if( isTop ) Feedback( "Heightmap: 50%" );
                DivideGrid( ref points, x + newWidth, y + newHeight, width - newWidth, height - newHeight, Middle, Edge2, c3, Edge3, false );
                DivideGrid( ref points, x, y + newHeight, newWidth, height - newHeight, Edge4, Middle, Edge3, c4, false );
                if( isTop ) Feedback( "Heightmap: 100%" );
            } else {
                //This is the "base case," where each grid piece is less than the size of a pixel.
                //The four corners of the grid piece will be averaged and drawn as a single pixel.
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
    }
}