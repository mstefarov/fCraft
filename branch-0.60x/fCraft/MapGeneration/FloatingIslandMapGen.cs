using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;

namespace fCraft {
    public class FloatingIslandMapGen : IMapGenerator {
        public static FloatingIslandMapGen Instance { get; private set; }
        FloatingIslandMapGen() {}

        static FloatingIslandMapGen() {
            Instance = new FloatingIslandMapGen();
        }

        public string Name {
            get { return "Floating Island"; }
        }

        public Version Version {
            get { return new Version( 1, 0 ); }
        }

        public IMapGeneratorParameters GetDefaultParameters() {
            return new FloatingIslandMapGenParameters();
        }

        public IMapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return new FloatingIslandMapGenParameters( serializedParameters );
        }

        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            return new FloatingIslandMapGenParameters(); // TODO: command parsing
        }
    }


    public class FloatingIslandMapGenParameters : IMapGeneratorParameters {
        [Browsable( false )]
        public int MapWidth { get; set; }

        [Browsable( false )]
        public int MapLength { get; set; }

        [Browsable( false )]
        public int MapHeight { get; set; }

        [Browsable( false )]
        public IMapGenerator Generator {
            get { return FloatingIslandMapGen.Instance; }
        }

        public double IslandDensity { get; set; }
        public double SphereSeparation { get; set; }
        public double SphereSizeReduction { get; set; }
        public int SphereCount { get; set; }
        public int SphereSize { get; set; }
        public int SphereSizeSpread { get; set; }

        public double Verticality { get; set; }

        public int TreeClusterDensity { get; set; }
        public int TreeChainsPerCluster { get; set; }
        public int TreeHopsPerChain { get; set; }
        public int TreeSpread { get; set; }
        public int TreePlantRatio { get; set; }

        public int TreeSpacingMax { get; set; }
        public int TreeSpacingMin { get; set; }

        public int FlowerClusterDensity { get; set; }
        public int FlowerSpread { get; set; }
        public int FlowerChainsPerCluster { get; set; }
        public int FlowersPerChain { get; set; }

        public double SpringDensity { get; set; }
        public double SpringMaxHops { get; set; }

        public int Seed { get; set; }


        public FloatingIslandMapGenParameters() {
            IslandDensity = 1;
            SphereSeparation = 0.8;
            SphereSizeReduction = 0.9;
            SphereCount = 128;

            SphereSize = 12;
            SphereSizeSpread = 3;
            Verticality = 1;

            TreeClusterDensity = 4000;
            TreeChainsPerCluster = 20;
            TreeHopsPerChain = 20;
            TreeSpread = 6;
            TreePlantRatio = 4;

            FlowerClusterDensity = 3000;
            FlowerSpread = 6;
            FlowerChainsPerCluster = 10;
            FlowersPerChain = 5;

            SpringDensity = 1;
            SpringMaxHops = 1024;

            TreeSpacingMin = 7;
            TreeSpacingMax = 11;

            Seed = new Random().Next();
        }


        public FloatingIslandMapGenParameters( XElement el )
            : this() {
            throw new NotImplementedException();
        }


        public void Save( XElement baseElement ) {
            throw new NotImplementedException();
        }


        public IMapGeneratorState CreateGenerator() {
            return new FloatingIslandMapGenState( this );
        }


        public object Clone() {
            throw new NotImplementedException();
        }
    }


    class FloatingIslandMapGenState : IMapGeneratorState {
        public FloatingIslandMapGenState( FloatingIslandMapGenParameters parameters ) {
            genParams = parameters;
            Parameters = parameters;
        }

        readonly FloatingIslandMapGenParameters genParams;

        public IMapGeneratorParameters Parameters { get; private set; }
        public bool Canceled { get; private set; }
        public bool Finished { get; private set; }
        public int Progress { get; private set; }
        public string StatusString { get; private set; }

        public bool ReportsProgress {
            get { return true; }
        }

        public bool SupportsCancellation {
            get { return true; }
        }

        public Map Result { get; private set; }
        public event ProgressChangedEventHandler ProgressChanged;

        void ReportProgress( int progressPercent, string statusString ) {
            Progress = progressPercent;
            StatusString = statusString;
            var handler = ProgressChanged;
            if( handler != null ) {
                ProgressChangedEventArgs args = new ProgressChangedEventArgs( progressPercent, statusString );
                handler( this, args );
            }
        }

        Random rand;
        Map map;

        public Map Generate() {
            if( Finished ) return Result;
            try {
                ReportProgress( 0, "Clumping spheres..." );
                rand = new Random( genParams.Seed );
                map = new Map( null, genParams.MapWidth, genParams.MapLength, genParams.MapHeight, true );

                int numIslands = Math.Max( 1, (int)(map.Volume * genParams.IslandDensity / (96 * 96 * 64)) );
                Random islandCoordRand = new Random( rand.Next() );
                for( int i = 0; i < numIslands; i++ ) {
                    Vector3I offset = new Vector3I( islandCoordRand.Next( 16, genParams.MapWidth - 16 ),
                                                    islandCoordRand.Next( 16, genParams.MapLength - 16 ),
                                                    islandCoordRand.Next( 16, genParams.MapHeight - 16 ) );
                    CreateIsland( offset );
                }
                if( Canceled ) return null;
                
                ReportProgress( 10, "Smoothing..." );
                SmoothEdges();
                if( Canceled ) return null;
                
                ReportProgress( 20, "Smoothing..." );
                SmoothEdges();
                if( Canceled ) return null;
                
                ReportProgress( 30, "Expanding..." );
                ExpandGround();
                if( Canceled ) return null;
                
                ReportProgress( 80, "Planting grass..." );
                PlantGrass();
                if( Canceled ) return null;
                
                ReportProgress( 70, "Watering..." );
                for( int x = 0; x < map.Width; x++ ) {
                    for( int y = 0; y < map.Length; y++ ) {
                        if( map.GetBlock( x, y, 0 ) == Block.Air ) {
                            map.SetBlock( x, y, 0, Block.Water );
                        }
                    }
                }
                MakeWater();
                if( Canceled ) return null;
                
                ReportProgress( 85, "Planting trees..." );
                PlantGiantTrees();
                PlantTrees();
                if( Canceled ) return null;

                ReportProgress( 85, "Planting flowers..." );
                PlantFlowers();
                if( Canceled ) return null;
                
                ReportProgress( 90, "Eroding..." );
                Erode();
                if( Canceled ) return null;
                ReportProgress( 95, "Eroding..." );
                Erode();
                if( Canceled ) return null;
                
                Result = map;
                return Result;
            } finally {
                Finished = true;
                Progress = 100;
                StatusString = (Canceled ? "Canceled" : "Finished");
            }
        }

        void PlantGiantTrees() {
            Map outMap = new Map( null, map.Width, map.Length, map.Height, false ) {Blocks = (byte[])map.Blocks.Clone()};
            var foresterArgs = new ForesterArgs {
                Map = map,
                Rand = rand,
                TreeCount = (int)(map.Width*map.Length*4/(1024f*(genParams.TreeSpacingMax + genParams.TreeSpacingMin)/2)),
                Operation = Forester.ForesterOperation.Add,
                PlantOn = Block.Grass
            };
            foresterArgs.BlockPlacing += ( sender, e ) => outMap.SetBlock( e.Coordinate, e.Block );
            Forester.Generate( foresterArgs );
            map = outMap;
        }


        void MakeWater() {
            int waterSources = (int)(256 * 256 * 96 / 50000 / genParams.IslandDensity * genParams.SpringDensity);
            for( int i = 0; i < waterSources; i++ ) {
                int x = rand.Next( 0, map.Width );
                int y = rand.Next( 0, map.Length );
                int z = rand.Next( map.Height/2, map.Height-1 );
                while( z > 0 ) {
                    if( map.GetBlock( x, y, z + 1 ) == Block.Air && map.GetBlock( x, y, z ) == Block.Grass ) {
                        MakeWaterSource( x, y, z );
                    }
                    z--;
                }
            }
        }


        void MakeWaterSource( int x, int y, int z ) {
            int hop = 0;
            List<Vector3I> choices = new List<Vector3I>();
            while( hop < genParams.SpringMaxHops && z > 0 ) {
                hop++;
                map.SetBlock( x, y, z, Block.Water );
                Block blockUnder = map.GetBlock( x, y, z - 1 );
                if( blockUnder == Block.Air || blockUnder == Block.Water ) {
                    do {
                        z--;
                        blockUnder = map.GetBlock( x, y, z );
                        map.SetBlock( x, y, z, Block.Water );
                    } while( blockUnder == Block.Air || blockUnder == Block.Water );
                    continue;
                }
                map.SetBlock( x, y, z-1, Block.Gravel );
                choices.Clear();
                if( x > 0 && map.GetBlock( x - 1, y, z + 1 ) == Block.Air ) {
                    choices.Add( new Vector3I( x - 1, y, z ) );
                }
                if( x < map.Width - 1 && map.GetBlock( x + 1, y, z + 1 ) == Block.Air ) {
                    choices.Add( new Vector3I( x + 1, y, z ) );
                }
                if( y > 0 && map.GetBlock( x, y - 1, z + 1 ) == Block.Air ) {
                    choices.Add( new Vector3I( x, y - 1, z ) );
                }
                if( y < map.Length - 1 && map.GetBlock( x, y + 1, z + 1 ) == Block.Air ) {
                    choices.Add( new Vector3I( x, y + 1, z ) );
                }
                if( choices.Count == 0 ) break;
                Vector3I nextCoord = choices[rand.Next( choices.Count )];
                x = nextCoord.X;
                y = nextCoord.Y;
            }
        }


        void CreateIsland( Vector3I offset ) {
            // step 1: create clumpy spheres
            List<Sphere> spheres = new List<Sphere>();

            const int hSpread = 100;
            double vSpreadMin = -hSpread*genParams.Verticality/2,
                   vSpreadMax = hSpread*genParams.Verticality/2;

            double sphereSize = rand.Next( genParams.SphereSize - genParams.SphereSizeSpread,
                                           genParams.SphereSize + genParams.SphereSizeSpread + 1 );
            Sphere firstSphere = new Sphere( 0, 0, 0, (float)sphereSize );
            spheres.Add( firstSphere );

            for( int i = 1; i < genParams.SphereCount; i++ ) {
                float newRadius = (float)(sphereSize + 1);
                double angle = rand.NextDouble()*Math.PI*2;
                Sphere newSphere = new Sphere( (float)(Math.Cos( angle )*rand.NextDouble()*hSpread),
                                               (float)(Math.Sin( angle )*rand.NextDouble()*hSpread),
                                               RandNextFloat( vSpreadMin, vSpreadMax ),
                                               newRadius );

                double closestDist = newSphere.DistanceTo( spheres[0] );
                Sphere closestSphere = spheres[0];
                for( int j = 1; j < i; j++ ) {
                    double newDist = newSphere.DistanceTo( spheres[j] );
                    if( newDist < closestDist ) {
                        closestDist = newDist;
                        closestSphere = spheres[j];
                    }
                }

                Vector3F displacement = newSphere.Origin - closestSphere.Origin;
                Vector3F direction = displacement.Normalize();
                float distance = (float)Math.Pow( newSphere.Radius + closestSphere.Radius, genParams.SphereSeparation );
                newSphere.Origin = closestSphere.Origin + direction*distance;

                spheres.Add( newSphere );
                sphereSize *= genParams.SphereSizeReduction;
            }

            // step 2: voxelize our spheres
            foreach( Sphere sphere in spheres ) {
                Vector3I origin = new Vector3I( (int)Math.Floor( sphere.Origin.X - sphere.Radius ) - 4,
                                                (int)Math.Floor( sphere.Origin.Y - sphere.Radius ) - 4,
                                                (int)Math.Floor( sphere.Origin.Z - sphere.Radius ) - 4 );
                BoundingBox box = new BoundingBox( origin,
                                                   (int)Math.Ceiling( sphere.Radius )*2 + 8,
                                                   (int)Math.Ceiling( sphere.Radius )*2 + 8,
                                                   (int)Math.Ceiling( sphere.Radius ) + 4 );
                for( int x = box.XMin; x <= box.XMax; x++ ) {
                    for( int y = box.YMin; y <= box.YMax; y++ ) {
                        for( int z = box.ZMin; z <= box.ZMax; z++ ) {
                            Vector3I coord = new Vector3I( x, y, z );
                            if( sphere.DistanceTo( coord ) < sphere.Radius ) {
                                map.SetBlock( coord + offset, Block.Stone );
                            }
                        }
                    }
                }
            }
        }


        void SmoothEdges() {
            Map newMap = new Map( null, map.Width, map.Length, map.Height, true );
            for( int x = 1; x < genParams.MapWidth - 1; x++ ) {
                for( int y = 1; y < genParams.MapLength - 1; y++ ) {
                    for( int z = 1; z < genParams.MapHeight - 1; z++ ) {
                        int sum = (map.GetBlock( x - 1, y, z ) != Block.Air ? 1 : 0) +
                                  (map.GetBlock( x + 1, y, z ) != Block.Air ? 1 : 0) +
                                  (map.GetBlock( x, y - 1, z ) != Block.Air ? 1 : 0) +
                                  (map.GetBlock( x, y + 1, z ) != Block.Air ? 1 : 0) +
                                  (map.GetBlock( x, y, z - 1 ) != Block.Air ? 1 : 0) +
                                  (map.GetBlock( x, y, z + 1 ) != Block.Air ? 1 : 0);
                        if( map.GetBlock( x, y, z ) != Block.Air ) {
                            if( sum < 3 ) {
                                newMap.SetBlock( x, y, z, Block.Red );
                            } else {
                                newMap.SetBlock( x, y, z, Block.White );
                            }
                        } else if( sum > 1 && map.GetBlock( x, y, z - 1 ) != Block.Air ) {
                            newMap.SetBlock( x, y, z, Block.Blue );
                        }
                    }
                }
            }
            map = newMap;
        }


        void ExpandGround() {
            Map newMap = new Map( null, map.Width, map.Length, map.Height, true );
            for( int x = 2; x < genParams.MapWidth - 2; x++ ) {
                for( int y = 2; y < genParams.MapLength - 2; y++ ) {
                    for( int z = 2; z < genParams.MapHeight - 2; z++ ) {
                        if( map.GetBlock( x, y, z ) == Block.Air ) {
                            if( HasNeighbors( x, y, z ) ) {
                                newMap.SetBlock( x, y, z, Block.Dirt );
                            }
                        } else {
                            newMap.SetBlock( x, y, z, Block.Stone );
                        }
                    }
                }
                if( x%4 == 0 ) {
                    int percent = x*100/map.Width;
                    ReportProgress( percent/2 + 30, "Expanding (" + percent + "%)..." );
                }
            }
            map = newMap;
        }


        bool HasNeighbors( int x, int y, int z ) {
            return map.GetBlock( x - 1, y - 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y - 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y - 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y, z - 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y + 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y + 1, z - 1 ) != Block.Air ||

                   map.GetBlock( x - 1, y - 1, z ) != Block.Air ||
                   map.GetBlock( x, y - 1, z ) != Block.Air ||
                   map.GetBlock( x + 1, y - 1, z ) != Block.Air ||
                   map.GetBlock( x - 1, y, z ) != Block.Air ||
                   map.GetBlock( x + 1, y, z ) != Block.Air ||
                   map.GetBlock( x - 1, y + 1, z ) != Block.Air ||
                   map.GetBlock( x, y + 1, z ) != Block.Air ||
                   map.GetBlock( x + 1, y + 1, z ) != Block.Air ||

                   map.GetBlock( x - 1, y - 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x, y - 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y - 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y, z + 1 ) != Block.Air ||
                   map.GetBlock( x, y, z + 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x, y + 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x + 1, y + 1, z + 1 ) != Block.Air ||

                   map.GetBlock( x - 2, y - 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x - 2, y - 1, z ) != Block.Air ||
                   map.GetBlock( x - 2, y - 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 2, y, z - 1 ) != Block.Air ||
                   map.GetBlock( x - 2, y, z ) != Block.Air ||
                   map.GetBlock( x - 2, y, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 2, y + 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x - 2, y + 1, z ) != Block.Air ||
                   map.GetBlock( x - 2, y + 1, z + 1 ) != Block.Air ||

                   map.GetBlock( x + 2, y - 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 2, y - 1, z ) != Block.Air ||
                   map.GetBlock( x + 2, y - 1, z + 1 ) != Block.Air ||
                   map.GetBlock( x + 2, y, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 2, y, z ) != Block.Air ||
                   map.GetBlock( x + 2, y, z + 1 ) != Block.Air ||
                   map.GetBlock( x + 2, y + 1, z - 1 ) != Block.Air ||
                   map.GetBlock( x + 2, y + 1, z ) != Block.Air ||
                   map.GetBlock( x + 2, y + 1, z + 1 ) != Block.Air ||

                   map.GetBlock( x - 1, y - 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y - 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y - 2, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y - 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y - 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y - 2, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y - 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y - 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y - 2, z + 1 ) != Block.Air ||

                   map.GetBlock( x - 1, y + 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y + 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y + 2, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y + 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y + 2, z + 1 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 2, z - 1 ) != Block.Air ||
                   map.GetBlock( x, y + 2, z ) != Block.Air ||
                   map.GetBlock( x + 1, y + 2, z + 1 ) != Block.Air ||

                   // bottom
                   map.GetBlock( x - 1, y - 1, z - 2 ) != Block.Air ||
                   map.GetBlock( x, y - 1, z - 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y - 1, z - 2 ) != Block.Air ||
                   map.GetBlock( x - 1, y, z - 2 ) != Block.Air ||
                   map.GetBlock( x, y, z - 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y, z - 2 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 1, z - 2 ) != Block.Air ||
                   map.GetBlock( x, y + 1, z - 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y + 1, z - 2 ) != Block.Air ||

                   // top
                   map.GetBlock( x - 1, y - 1, z + 2 ) != Block.Air ||
                   map.GetBlock( x, y - 1, z + 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y - 1, z + 2 ) != Block.Air ||
                   map.GetBlock( x - 1, y, z + 2 ) != Block.Air ||
                   map.GetBlock( x, y, z + 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y, z + 2 ) != Block.Air ||
                   map.GetBlock( x - 1, y + 1, z + 2 ) != Block.Air ||
                   map.GetBlock( x, y + 1, z + 2 ) != Block.Air ||
                   map.GetBlock( x + 1, y + 1, z + 2 ) != Block.Air;
        }


        void PlantGrass() {
            for( int x = 0; x < genParams.MapWidth; x++ ) {
                for( int y = 0; y < genParams.MapLength; y++ ) {
                    bool blockAboveIsAir = true;
                    for( int z = genParams.MapHeight - 1; z > 0; z-- ) {
                        bool thisBlockIsAir = (map.GetBlock( x, y, z ) == Block.Air);
                        if( blockAboveIsAir && !thisBlockIsAir ) {
                            map.SetBlock( x, y, z, Block.Grass );
                        }
                        blockAboveIsAir = thisBlockIsAir;
                    }
                }
            }
            /*
            PerlinNoise pn = new PerlinNoise( rand, 3 );
            for( int x = 1; x < genParams.MapWidth-1; x++ ) {
                for( int y = 1; y < genParams.MapLength-1; y++ ) {
                    for( int z = 1; z < genParams.MapHeight - 1; z++ ) {
                        if( map.GetBlock( x, y, z ) == Block.Grass ) {
                            double slope = FindSlope( x, y, z );
                            if( slope < 2 && pn.GetNoise( x/5d, y/5d ) > .6 ) {
                                map.SetBlock( x, y, z, Block.Sand );
                            }
                        }
                    }
                }
            }*/
        }


        double FindSlope( int x, int y, int z ) {
            int z1 = FindNearestZ( x - 1, y, z );
            int z2 = FindNearestZ( x + 1, y, z );
            int z3 = FindNearestZ( x, y - 1, z );
            int z4 = FindNearestZ( x, y + 1, z );
            return Math.Abs( z1 - z2 ) + Math.Abs( z3 - z4 );
        }


        int FindNearestZ( int x, int y, int startZ ) {
            int dz = 0;
            while( Math.Abs( dz ) < 4 ) {
                if( map.GetBlock( x, y, startZ + dz + 1 ) == Block.Air ) {
                    if( map.GetBlock( x, y, startZ + dz ) != Block.Air ) {
                        break;
                    } else {
                        dz--;
                    }
                } else {
                    dz++;
                }
            }
            return startZ + dz;
        }


        void PlantTrees() {
            Random treeRand = new Random( rand.Next() );
            int maxTrees = genParams.MapWidth*genParams.MapLength/genParams.TreeClusterDensity;
            for( int cluster = 0; cluster < maxTrees; cluster++ ) {
                int clusterX = treeRand.Next( genParams.MapWidth );
                int clusterY = treeRand.Next( genParams.MapLength );
                for( int tree = 0; tree < genParams.TreeChainsPerCluster; tree++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < genParams.TreeHopsPerChain; hop++ ) {
                        x += treeRand.Next( genParams.TreeSpread ) - treeRand.Next( genParams.TreeSpread );
                        y += treeRand.Next( genParams.TreeSpread ) - treeRand.Next( genParams.TreeSpread );
                        if( (x < 0) || (y < 0) || (x >= genParams.MapWidth) || (y >= genParams.MapLength) )
                            continue;
                        if( treeRand.Next( genParams.TreePlantRatio ) != 0 )
                            continue;

                        for( int z = genParams.MapHeight - 1; z > 0; z-- ) {
                            if( map.GetBlock( x, y, z - 1 ) == Block.Grass ) {
                                GrowTree( treeRand, x, y, z );
                                break;
                            }
                        }
                    }
                }
            }
        }


        void GrowTree( Random treeRand, int startX, int startY, int startZ ) {
            int treeHeight = treeRand.Next( 3 ) + 4;

            Block blockUnder = map.GetBlock( startX, startY, startZ - 1 );
            if( (blockUnder != Block.Grass) || (startZ >= map.Height - treeHeight - 1) )
                return;

            for( int z = startZ; z <= startZ + 1 + treeHeight; z++ ) {
                int extent = 1;
                if( z == startZ ) extent = 0;
                if( z >= startZ + 1 + treeHeight - 2 ) extent = 2;
                for( int x = startX - extent; (x <= startX + extent); x++ ) {
                    for( int y = startY - extent; (y <= startY + extent); y++ ) {
                        if( (x >= 0) && (z >= 0) && (y >= 0) && (x < map.Width) && (z < map.Height) && (y < map.Length) ) {
                            if( map.GetBlock( x, y, z ) != Block.Air )
                                return;
                        } else {
                            return;
                        }
                    }
                }
            }

            map.SetBlock( startX, startY, startZ - 1, Block.Dirt );

            for( int z = startZ - 3 + treeHeight; z <= startZ + treeHeight; z++ ) {
                int n = z - (startZ + treeHeight);
                int foliageExtent = 1 - n/2;
                for( int x = startX - foliageExtent; x <= startX + foliageExtent; x++ ) {
                    int j = x - startX;
                    for( int y = startY - foliageExtent; y <= startY + foliageExtent; y++ ) {
                        int i3 = y - startY;
                        if( (Math.Abs( j ) == foliageExtent) && (Math.Abs( i3 ) == foliageExtent) &&
                            ((treeRand.Next( 2 ) == 0) || (n == 0)) )
                            continue;
                        map.SetBlock( x, y, z, Block.Leaves );
                    }
                }
            }
            for( int z = 0; z < treeHeight; z++ ) {
                map.SetBlock( startX, startY, startZ + z, Block.Log );
            }
        }


        void PlantFlowers() {
            Random flowerRand = new Random( rand.Next() );
            int maxFlowers = genParams.MapWidth * genParams.MapLength / genParams.FlowerClusterDensity;
            for( int cluster = 0; cluster < maxFlowers; cluster++ ) {
                int flowerType = flowerRand.Next( 2 );
                int clusterX = flowerRand.Next( genParams.MapWidth );
                int clusterY = flowerRand.Next( genParams.MapLength );
                for( int flower = 0; flower < genParams.FlowerChainsPerCluster; flower++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < genParams.FlowersPerChain; hop++ ) {
                        x += flowerRand.Next( genParams.FlowerSpread ) - flowerRand.Next( genParams.FlowerSpread );
                        y += flowerRand.Next( genParams.FlowerSpread ) - flowerRand.Next( genParams.FlowerSpread );
                        if( (x < 0) || (y < 0) || (x >= genParams.MapWidth) || (y >= genParams.MapLength) )
                            continue;
                        for( int z = genParams.MapHeight - 1; z > 0; z-- ) {
                            if( map.GetBlock( x, y, z - 1 ) == Block.Grass ) {
                                if( flowerType == 0 ) {
                                    map.SetBlock( x, y, z, Block.YellowFlower );
                                } else {
                                    map.SetBlock( x, y, z, Block.RedFlower );
                                }
                            }
                        }
                    }
                }
            }
        }


        void Erode() {
            for( int x = 0; x < genParams.MapWidth; x++ ) {
                for( int y = 0; y < genParams.MapLength; y++ ) {
                    for( int z = genParams.MapHeight - 1; z > 0; z-- ) {
                        if( map.GetBlock( x, y, z ) == Block.Dirt && map.GetBlock( x, y, z - 1 ) == Block.Air &&
                            rand.NextDouble() > .5 ) {
                            map.SetBlock( x, y, z, Block.Air );
                        }
                    }
                }
            }
        }


        float RandNextFloat( double min, double max ) {
            return (float)(rand.NextDouble()*(max - min) + min);
        }


        class Sphere {
            public Vector3F Origin;
            public readonly float Radius;

            public Sphere( float x, float y, float z, float radius ) {
                Origin = new Vector3F( x, y, z );
                Radius = radius;
            }

            public float DistanceTo( Sphere other ) {
                return (other.Origin - Origin).Length - (Radius + other.Radius);
            }

            public float DistanceTo( Vector3I other ) {
                return (other - Origin).Length;
            }
        }


        public void CancelAsync() {
            Canceled = true;
        }
    }
}