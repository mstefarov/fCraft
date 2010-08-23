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
        Rocky
    }

    public enum MapGenTemplate {
        Archipelago,
        Atoll,
        Bay,
        Default,
        Dunes,
        Hills,
        Ice,
        Island,
        Lake,
        Mountains,
        River,
        Streams
    }


    public sealed class MapGeneratorArgs {
        const int FormatVersion = 2;

        public MapGenTheme theme;
        public int seed, dimX, dimY, dimH, maxHeight, maxDepth, waterLevel;
        public bool addWater;

        public bool matchWaterCoverage;
        public float waterCoverage;
        public int raisedCorners, loweredCorners, midPoint;
        public float bias;
        public bool useBias;

        public int detailScale, featureScale;
        public float roughness;
        public bool layeredHeightmap, marbledHeightmap, invertHeightmap;

        public bool addTrees;
        public int treeSpacingMin, treeSpacingMax, treeHeightMin, treeHeightMax;

        public void Validate() {
            if( raisedCorners < 0 || raisedCorners > 4 || loweredCorners < 0 || raisedCorners > 4 || raisedCorners + loweredCorners > 4 ) {
                throw new ArgumentException( "raisedCorners and loweredCorners must be between 0 and 4." );
            }
            // todo: additional validation
        }

        public MapGeneratorArgs(){
            theme = MapGenTheme.Forest;
            seed = (new Random()).Next();
            dimX = 128;
            dimY = 128;
            dimH = 80;
            maxHeight = 20;
            maxDepth = 12;
            waterLevel = 40;
            addWater = true;

            matchWaterCoverage = false;
            waterCoverage = .5f;
            raisedCorners = 0;
            loweredCorners = 0;
            midPoint = 0;
            bias = 0;
            useBias = false;

            detailScale = 7;
            featureScale = 1;
            roughness = .5f;
            layeredHeightmap = false;
            marbledHeightmap = false;
            invertHeightmap = false;

            addTrees = true;
            treeSpacingMin = 6;
            treeSpacingMax = 10;
            treeHeightMin = 5;
            treeHeightMax = 7;
        }

        public MapGeneratorArgs( string fileName ) {
            XDocument doc = XDocument.Load( fileName );
            XElement root = doc.Root;

            XAttribute versionTag = root.Attribute( "version" );
            int version = 0;
            if( versionTag != null && versionTag.Value != null && versionTag.Value.Length > 0 ) {
                version = Int32.Parse( versionTag.Value );
            }

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

            if( version == 0 ) {
                detailScale = Int32.Parse( root.Element( "minDetailSize" ).Value );
                featureScale = Int32.Parse( root.Element( "maxDetailSize" ).Value );
            } else {
                detailScale = Int32.Parse( root.Element( "detailScale" ).Value );
                featureScale = Int32.Parse( root.Element( "featureScale" ).Value );
            }
            roughness = float.Parse( root.Element( "roughness" ).Value );
            layeredHeightmap = Boolean.Parse( root.Element( "layeredHeightmap" ).Value );
            marbledHeightmap = Boolean.Parse( root.Element( "marbledHeightmap" ).Value );
            invertHeightmap = Boolean.Parse( root.Element( "invertHeightmap" ).Value );

            addTrees = Boolean.Parse( root.Element( "addTrees" ).Value );
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

            root.Add( new XAttribute( "version", FormatVersion ) );

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

            root.Add( new XElement( "detailScale", detailScale ) );
            root.Add( new XElement( "featureScale", featureScale ) );
            root.Add( new XElement( "roughness", roughness ) );
            root.Add( new XElement( "layeredHeightmap", layeredHeightmap ) );
            root.Add( new XElement( "marbledHeightmap", marbledHeightmap ) );
            root.Add( new XElement( "invertHeightmap", invertHeightmap ) );

            root.Add( new XElement( "addTrees", addTrees ) );
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

            noise.PerlinNoiseMap( heightmap, args.featureScale, args.detailScale, args.roughness );

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
                new Noise( rand ).PerlinNoiseMap( heightmap2, 0, args.detailScale, args.roughness );
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

            if( args.addTrees ) {
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
            int TopLayers = 2;
            double Odds = 0.618;
            bool OnlyAir = true;

            Random rn = new Random();
            int nx, ny, nz, nh;
            int radius;

            map.CalculateShadows();

            for( int x = 0; x < map.widthX; x += rn.Next( MinTrunkPadding, MaxTrunkPadding ) ) {
                for( int y = 0; y < map.widthY; y += rn.Next( MinTrunkPadding, MaxTrunkPadding ) ) {
                    nx = x + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) );
                    ny = y + rn.Next( -(MinTrunkPadding / 2), (MaxTrunkPadding / 2) );
                    if( nx < 0 || nx >= map.widthX || ny < 0 || ny >= map.widthY ) continue;
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
                        treeSpacingMax=12,
                        treeSpacingMin=6
                    };
                case MapGenTemplate.Default:
                    return new MapGeneratorArgs();
                case MapGenTemplate.Dunes:
                    return new MapGeneratorArgs {
                        addTrees=false,
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
                        addWater=false,
                        maxHeight = 8,
                        maxDepth = 8,
                        featureScale = 2,
                        treeSpacingMin = 7,
                        treeSpacingMax = 13
                    };
                case MapGenTemplate.Ice:
                    return new MapGeneratorArgs {
                        addTrees=false,
                        theme = MapGenTheme.Arctic,
                        maxHeight = 2,
                        maxDepth = 2032,
                        featureScale = 2,
                        detailScale = 7,
                        roughness = .64f,
                        marbledHeightmap = true,
                        matchWaterCoverage = true,
                        waterCoverage = .3f
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
                        detailScale = 7
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
                        treeSpacingMin=8,
                        treeSpacingMax=14
                    };
            }
            return null; // can never happen
        }
    }
}