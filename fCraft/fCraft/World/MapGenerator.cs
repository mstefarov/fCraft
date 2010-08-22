// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;


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
        public bool addWater;

        public bool matchWaterCoverage;
        public float waterCoverage;
        public int raisedCorners, loweredCorners, midPoint;
        public float bias;
        public bool useBias;

        public int minDetailSize, maxDetailSize;
        public float roughness;
        public bool layeredHeightmap, marbledHeightmap, invertHeightmap;

        public bool placeTrees;
        public int treeSpacingMin, treeSpacingMax, treeHeightMin, treeHeightMax;

        public void Validate() {
            if( raisedCorners < 0 || raisedCorners > 4 || loweredCorners < 0 || raisedCorners > 4 || raisedCorners + loweredCorners > 4 ) {
                throw new ArgumentException( "raisedCorners and loweredCorners must be between 0 and 4." );
            }
        }

        public MapGeneratorArgs(){}

        public MapGeneratorArgs( string fileName ) {
            XDocument doc = XDocument.Load( fileName );
            XElement root = doc.Root;

            theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), root.Element( "theme" ).Value );
            seed = Int32.Parse( root.Element( "seed" ).Value );
            dimX = Int32.Parse( root.Element( "dimX" ).Value );
            dimY = Int32.Parse( root.Element( "dimY" ).Value );
            dimH = Int32.Parse( root.Element( "dimH" ).Value );
            maxHeight = Int32.Parse( root.Element( "maxHeight" ).Value );
            maxDepth = Int32.Parse( root.Element( "maxDepth" ).Value );
            waterLevel = Int32.Parse( root.Element( "waterLevel" ).Value );
            addWater = Boolean.Parse( root.Element( "addWater" ).Value );

            matchWaterCoverage = Boolean.Parse( root.Element( "matchWaterCoverage" ).Value );
            waterCoverage = float.Parse( root.Element( "waterCoverage" ).Value );
            raisedCorners = Int32.Parse( root.Element( "raisedCorners" ).Value );
            loweredCorners = Int32.Parse( root.Element( "loweredCorners" ).Value );
            midPoint = Int32.Parse( root.Element( "midPoint" ).Value );
            bias = float.Parse( root.Element( "bias" ).Value );
            useBias = Boolean.Parse( root.Element( "useBias" ).Value );

            minDetailSize = Int32.Parse( root.Element( "minDetailSize" ).Value );
            maxDetailSize = Int32.Parse( root.Element( "maxDetailSize" ).Value );
            roughness = float.Parse( root.Element( "roughness" ).Value );
            layeredHeightmap = Boolean.Parse( root.Element( "layeredHeightmap" ).Value );
            marbledHeightmap = Boolean.Parse( root.Element( "marbledHeightmap" ).Value );
            invertHeightmap = Boolean.Parse( root.Element( "invertHeightmap" ).Value );

            placeTrees = Boolean.Parse( root.Element( "placeTrees" ).Value );
            treeSpacingMin = Int32.Parse( root.Element( "treeSpacingMin" ).Value );
            treeSpacingMax = Int32.Parse( root.Element( "treeSpacingMax" ).Value );
            treeHeightMin = Int32.Parse( root.Element( "treeHeightMin" ).Value );
            treeHeightMax = Int32.Parse( root.Element( "treeHeightMax" ).Value );

            Validate();
        }

        const string RootTagName = "fCraftMapGeneratorArgs";
        public void Save( string fileName ) {
            XDocument document = new XDocument();
            XElement root = new XElement( RootTagName );

            root.Add( new XElement( "theme", theme ) );
            root.Add( new XElement( "seed", seed ) );
            root.Add( new XElement( "dimX", dimX ) );
            root.Add( new XElement( "dimY", dimY ) );
            root.Add( new XElement( "dimH", dimH ) );
            root.Add( new XElement( "maxHeight", maxHeight ) );
            root.Add( new XElement( "maxDepth", maxDepth ) );
            root.Add( new XElement( "waterLevel", waterLevel ) );
            root.Add( new XElement( "addWater", addWater ) );

            root.Add( new XElement( "matchWaterCoverage", matchWaterCoverage ) );
            root.Add( new XElement( "waterCoverage", waterCoverage ) );
            root.Add( new XElement( "raisedCorners", raisedCorners ) );
            root.Add( new XElement( "loweredCorners", loweredCorners ) );
            root.Add( new XElement( "midPoint", midPoint ) );
            root.Add( new XElement( "bias", bias ) );
            root.Add( new XElement( "useBias", useBias ) );

            root.Add( new XElement( "minDetailSize", minDetailSize ) );
            root.Add( new XElement( "maxDetailSize", maxDetailSize ) );
            root.Add( new XElement( "roughness", roughness ) );
            root.Add( new XElement( "layeredHeightmap", layeredHeightmap ) );
            root.Add( new XElement( "marbledHeightmap", marbledHeightmap ) );
            root.Add( new XElement( "invertHeightmap", invertHeightmap ) );

            root.Add( new XElement( "placeTrees", placeTrees ) );
            root.Add( new XElement( "treeSpacingMin", treeSpacingMin ) );
            root.Add( new XElement( "treeSpacingMax", treeSpacingMax ) );
            root.Add( new XElement( "treeHeightMin", treeHeightMin ) );
            root.Add( new XElement( "treeHeightMax", treeHeightMax ) );

            document.Add( root );
            document.Save( fileName );
        }
    }


    public sealed class MapGenerator {
        MapGeneratorArgs args;
        Random rand;
        Noise noise;
        float[,] heightmap, blendmap;

        const int WaterCoveragePasses = 10;
        const float CliffsideBlockThreshold = 0.01f;

        // theme-dependent vars
        Block bWaterSurface, bGroundSurface, bWater, bGround, bSeaFloor, bBedrock, bDeepWaterSurface, bCliff;
        int groundThickness = 5, seaFloorThickness = 3;


        public MapGenerator( MapGeneratorArgs _args ) {
            args = _args;
            args.Validate();
            rand = new Random( args.seed );
            noise = new Noise( rand );
            ApplyTheme( args.theme );
        }


        public Map Generate() {
            GenerateHeightmap();
            return GenerateMap();
        }


        public void GenerateHeightmap() {
            heightmap = new float[args.dimX, args.dimY];

            noise.PerlinNoiseMap( heightmap, args.maxDetailSize, args.minDetailSize, args.roughness );

            if( args.useBias ) {
                Noise.Normalize( heightmap );

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
            Noise.Normalize( heightmap );

            if( args.layeredHeightmap ) {
                // needs a new Noise object to randomize second map
                float[,] heightmap2 = new float[args.dimX, args.dimY];
                new Noise( rand ).PerlinNoiseMap( heightmap2, 0, args.minDetailSize, args.roughness );
                Noise.Normalize( heightmap2 );

                // make a blendmap
                blendmap = new float[args.dimX, args.dimY];
                int blendmapDetailSize = (int)Math.Log( (double)Math.Max( args.dimX, args.dimY ), 2 ) - 2;
                new Noise( rand ).PerlinNoiseMap( blendmap, 3, blendmapDetailSize, 0.5f );
                Noise.Normalize( blendmap );
                float cliffSteepness = Math.Max( args.dimX, args.dimY ) / 6f;
                Noise.ScaleAndClip( blendmap, cliffSteepness );

                Noise.Blend( heightmap, heightmap2, blendmap );
            }

            if( args.marbledHeightmap ) {
                Noise.Marble( heightmap );
            }

            if( args.invertHeightmap ) {
                Noise.Invert( heightmap );
            }
        }


        public Map GenerateMap() {
            Map map = new Map( null, args.dimX, args.dimY, args.dimH );
            args.waterLevel = (map.height - 1) / 2;

            float desiredWaterLevel = .5f;
            if( args.matchWaterCoverage ) {
                desiredWaterLevel = MatchWaterCoverage( heightmap, args.waterCoverage );
            }
            float underWaterMultiplier = 0, aboveWaterMultiplier = 0;

            if( desiredWaterLevel != 0 ) {
                underWaterMultiplier = args.maxDepth / desiredWaterLevel;
            }
            if( desiredWaterLevel != 1 ) {
                aboveWaterMultiplier = args.maxHeight / (1 - desiredWaterLevel);
            }

            int level;
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( heightmap[x, y] < desiredWaterLevel ) {
                        level = args.waterLevel - (int)Math.Round( (1 - heightmap[x, y] / desiredWaterLevel) * args.maxDepth );
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
                                map.SetBlock( x, y, level, bGroundSurface );
                            }
                            for( int i = level - 1; i >= 0; i-- ) {
                                if( level - i < groundThickness ) {
                                    if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold) ) {
                                        map.SetBlock( x, y, i, bCliff );
                                    } else {
                                        map.SetBlock( x, y, i, bGround );
                                    }
                                } else {
                                    map.SetBlock( x, y, i, bBedrock );
                                }
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
                                if( blendmap != null && blendmap[x, y] > CliffsideBlockThreshold && blendmap[x, y] < (1 - CliffsideBlockThreshold) ) {
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

                    if( map.GetBlock( nx, ny, nz ) == (byte)bGroundSurface ) {
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


        /*
        public static MapGeneratorArgs MakePreset( MapGenType preset ) {
            switch( preset ) {
                case MapGenType.Cliffs:
                    return new MapGeneratorArgs {
                        cornerBiasMax = 0.2f,
                        cornerBiasMin = 0.05f,
                        minDetailSize = 1,
                        layeredHeightmap = true,
                        midpointBias = 0,
                        roughness = .6f,
                        useBias = true
                    };
                case MapGenType.Coast:
                    return new MapGeneratorArgs {
                        cornerBiasMax = -0.1f,
                        cornerBiasMin = 0.13f,
                        minDetailSize = 1,
                        matchWaterCoverage = true,
                        midpointBias = 0,
                        roughness = .5f,
                        useBias = true,
                        waterCoverage = .6f
                    };
                case MapGenType.Hills:
                    return new MapGeneratorArgs {
                        minDetailSize = 2,
                        matchWaterCoverage = true,
                        roughness = .45f,
                        waterCoverage = .6f
                    };
                case MapGenType.Island:
                    return new MapGeneratorArgs {
                        cornerBiasMax = -0.05f,
                        cornerBiasMin = -0.2f,
                        minDetailSize = 2,
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
                        minDetailSize = 2,
                        matchWaterCoverage = true,
                        midpointBias = -.3f,
                        roughness = .5f,
                        useBias = true,
                        waterCoverage = .25f
                    };
                case MapGenType.Mountains:
                    return new MapGeneratorArgs {
                        minDetailSize = 1,
                        layeredHeightmap = true,
                        matchWaterCoverage = true,
                        roughness = .6f,
                        waterCoverage = .6f
                    };
                case MapGenType.River:
                    return new MapGeneratorArgs {
                        minDetailSize = 1,
                        marbled = true,
                        roughness = .5f,
                    };
            }
            return null; // can never happen
        }*/
    }
}