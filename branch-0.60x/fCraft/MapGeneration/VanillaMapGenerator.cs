using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft {
    sealed class NotchyMapGenerator {
        const int TerrainFeatureOctaves = 6,
                  TerrainDetailOctaves = 8,
                  WaterSpawnDensity = 8000,
                  LavaSpawnDensity = 20000,
                  FlowerClusterDensity = 3000,
                  FlowerSpread = 6,
                  FlowerChainsPerCluster = 10,
                  FlowersPerChain = 5,
                  ShroomClusterDensity = 2000,
                  ShroomChainsPerCluster = 20,
                  ShroomHopsPerChain = 5,
                  ShroomSpreadHozirontal = 6,
                  ShroomSpreadVertical = 2,
                  TreeClusterDensity = 4000,
                  TreeChainsPerCluster = 20,
                  TreeHopsPerChain = 20,
                  TreeSpread = 6,
                  TreePlantRatio = 4,
                  CoalOreDensity = 90,
                  IronOreDensity = 70,
                  GoldOreDensity = 50,
                  CaveDensity = 256;

        readonly int mapWidth;
        readonly int mapLength;
        readonly int mapHeight;
        readonly Random random;
        readonly byte[] blocks;
        readonly int waterLevel;
        readonly int[] heightmap;
        readonly Map map;


        [NotNull]
        public static Map Generate( int mapWidth, int mapLength, int mapHeight ) {
            return new NotchyMapGenerator( mapWidth, mapLength, mapHeight ).Generate();
        }

        NotchyMapGenerator( int mapWidth, int mapLength, int mapHeight ) {
            this.mapWidth = mapWidth;
            this.mapLength = mapLength;
            this.mapHeight = mapHeight;
            random = new Random();
            waterLevel = mapHeight/2;
            heightmap = new int[mapWidth*mapLength];
            map = new Map( null, mapWidth, mapLength, mapHeight, true );
            blocks = map.Blocks;
        }

        Map Generate() {
            Raise();
            Erode();
            Soil();
            Carve();
            MakeOreVeins( Block.Coal, CoalOreDensity );
            MakeOreVeins( Block.IronOre, IronOreDensity );
            MakeOreVeins( Block.GoldOre, GoldOreDensity );
            Water();
            Melt();
            Grow();
            PlantFlowers();
            PlantShrooms();
            PlantTrees();
            return map;
        }


        void Raise() {
            FilteredNoise raiseNoise1 = new FilteredNoise( new PerlinNoise( random, TerrainDetailOctaves ),
                                                           new PerlinNoise( random, TerrainDetailOctaves ) );
            FilteredNoise raiseNoise2 = new FilteredNoise( new PerlinNoise( random, TerrainDetailOctaves ),
                                                           new PerlinNoise( random, TerrainDetailOctaves ) );
            PerlinNoise raiseNoise3 = new PerlinNoise( random, TerrainFeatureOctaves );

            // raising
            const double scale = 1.3;
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    double d2 = raiseNoise1.GetNoise( x*scale, y*scale )/6.0 - 4;
                    double d3 = raiseNoise2.GetNoise( x*scale, y*scale )/5.0 + 10.0 - 4;
                    double d4 = raiseNoise3.GetNoise( x, y )/8.0;
                    if( d4 > 0 )
                        d3 = d2;
                    double elevation = Math.Max( d2, d3 )/2.0;
                    if( elevation < 0 )
                        elevation *= 0.8;
                    heightmap[(x + y*mapWidth)] = (int)elevation;
                }
            }
        }


        void Erode() {
            FilteredNoise erodeNoise1 = new FilteredNoise( new PerlinNoise( random, TerrainDetailOctaves ),
                                                           new PerlinNoise( random, TerrainDetailOctaves ) );
            FilteredNoise erodeNoise2 = new FilteredNoise( new PerlinNoise( random, TerrainDetailOctaves ),
                                                           new PerlinNoise( random, TerrainDetailOctaves ) );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    double d1 = erodeNoise1.GetNoise( x*2, y*2 )/8.0;
                    int i7 = erodeNoise2.GetNoise( x*2, y*2 ) > 0 ? 1 : 0;
                    if( d1 <= 2 )
                        continue;
                    int i19 = ((heightmap[(x + y*mapWidth)] - i7)/2*2) + i7;
                    heightmap[(x + y*mapWidth)] = i19;
                }
            }
        }


        void Soil() {
            PerlinNoise soilNoise1 = new PerlinNoise( random, 8 );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    int i7 = (int)(soilNoise1.GetNoise( x, y )/24.0) - 4;
                    int i19 = heightmap[(x + y*mapWidth)] + waterLevel;
                    int i21 = i19 + i7;
                    heightmap[(x + y*mapWidth)] = Math.Max( i19, i21 );
                    if( heightmap[(x + y*mapWidth)] > mapHeight - 2 )
                        heightmap[(x + y*mapWidth)] = (mapHeight - 2);
                    if( heightmap[(x + y*mapWidth)] < 1 )
                        heightmap[(x + y*mapWidth)] = 1;
                    for( int z = 0; z < mapHeight; z++ ) {
                        Block block = Block.Air;
                        if( z <= i19 )
                            block = Block.Dirt;
                        if( z <= i21 )
                            block = Block.Stone;
                        if( z == 0 )
                            block = Block.Lava;
                        int index = (z*mapLength + y)*mapWidth + x;
                        blocks[index] = (byte)block;
                    }
                }
            }
        }


        void Water() {
            for( int x = 0; x < mapWidth; x++ ) {
                FloodFill( x, 0, mapHeight/2 - 1, Block.StillWater );
                FloodFill( x, mapLength - 1, mapHeight/2 - 1, Block.StillWater );
            }
            for( int y = 0; y < mapLength; y++ ) {
                FloodFill( 0, y, mapHeight/2 - 1, Block.StillWater );
                FloodFill( mapWidth - 1, y, mapHeight/2 - 1, Block.StillWater );
            }
            int maxWaterSpawns = mapWidth*mapLength/WaterSpawnDensity;
            for( int waterSpawn = 0; waterSpawn < maxWaterSpawns; waterSpawn++ ) {
                int x = random.Next( mapWidth );
                int y = random.Next( mapLength );
                int z = waterLevel - 1 - random.Next( 2 );
                if( blocks[((z*mapLength + y)*mapWidth + x)] != (byte)Block.Air )
                    continue;
                FloodFill( x, y, z, Block.StillWater );
            }
        }


        void Melt() {
            int lavaSpawns = mapWidth*mapLength*mapHeight/LavaSpawnDensity;
            for( int lavaSpawn = 0; lavaSpawn < lavaSpawns; lavaSpawn++ ) {
                int x = random.Next( mapWidth );
                int y = random.Next( mapLength );
                int z = (int)(random.NextDouble()*random.NextDouble()*(waterLevel - 3));
                if( blocks[((z*mapLength + y)*mapWidth + x)] != (byte)Block.Air )
                    continue;
                FloodFill( x, y, z, Block.StillLava );
            }
        }


        void Grow() {
            PerlinNoise growNoise1 = new PerlinNoise( random, 8 );
            PerlinNoise growNoise2 = new PerlinNoise( random, 8 );
            for( int x = 0; x < mapWidth; x++ ) {
                for( int y = 0; y < mapLength; y++ ) {
                    int elevation = heightmap[(x + y*mapWidth)];
                    Block blockAbove = (Block)blocks[(((elevation + 1)*mapLength + y)*mapWidth + x)];
                    int index = (elevation*mapLength + y)*mapWidth + x;

                    if( blockAbove == Block.Air ) {
                        bool placeSand = growNoise1.GetNoise( x, y ) > 8.0;
                        if( (elevation <= mapHeight/2 - 1) && placeSand ) {
                            blocks[index] = (byte)Block.Sand;
                        } else {
                            blocks[index] = (byte)Block.Grass;
                        }
                    } else if( ((blockAbove == Block.Water) || (blockAbove == Block.StillWater)) &&
                               (elevation <= mapHeight/2 - 1) ) {
                        bool placeGravel = growNoise2.GetNoise( x, y ) > 12.0;
                        if( placeGravel ) {
                            blocks[index] = (byte)Block.Gravel;
                        }
                    }
                }
            }
        }


        void PlantFlowers() {
            int maxFlowers = mapWidth*mapLength/FlowerClusterDensity;
            for( int cluster = 0; cluster < maxFlowers; cluster++ ) {
                int flowerType = random.Next( 2 );
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                for( int flower = 0; flower < FlowerChainsPerCluster; flower++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < FlowersPerChain; hop++ ) {
                        x += random.Next( FlowerSpread ) - random.Next( FlowerSpread );
                        y += random.Next( FlowerSpread ) - random.Next( FlowerSpread );
                        if( (x < 0) || (y < 0) || (x >= mapWidth) || (y >= mapLength) )
                            continue;

                        int z = heightmap[(x + y*mapWidth)] + 1;
                        int index = Index( x, y, z );

                        Block blockAbove = (Block)blocks[index];
                        if( blockAbove != Block.Air )
                            continue;
                        Block blockUnder = (Block)blocks[Index( x, y, z - 1 )];
                        if( blockUnder != Block.Grass )
                            continue;

                        if( flowerType == 0 ) {
                            blocks[index] = (byte)Block.YellowFlower;
                        } else {
                            blocks[index] = (byte)Block.RedFlower;
                        }
                    }
                }
            }
        }


        void PlantShrooms() {
            int maxShrooms = mapWidth*mapLength*mapHeight/ShroomClusterDensity;
            for( int cluster = 0; cluster < maxShrooms; cluster++ ) {
                int shroomType = random.Next( 2 );
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                int clusterZ = random.Next( mapHeight );
                for( int shroom = 0; shroom < ShroomChainsPerCluster; shroom++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    int z = clusterZ;
                    for( int hop = 0; hop < ShroomHopsPerChain; hop++ ) {
                        x += random.Next( ShroomSpreadHozirontal ) - random.Next( ShroomSpreadHozirontal );
                        y += random.Next( ShroomSpreadHozirontal ) - random.Next( ShroomSpreadHozirontal );
                        z += random.Next( ShroomSpreadVertical ) - random.Next( ShroomSpreadVertical );
                        if( (x < 0) || (y < 0) || (z < 1) || (x >= mapWidth) || (y >= mapLength) ||
                            (z >= heightmap[(x + y*mapWidth)] - 1) )
                            continue;

                        int index = Index( x, y, z );
                        Block blockAbove = (Block)blocks[index];
                        if( blockAbove != Block.Air )
                            continue;
                        Block blockUnder = (Block)blocks[Index( x, y, z - 1 )];
                        if( blockUnder != Block.Stone )
                            continue;

                        if( shroomType == 0 ) {
                            blocks[index] = (byte)Block.BrownMushroom;
                        } else {
                            blocks[index] = (byte)Block.RedMushroom;
                        }
                    }
                }
            }
        }


        void PlantTrees() {
            int maxTrees = mapWidth*mapLength/TreeClusterDensity;
            for( int cluster = 0; cluster < maxTrees; cluster++ ) {
                int clusterX = random.Next( mapWidth );
                int clusterY = random.Next( mapLength );
                for( int tree = 0; tree < TreeChainsPerCluster; tree++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < TreeHopsPerChain; hop++ ) {
                        x += random.Next( TreeSpread ) - random.Next( TreeSpread );
                        y += random.Next( TreeSpread ) - random.Next( TreeSpread );
                        if( (x < 0) || (y < 0) || (x >= mapWidth) || (y >= mapLength) )
                            continue;
                        if( random.Next( TreePlantRatio ) != 0 )
                            continue;
                        int z = heightmap[(x + y*mapWidth)] + 1;
                        GrowTree( random, x, y, z );
                    }
                }
            }
        }


        // Based on Minecraft Classic's "com.mojang.minecraft.level.maybeGrowTree"
        public bool GrowTree( Random random, int startX, int startY, int startZ ) {
            int treeHeight = random.Next( 3 ) + 4;

            Block blockUnder = map.GetBlock( startX, startY, startZ - 1 );
            if( (blockUnder != Block.Grass) || (startZ >= map.Height - treeHeight - 1) )
                return false;

            for( int z = startZ; z <= startZ + 1 + treeHeight; z++ ) {
                int extent = 1;
                if( z == startZ ) extent = 0;
                if( z >= startZ + 1 + treeHeight - 2 ) extent = 2;
                for( int x = startX - extent; (x <= startX + extent); x++ ) {
                    for( int y = startY - extent; (y <= startY + extent); y++ ) {
                        if( (x >= 0) && (z >= 0) && (y >= 0) && (x < map.Width) && (z < map.Height) && (y < map.Length) ) {
                            if( map.GetBlock( x, y, z ) != Block.Air )
                                return false;
                        } else {
                            return false;
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
                            ((random.Next( 2 ) == 0) || (n == 0)) )
                            continue;
                        map.SetBlock( x, y, z, Block.Leaves );
                    }
                }
            }
            for( int z = 0; z < treeHeight; z++ ) {
                map.SetBlock( startX, startY, startZ + z, Block.Log );
            }
            return true;
        }


        void Carve() {
            int maxCaves = mapWidth*mapLength*mapHeight/CaveDensity/64*2;
            for( int i = 0; i < maxCaves; i++ ) {
                double startX = random.NextDouble()*mapWidth;
                double startY = random.NextDouble()*mapLength;
                double startZ = random.NextDouble()*mapHeight;
                double f9 = random.NextDouble()*Math.PI*2;
                double f10 = 0;
                double f11 = random.NextDouble()*Math.PI*2;
                double f12 = 0;
                double f13 = random.NextDouble()*random.NextDouble();
                int caveLength = (int)((random.NextDouble() + random.NextDouble())*200);
                for( int step = 0; step < caveLength; step++ ) {
                    startX += Math.Sin( f9 )*Math.Cos( f11 );
                    startY += Math.Cos( f9 )*Math.Cos( f11 );
                    startZ += Math.Sin( f11 );
                    f9 += f10*0.2;
                    f10 = f10*0.9 + (random.NextDouble() - random.NextDouble());
                    f11 = (f11 + f12*0.5)*0.5;
                    f12 = f12*0.75 + (random.NextDouble() - random.NextDouble());
                    if( random.NextDouble() < 0.25 )
                        continue;
                    double f1 = startX + (random.NextDouble()*4 - 2)*0.2;
                    double f2 = startZ + (random.NextDouble()*4 - 2)*0.2;
                    double f5 = startY + (random.NextDouble()*4 - 2)*0.2;
                    double f6 = (mapHeight - f2)/mapHeight;
                    f6 = 1.2 + (f6*3.5 + 1)*f13;
                    f6 = Math.Sin( step*Math.PI/caveLength )*f6;
                    for( int x = (int)(f1 - f6); x <= (int)(f1 + f6); x++ ) {
                        for( int z = (int)(f2 - f6); z <= (int)(f2 + f6); z++ ) {
                            for( int y = (int)(f5 - f6); y <= (int)(f5 + f6); y++ ) {
                                double f14 = x - f1;
                                double f15 = z - f2;
                                double f16 = y - f5;
                                f14 = f14*f14 + f15*f15*2 + f16*f16;
                                if( (f14 >= f6*f6) ||
                                    (x < 1) || (z < 1) || (y < 1) ||
                                    (x >= mapWidth - 1) || (z >= mapHeight - 1) || (y >= mapLength - 1) ) {
                                    continue;
                                }
                                int index = Index( x, y, z );
                                if( (Block)blocks[index] == Block.Stone ) {
                                    blocks[index] = (byte)Block.Air;
                                }
                            }
                        }
                    }
                }
            }
        }


        void MakeOreVeins( Block oreTile, int density ) {
            int maxVeins = mapWidth*mapLength*mapHeight/256/64*density/100;
            for( int vein = 0; vein < maxVeins; vein++ ) {
                double startX = random.NextDouble()*mapWidth;
                double startY = random.NextDouble()*mapLength;
                double startZ = random.NextDouble()*mapHeight;
                double f4 = random.NextDouble()*Math.PI*2;
                double f5 = 0;
                double f6 = random.NextDouble()*Math.PI*2;
                double f7 = 0;
                int m = (int)((random.NextDouble() + random.NextDouble())*75*density/100);
                for( int n = 0; n < m; n++ ) {
                    startX += Math.Sin( f4 )*Math.Cos( f6 );
                    startY += Math.Cos( f4 )*Math.Sin( f6 );
                    startZ += Math.Sin( f6 );
                    f4 += f5*0.2;
                    f5 = (f5*0.9) + (random.NextDouble() - random.NextDouble());
                    f6 = (f6 + f7*0.5)*0.5;
                    f7 = (f7*0.9) + (random.NextDouble() - random.NextDouble());
                    double f8 = Math.Sin( n*Math.PI/m )*density/100 + 1;
                    for( int x = (int)(startX - f8); x <= (int)(startX + f8); x++ ) {
                        for( int z = (int)(startZ - f8); z <= (int)(startZ + f8); z++ ) {
                            for( int y = (int)(startY - f8); y <= (int)(startY + f8); y++ ) {
                                double f9 = x - startX;
                                double f10 = z - startZ;
                                double f11 = y - startY;
                                f9 = f9*f9 + f10*f10*2 + f11*f11;
                                if( (f9 >= f8*f8) || (x < 1) || (z < 1) || (y < 1) ||
                                    (x >= mapWidth - 1) || (z >= mapHeight - 1) || (y >= mapLength - 1) )
                                    continue;
                                int index = Index( x, y, z );
                                if( (Block)blocks[index] == Block.Stone ) {
                                    blocks[index] = (byte)oreTile;
                                }
                            }
                        }
                    }
                }
            }
        }


        void FloodFill( int x, int y, int z, Block newBlock ) {
            if( blocks[Index( x, y, z )] != (byte)Block.Air ) return;
            Vector3I coord = new Vector3I( x, y, z );
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push( coord );
            while( stack.Count > 0 ) {
                coord = stack.Pop();
                blocks[Index( coord.X, coord.Y, coord.Z )] = (byte)newBlock;
                if( coord.X + 1 < mapWidth && blocks[Index( coord.X + 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X + 1, coord.Y, coord.Z ) );
                }
                if( coord.X - 1 >= 0 && blocks[Index( coord.X - 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X - 1, coord.Y, coord.Z ) );
                }
                if( coord.Y + 1 < mapLength && blocks[Index( coord.X, coord.Y + 1, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y + 1, coord.Z ) );
                }
                if( coord.Y - 1 >= 0 && blocks[Index( coord.X, coord.Y - 1, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y - 1, coord.Z ) );
                }
                if( coord.Z - 1 >= 0 && blocks[Index( coord.X, coord.Y, coord.Z - 1 )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X, coord.Y, coord.Z - 1 ) );
                }
            }
        }


        struct Vector3I {
            public Vector3I( int x, int y, int z ) {
                X = x;
                Y = y;
                Z = z;
            }

            public readonly int X, Y, Z;
        }


        int Index( int x, int y, int z ) {
            return (z*mapLength + y)*mapWidth + x;
        }
    }
}