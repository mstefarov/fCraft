using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace fCraft {
    public class VanillaMapGen : IMapGenerator {
        public static VanillaMapGen Instance { get; private set; }

        static VanillaMapGen() {
            Instance = new VanillaMapGen();
        }

        VanillaMapGen() {}


        public string Name {
            get { return "Vanilla"; }
        }

        public Version Version {
            get { return new Version( 1, 0 ); }
        }

        public IMapGeneratorParameters GetDefaultParameters() {
            return new VanillaMapGenParameters();
        }

        public IMapGeneratorParameters CreateParameters( string serializedParameters ) {
            throw new NotImplementedException();
        }

        public IMapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            throw new NotImplementedException();
        }
    }


    public class VanillaMapGenParameters : IMapGeneratorParameters {
        public VanillaMapGenParameters() {
            TerrainFeatureOctaves = 6;
            TerrainDetailOctaves = 8;
            WaterSpawnDensity = 8000;
            LavaSpawnDensity = 20000;
            FlowerClusterDensity = 3000;
            FlowerSpread = 6;
            FlowerChainsPerCluster = 10;
            FlowersPerChain = 5;
            ShroomClusterDensity = 2000;
            ShroomChainsPerCluster = 20;
            ShroomHopsPerChain = 5;
            ShroomSpreadHozirontal = 6;
            ShroomSpreadVertical = 2;
            TreeClusterDensity = 4000;
            TreeChainsPerCluster = 20;
            TreeHopsPerChain = 20;
            TreeSpread = 6;
            TreePlantRatio = 4;
            CoalOreDensity = 90;
            IronOreDensity = 70;
            GoldOreDensity = 50;
            CaveDensity = 256;
            Seed = 0;

            Generator = VanillaMapGen.Instance;
        }


        public int TerrainFeatureOctaves { get; set; }
        public int TerrainDetailOctaves { get; set; }
        public int WaterSpawnDensity { get; set; }
        public int LavaSpawnDensity { get; set; }
        public int FlowerClusterDensity { get; set; }
        public int FlowerSpread { get; set; }
        public int FlowerChainsPerCluster { get; set; }
        public int FlowersPerChain { get; set; }
        public int ShroomClusterDensity { get; set; }
        public int ShroomChainsPerCluster { get; set; }
        public int ShroomHopsPerChain { get; set; }
        public int ShroomSpreadHozirontal { get; set; }
        public int ShroomSpreadVertical { get; set; }
        public int TreeClusterDensity { get; set; }
        public int TreeChainsPerCluster { get; set; }
        public int TreeHopsPerChain { get; set; }
        public int TreeSpread { get; set; }
        public int TreePlantRatio { get; set; }
        public int CoalOreDensity { get; set; }
        public int IronOreDensity { get; set; }
        public int GoldOreDensity { get; set; }
        public int CaveDensity { get; set; }
        public int Seed { get; set; }


        public object Clone() {
            return new VanillaMapGenParameters {
                TerrainFeatureOctaves = TerrainFeatureOctaves,
                TerrainDetailOctaves = TerrainDetailOctaves,
                WaterSpawnDensity = WaterSpawnDensity,
                LavaSpawnDensity = LavaSpawnDensity,
                FlowerClusterDensity = FlowerClusterDensity,
                FlowerSpread = FlowerSpread,
                FlowerChainsPerCluster = FlowerChainsPerCluster,
                FlowersPerChain = FlowersPerChain,
                ShroomClusterDensity = ShroomClusterDensity,
                ShroomChainsPerCluster = ShroomChainsPerCluster,
                ShroomHopsPerChain = ShroomHopsPerChain,
                ShroomSpreadHozirontal = ShroomSpreadHozirontal,
                ShroomSpreadVertical = ShroomSpreadVertical,
                TreeClusterDensity = TreeClusterDensity,
                TreeChainsPerCluster = TreeChainsPerCluster,
                TreeHopsPerChain = TreeHopsPerChain,
                TreeSpread = TreeSpread,
                TreePlantRatio = TreePlantRatio,
                CoalOreDensity = CoalOreDensity,
                IronOreDensity = IronOreDensity,
                GoldOreDensity = GoldOreDensity,
                CaveDensity = CaveDensity,
                Seed = Seed
            };
        }


        [Browsable(false)]
        public IMapGenerator Generator { get; private set; }
        [Browsable( false )]
        public int MapWidth { get; set; }
        [Browsable( false )]
        public int MapLength { get; set; }
        [Browsable( false )]
        public int MapHeight { get; set; }

        public string Save() {
            throw new NotImplementedException();
        }

        public IMapGeneratorState CreateGenerator() {
            return new NotchyMapGenerator( this );
        }
    }


    public sealed class NotchyMapGenerator : IMapGeneratorState {
        readonly Random random;
        readonly byte[] blocks;
        readonly int waterLevel;
        readonly int[] heightmap;
        readonly Map map;

        public NotchyMapGenerator( VanillaMapGenParameters genParams ) {
            param = genParams;
            random = new Random();
            waterLevel = genParams.MapHeight/2;
            heightmap = new int[genParams.MapWidth*genParams.MapLength];
            map = new Map( null, genParams.MapWidth, genParams.MapLength, genParams.MapHeight, true );
            blocks = map.Blocks;
        }

        VanillaMapGenParameters param;
        public IMapGeneratorParameters Parameters { get; private set; }


        public bool Canceled { get; private set; }
        public bool Finished { get; private set; }
        public bool SupportsCancellation {
            get { return false; }
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
        public bool ReportsProgress {
            get { return true; }
        }
        public int Progress { get; private set; }
        public string StatusString { get; private set; }



        Map IMapGeneratorState.Generate() {
            return Generate();
        }

        public void CancelAsync() {}


        Map Generate() {
            try {
                ReportProgress( 0, "Raising..." );
                Raise();

                ReportProgress( 20, "Eroding..." );
                Erode();

                ReportProgress( 35, "Soiling..." );
                Soil();

                ReportProgress( 45, "Carving..." );
                Carve();

                ReportProgress( 55, "Depositing coal..." );
                MakeOreVeins( Block.Coal, param.CoalOreDensity );
                ReportProgress( 58, "Depositing iron..." );
                MakeOreVeins( Block.IronOre, param.IronOreDensity );
                ReportProgress( 61, "Depositing gold..." );
                MakeOreVeins( Block.GoldOre, param.GoldOreDensity );

                ReportProgress( 65, "Watering..." );
                Water();

                ReportProgress( 75, "Melting..." );
                Melt();

                ReportProgress( 80, "Growing..." );
                Grow();

                ReportProgress( 90, "Planting flowers..." );
                PlantFlowers();

                ReportProgress( 93, "Planting shrooms..." );
                PlantShrooms();

                ReportProgress( 96, "Planting trees..." );
                PlantTrees();

                ReportProgress( 100, "Finished." );
                Result = map;
                return map;
            } finally {
                Finished = true;
            }
        }


        void Raise() {
            Random raiseRand = new Random( random.Next() );
            FilteredNoise raiseNoise1 = new FilteredNoise( new PerlinNoise( raiseRand, param.TerrainDetailOctaves ),
                                                           new PerlinNoise( raiseRand, param.TerrainDetailOctaves ) );
            FilteredNoise raiseNoise2 = new FilteredNoise( new PerlinNoise( raiseRand, param.TerrainDetailOctaves ),
                                                           new PerlinNoise( raiseRand, param.TerrainDetailOctaves ) );
            PerlinNoise raiseNoise3 = new PerlinNoise( raiseRand, param.TerrainFeatureOctaves );

            // raising
            const double scale = 1.3;
            for( int x = 0; x < param.MapWidth; x++ ) {
                for( int y = 0; y < param.MapLength; y++ ) {
                    double d2 = raiseNoise1.GetNoise( x*scale, y*scale )/6.0 - 4;
                    double d3 = raiseNoise2.GetNoise( x*scale, y*scale )/5.0 + 10.0 - 4;
                    double d4 = raiseNoise3.GetNoise( x, y )/8.0;
                    if( d4 > 0 )
                        d3 = d2;
                    double elevation = Math.Max( d2, d3 )/2.0;
                    if( elevation < 0 )
                        elevation *= 0.8;
                    heightmap[(x + y*param.MapWidth)] = (int)elevation;
                }
            }
        }


        void Erode() {
            Random erodeRand = new Random( random.Next() );
            FilteredNoise erodeNoise1 = new FilteredNoise( new PerlinNoise( erodeRand, param.TerrainDetailOctaves ),
                                                           new PerlinNoise( erodeRand, param.TerrainDetailOctaves ) );
            FilteredNoise erodeNoise2 = new FilteredNoise( new PerlinNoise( erodeRand, param.TerrainDetailOctaves ),
                                                           new PerlinNoise( erodeRand, param.TerrainDetailOctaves ) );
            for( int x = 0; x < param.MapWidth; x++ ) {
                for( int y = 0; y < param.MapLength; y++ ) {
                    double d1 = erodeNoise1.GetNoise( x*2, y*2 )/8.0;
                    int i7 = erodeNoise2.GetNoise( x*2, y*2 ) > 0 ? 1 : 0;
                    if( d1 <= 2 )
                        continue;
                    int i19 = ((heightmap[(x + y*param.MapWidth)] - i7)/2*2) + i7;
                    heightmap[(x + y*param.MapWidth)] = i19;
                }
            }
        }


        void Soil() {
            Random soilRand = new Random( random.Next() );
            PerlinNoise soilNoise1 = new PerlinNoise( soilRand, 8 );
            for( int x = 0; x < param.MapWidth; x++ ) {
                for( int y = 0; y < param.MapLength; y++ ) {
                    int i7 = (int)(soilNoise1.GetNoise( x, y )/24.0) - 4;
                    int i19 = heightmap[(x + y*param.MapWidth)] + waterLevel;
                    int i21 = i19 + i7;
                    heightmap[(x + y*param.MapWidth)] = Math.Max( i19, i21 );
                    if( heightmap[(x + y*param.MapWidth)] > param.MapHeight - 2 )
                        heightmap[(x + y*param.MapWidth)] = (param.MapHeight - 2);
                    if( heightmap[(x + y*param.MapWidth)] < 1 )
                        heightmap[(x + y*param.MapWidth)] = 1;
                    for( int z = 0; z < param.MapHeight; z++ ) {
                        Block block = Block.Air;
                        if( z <= i19 )
                            block = Block.Dirt;
                        if( z <= i21 )
                            block = Block.Stone;
                        if( z == 0 )
                            block = Block.Lava;
                        int index = (z*param.MapLength + y)*param.MapWidth + x;
                        blocks[index] = (byte)block;
                    }
                }
            }
        }


        void Water() {
            Random waterRand = new Random( random.Next() );
            for( int x = 0; x < param.MapWidth; x++ ) {
                FloodFill( x, 0, param.MapHeight/2 - 1, Block.StillWater );
                FloodFill( x, param.MapLength - 1, param.MapHeight/2 - 1, Block.StillWater );
            }
            for( int y = 0; y < param.MapLength; y++ ) {
                FloodFill( 0, y, param.MapHeight/2 - 1, Block.StillWater );
                FloodFill( param.MapWidth - 1, y, param.MapHeight/2 - 1, Block.StillWater );
            }
            int maxWaterSpawns = param.MapWidth*param.MapLength/param.WaterSpawnDensity;
            for( int waterSpawn = 0; waterSpawn < maxWaterSpawns; waterSpawn++ ) {
                int x = waterRand.Next( param.MapWidth );
                int y = waterRand.Next( param.MapLength );
                int z = waterLevel - 1 - waterRand.Next( 2 );
                if( blocks[((z*param.MapLength + y)*param.MapWidth + x)] != (byte)Block.Air )
                    continue;
                FloodFill( x, y, z, Block.StillWater );
            }
        }


        void Melt() {
            Random meltRand = new Random( random.Next() );
            int lavaSpawns = param.MapWidth*param.MapLength*param.MapHeight/param.LavaSpawnDensity;
            for( int lavaSpawn = 0; lavaSpawn < lavaSpawns; lavaSpawn++ ) {
                int x = meltRand.Next( param.MapWidth );
                int y = meltRand.Next( param.MapLength );
                int z = (int)(meltRand.NextDouble()*meltRand.NextDouble()*(waterLevel - 3));
                if( blocks[((z*param.MapLength + y)*param.MapWidth + x)] != (byte)Block.Air )
                    continue;
                FloodFill( x, y, z, Block.StillLava );
            }
        }


        void Grow() {
            PerlinNoise growNoise1 = new PerlinNoise( random, 8 );
            PerlinNoise growNoise2 = new PerlinNoise( random, 8 );
            for( int x = 0; x < param.MapWidth; x++ ) {
                for( int y = 0; y < param.MapLength; y++ ) {
                    int elevation = heightmap[(x + y*param.MapWidth)];
                    Block blockAbove = (Block)blocks[(((elevation + 1)*param.MapLength + y)*param.MapWidth + x)];
                    int index = (elevation*param.MapLength + y)*param.MapWidth + x;

                    if( blockAbove == Block.Air ) {
                        bool placeSand = growNoise1.GetNoise( x, y ) > 8.0;
                        if( (elevation <= param.MapHeight/2 - 1) && placeSand ) {
                            blocks[index] = (byte)Block.Sand;
                        } else {
                            blocks[index] = (byte)Block.Grass;
                        }
                    } else if( ((blockAbove == Block.Water) || (blockAbove == Block.StillWater)) &&
                               (elevation <= param.MapHeight/2 - 1) ) {
                        bool placeGravel = growNoise2.GetNoise( x, y ) > 12.0;
                        if( placeGravel ) {
                            blocks[index] = (byte)Block.Gravel;
                        }
                    }
                }
            }
        }


        void PlantFlowers() {
            Random flowerRand = new Random( random.Next() );
            int maxFlowers = param.MapWidth*param.MapLength/param.FlowerClusterDensity;
            for( int cluster = 0; cluster < maxFlowers; cluster++ ) {
                int flowerType = flowerRand.Next( 2 );
                int clusterX = flowerRand.Next( param.MapWidth );
                int clusterY = flowerRand.Next( param.MapLength );
                for( int flower = 0; flower < param.FlowerChainsPerCluster; flower++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < param.FlowersPerChain; hop++ ) {
                        x += flowerRand.Next( param.FlowerSpread ) - flowerRand.Next( param.FlowerSpread );
                        y += flowerRand.Next( param.FlowerSpread ) - flowerRand.Next( param.FlowerSpread );
                        if( (x < 0) || (y < 0) || (x >= param.MapWidth) || (y >= param.MapLength) )
                            continue;

                        int z = heightmap[(x + y*param.MapWidth)] + 1;
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
            Random shroomRand = new Random( random.Next() );
            int maxShrooms = param.MapWidth*param.MapLength*param.MapHeight/param.ShroomClusterDensity;
            for( int cluster = 0; cluster < maxShrooms; cluster++ ) {
                int shroomType = shroomRand.Next( 2 );
                int clusterX = shroomRand.Next( param.MapWidth );
                int clusterY = shroomRand.Next( param.MapLength );
                int clusterZ = shroomRand.Next( param.MapHeight );
                for( int shroom = 0; shroom < param.ShroomChainsPerCluster; shroom++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    int z = clusterZ;
                    for( int hop = 0; hop < param.ShroomHopsPerChain; hop++ ) {
                        x += shroomRand.Next( param.ShroomSpreadHozirontal ) -
                             shroomRand.Next( param.ShroomSpreadHozirontal );
                        y += shroomRand.Next( param.ShroomSpreadHozirontal ) -
                             shroomRand.Next( param.ShroomSpreadHozirontal );
                        z += shroomRand.Next( param.ShroomSpreadVertical ) -
                             shroomRand.Next( param.ShroomSpreadVertical );
                        if( (x < 0) || (y < 0) || (z < 1) || (x >= param.MapWidth) || (y >= param.MapLength) ||
                            (z >= heightmap[(x + y*param.MapWidth)] - 1) )
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
            Random treeRand = new Random( random.Next() );
            int maxTrees = param.MapWidth*param.MapLength/param.TreeClusterDensity;
            for( int cluster = 0; cluster < maxTrees; cluster++ ) {
                int clusterX = treeRand.Next( param.MapWidth );
                int clusterY = treeRand.Next( param.MapLength );
                for( int tree = 0; tree < param.TreeChainsPerCluster; tree++ ) {
                    int x = clusterX;
                    int y = clusterY;
                    for( int hop = 0; hop < param.TreeHopsPerChain; hop++ ) {
                        x += treeRand.Next( param.TreeSpread ) - treeRand.Next( param.TreeSpread );
                        y += treeRand.Next( param.TreeSpread ) - treeRand.Next( param.TreeSpread );
                        if( (x < 0) || (y < 0) || (x >= param.MapWidth) || (y >= param.MapLength) )
                            continue;
                        if( treeRand.Next( param.TreePlantRatio ) != 0 )
                            continue;
                        int z = heightmap[(x + y*param.MapWidth)] + 1;
                        GrowTree( treeRand, x, y, z );
                    }
                }
            }
        }


        // Based on Minecraft Classic's "com.mojang.minecraft.level.maybeGrowTree"
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


        void Carve() {
            Random carveRand = new Random( random.Next() );
            int maxCaves = param.MapWidth*param.MapLength*param.MapHeight/param.CaveDensity/64*2;
            for( int i = 0; i < maxCaves; i++ ) {
                double startX = carveRand.NextDouble()*param.MapWidth;
                double startY = carveRand.NextDouble()*param.MapLength;
                double startZ = carveRand.NextDouble()*param.MapHeight;
                double f9 = carveRand.NextDouble()*Math.PI*2;
                double f10 = 0;
                double f11 = carveRand.NextDouble()*Math.PI*2;
                double f12 = 0;
                double f13 = carveRand.NextDouble()*carveRand.NextDouble();
                int caveLength = (int)((carveRand.NextDouble() + carveRand.NextDouble())*200);
                for( int step = 0; step < caveLength; step++ ) {
                    startX += Math.Sin( f9 )*Math.Cos( f11 );
                    startY += Math.Cos( f9 )*Math.Cos( f11 );
                    startZ += Math.Sin( f11 );
                    f9 += f10*0.2;
                    f10 = f10*0.9 + (carveRand.NextDouble() - carveRand.NextDouble());
                    f11 = (f11 + f12*0.5)*0.5;
                    f12 = f12*0.75 + (carveRand.NextDouble() - carveRand.NextDouble());
                    if( carveRand.NextDouble() < 0.25 )
                        continue;
                    double f1 = startX + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f2 = startZ + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f5 = startY + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f6 = (param.MapHeight - f2)/param.MapHeight;
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
                                    (x >= param.MapWidth - 1) || (z >= param.MapHeight - 1) ||
                                    (y >= param.MapLength - 1) ) {
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
            Random oreVeinRand = new Random( random.Next() );
            int maxVeins = param.MapWidth*param.MapLength*param.MapHeight/256/64*density/100;
            for( int vein = 0; vein < maxVeins; vein++ ) {
                double startX = oreVeinRand.NextDouble()*param.MapWidth;
                double startY = oreVeinRand.NextDouble()*param.MapLength;
                double startZ = oreVeinRand.NextDouble()*param.MapHeight;
                double f4 = oreVeinRand.NextDouble()*Math.PI*2;
                double f5 = 0;
                double f6 = oreVeinRand.NextDouble()*Math.PI*2;
                double f7 = 0;
                int m = (int)((oreVeinRand.NextDouble() + oreVeinRand.NextDouble())*75*density/100);
                for( int n = 0; n < m; n++ ) {
                    startX += Math.Sin( f4 )*Math.Cos( f6 );
                    startY += Math.Cos( f4 )*Math.Sin( f6 );
                    startZ += Math.Sin( f6 );
                    f4 += f5*0.2;
                    f5 = (f5*0.9) + (oreVeinRand.NextDouble() - oreVeinRand.NextDouble());
                    f6 = (f6 + f7*0.5)*0.5;
                    f7 = (f7*0.9) + (oreVeinRand.NextDouble() - oreVeinRand.NextDouble());
                    double f8 = Math.Sin( n*Math.PI/m )*density/100 + 1;
                    for( int x = (int)(startX - f8); x <= (int)(startX + f8); x++ ) {
                        for( int z = (int)(startZ - f8); z <= (int)(startZ + f8); z++ ) {
                            for( int y = (int)(startY - f8); y <= (int)(startY + f8); y++ ) {
                                double f9 = x - startX;
                                double f10 = z - startZ;
                                double f11 = y - startY;
                                f9 = f9*f9 + f10*f10*2 + f11*f11;
                                if( (f9 >= f8*f8) || (x < 1) || (z < 1) || (y < 1) ||
                                    (x >= param.MapWidth - 1) || (z >= param.MapHeight - 1) ||
                                    (y >= param.MapLength - 1) )
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
                if( coord.X + 1 < param.MapWidth && blocks[Index( coord.X + 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X + 1, coord.Y, coord.Z ) );
                }
                if( coord.X - 1 >= 0 && blocks[Index( coord.X - 1, coord.Y, coord.Z )] == (byte)Block.Air ) {
                    stack.Push( new Vector3I( coord.X - 1, coord.Y, coord.Z ) );
                }
                if( coord.Y + 1 < param.MapLength && blocks[Index( coord.X, coord.Y + 1, coord.Z )] == (byte)Block.Air ) {
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
            return (z*param.MapLength + y)*param.MapWidth + x;
        }
    }
}