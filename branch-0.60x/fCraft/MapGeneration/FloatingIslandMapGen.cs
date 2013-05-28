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

        public int TreeClusterDensity { get; set; }
        public int TreeChainsPerCluster { get; set; }
        public int TreeHopsPerChain { get; set; }
        public int TreeSpread { get; set; }
        public int TreePlantRatio { get; set; }


        public FloatingIslandMapGenParameters() {
            TreeClusterDensity = 4000;
            TreeChainsPerCluster = 20;
            TreeHopsPerChain = 20;
            TreeSpread = 6;
            TreePlantRatio = 4;
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

        Random rand = new Random();
        Map map;

        public Map Generate() {
            if( Finished ) return Result;
            try {
                ReportProgress( 0, "Clumping spheres..." );
                map = new Map( null, genParams.MapWidth, genParams.MapLength, genParams.MapHeight, true );

                for( int i = 0; i < 8; i++ ) {
                    Vector3I offset = new Vector3I( rand.Next( 16, genParams.MapWidth - 16 ),
                                                    rand.Next( 16, genParams.MapLength - 16 ),
                                                    rand.Next( 16, genParams.MapHeight - 16 ) );
                    CreateIsland( offset );
                }

                ReportProgress( 20, "Smoothing..." );
                SmoothEdges();

                ReportProgress( 40, "Smoothing..." );
                SmoothEdges();

                ReportProgress( 60, "Expanding..." );
                ExpandGround();

                ReportProgress( 90, "Expanding..." );
                PlantGrass();

                PlantTrees();

                if( Canceled ) return null;
                Result = map;
                return Result;
            } finally {
                Finished = true;
                StatusString = (Canceled ? "Canceled" : "Finished");
            }
        }


        void CreateIsland( Vector3I offset ) {
            // step 1: create clumpy spheres
            List<Sphere> spheres = new List<Sphere>();
            const int sphereCount = 128;
            const float sphereSizeReduction = .92f;

            const int startSphereSize = 12;
            float sphereSize = startSphereSize;
            Sphere firstSphere = new Sphere( genParams.MapWidth/2f,
                                             genParams.MapLength/2f,
                                             genParams.MapHeight/2f,
                                             sphereSize );
            spheres.Add( firstSphere );

            for( int i = 1; i < sphereCount; i++ ) {
                float newRadius = sphereSize + 1;
                Sphere newSphere = new Sphere( RandNextFloat( newRadius, genParams.MapWidth - newRadius ),
                                               RandNextFloat( newRadius, genParams.MapLength - newRadius ),
                                               RandNextFloat( newRadius, genParams.MapHeight - sphereSize ),
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
                float distance = (float)Math.Pow( newSphere.Radius + closestSphere.Radius, .75 );
                newSphere.Origin = closestSphere.Origin + direction*distance;

                spheres.Add( newSphere );
                sphereSize *= sphereSizeReduction;
            }

            PerlinNoise pn = new PerlinNoise( rand, 3 );

            // step 2: voxelize our spheres
            offset -= new Vector3I( genParams.MapWidth/2,
                                    genParams.MapLength/2,
                                    genParams.MapHeight/2 );
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
                            if( sphere.DistanceTo( coord ) < sphere.Radius + pn.GetNoise( x, y ) ) {
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
                            newMap.SetBlock( x, y, z, Block.White );
                        } else if( sum > 1 && map.GetBlock( x, y, z - 1 ) != Block.Air ) {
                            newMap.SetBlock( x, y, z, Block.Red );
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


        public void GrowTree( Random treeRand, int startX, int startY, int startZ ) {
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