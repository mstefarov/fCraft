// Originally part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// Based in part on Minecraft 0.30 bytecode, copyright 2009 Markus Persson / Mojang AB

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    /// <summary> Map generator that creates landscapes identical to
    /// Notch's original ("vanilla") implementation of Minecraft. </summary>
    public class VanillaMapGen : MapGenerator {
        public static VanillaMapGen Instance { get; private set; }

        VanillaMapGen() {}


        static VanillaMapGen() {
            Instance = new VanillaMapGen {
                Name = "Vanilla",
                Help = "&S\"Vanilla\" map generator:\n" +
                       "Creates landscapes identical to Notch's original (\"vanilla\") implementation of Minecraft. " +
                       "Does not take any parameters (yet)."
            };
        }


        public override MapGeneratorParameters CreateDefaultParameters() {
            return new VanillaMapGenParameters();
        }


        public override MapGeneratorParameters CreateParameters(XElement root) {
            return new VanillaMapGenParameters(root);
        }


        public override MapGeneratorParameters CreateParameters(Player player, CommandReader cmd) {
            if (cmd.HasNext) {
                player.Message("Vanilla map generator does not take any parameters; using defaults.");
            }
            return new VanillaMapGenParameters();
        }


        public override MapGeneratorParameters CreateParameters(string presetName) {
            if (presetName == null) {
                throw new ArgumentNullException("presetName");
            } else if (presetName.Equals(Presets[0], StringComparison.OrdinalIgnoreCase)) {
                return CreateDefaultParameters();
            } else {
                return null; // TODO: make some presets
            }
        }
    }


    internal class VanillaMapGenParameters : MapGeneratorParameters {
        static readonly Random SeedRng = new Random();
        int shroomClusterDensity;
        int treeClusterDensity;
        int flowerClusterDensity;
        double oreDensity;
        double caveDensity;

        public bool AddFlowers { get; set; }
        public bool AddMushrooms { get; set; }
        public bool AddCaves { get; set; }
        public bool AddTrees { get; set; }

        public int TerrainFeatureOctaves { get; set; }
        public int TerrainDetailOctaves { get; set; }
        public int WaterSpawnDensity { get; set; }
        public int LavaSpawnDensity { get; set; }

        /// <summary> Spacing between flower clusters; default is 3000; must be greater than 0. </summary>
        public int FlowerClusterDensity {
            get { return flowerClusterDensity; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", "FlowerClusterDensity must be greater than 0");
                }
                flowerClusterDensity = value;
            }
        }

        public int FlowerSpread { get; set; }
        public int FlowerChainsPerCluster { get; set; }
        public int FlowersPerChain { get; set; }

        /// <summary> Spacing between mushroom clusters; default is 2000; must be greater than 0. </summary>
        public int ShroomClusterDensity {
            get { return shroomClusterDensity; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", "ShroomClusterDensity must be greater than 0");
                }
                shroomClusterDensity = value;
            }
        }

        public int ShroomChainsPerCluster { get; set; }
        public int ShroomHopsPerChain { get; set; }
        public int ShroomSpreadHorizontal { get; set; }
        public int ShroomSpreadVertical { get; set; }

        /// <summary> Spacing between tree clusters; default is 4000; must be greater than 0. </summary>
        public int TreeClusterDensity {
            get { return treeClusterDensity; }
            set {
                if (value < 0) {
                    throw new ArgumentOutOfRangeException("value", "TreeClusterDensity must be greater than 0");
                }
                treeClusterDensity = value;
            }
        }

        public int TreeChainsPerCluster { get; set; }
        public int TreeHopsPerChain { get; set; }
        public int TreeSpread { get; set; }
        public int TreePlantRatio { get; set; }

        /// <summary> Ore density fraction; default is 1; must be between 0.2 and 5.0 </summary>
        public double OreDensity {
            get { return oreDensity; }
            set {
                if (value < 0.2 || value > 5) {
                    throw new ArgumentOutOfRangeException("value", "OreDensity must be between 0.2 and 5");
                }
                oreDensity = value;
            }
        }

        /// <summary> Cave density fraction; default is 1; must be between 0.1 and 10.0 </summary>
        public double CaveDensity {
            get { return caveDensity; }
            set {
                if (value < 0.1 || value > 10) {
                    throw new ArgumentOutOfRangeException("value", "CaveDensity must be between 0.2 and 10");
                }
                caveDensity = value;
            }
        }

        public int Seed { get; set; }


        public VanillaMapGenParameters() {
            Generator = VanillaMapGen.Instance;

            AddFlowers = true;
            AddMushrooms = true;
            AddCaves = true;
            AddTrees = true;
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
            ShroomSpreadHorizontal = 6;
            ShroomSpreadVertical = 2;
            TreeClusterDensity = 4000;
            TreeChainsPerCluster = 20;
            TreeHopsPerChain = 20;
            TreeSpread = 6;
            TreePlantRatio = 4;
            OreDensity = 1;
            CaveDensity = 1;
            lock (SeedRng) {
                Seed = SeedRng.Next();
            }
        }


        public VanillaMapGenParameters([NotNull] XElement baseElement)
            : this() {
            base.LoadProperties(baseElement);
        }


        public override MapGeneratorState CreateGenerator() {
            return new VanillaMapGenState(this);
        }
    }


    internal sealed class VanillaMapGenState : MapGeneratorState {
        const int CoalOreDensity = 90,
                  IronOreDensity = 70,
                  GoldOreDensity = 50,
                  BaseCaveDensity = 256;

        readonly Random random;
        readonly byte[] blocks;
        readonly int waterLevel;
        readonly int[] heightmap;
        readonly Map map;


        internal VanillaMapGenState([NotNull] VanillaMapGenParameters genParams) {
            if (genParams == null) throw new ArgumentNullException("genParams");
            this.genParams = genParams;
            Parameters = genParams;
            random = new Random(genParams.Seed);
            waterLevel = genParams.MapHeight/2;
            heightmap = new int[genParams.MapWidth*genParams.MapLength];
            map = new Map(null, genParams.MapWidth, genParams.MapLength, genParams.MapHeight, true);
            blocks = map.Blocks;
            ReportsProgress = true;
            SupportsCancellation = true;
        }


        readonly VanillaMapGenParameters genParams;


        public override Map Generate() {
            if (Finished) return Result;
            try {
                ReportProgress(0, "Raising...");
                Raise();
                if (Canceled) return null;

                ReportProgress(20, "Eroding...");
                Erode();
                if (Canceled) return null;

                ReportProgress(35, "Soiling...");
                Soil();
                if (Canceled) return null;

                if (genParams.AddCaves) {
                    ReportProgress(45, "Carving...");
                    Carve();
                    if (Canceled) return null;
                }

                ReportProgress(55, "Depositing coal...");
                int density = (int)Math.Round(genParams.OreDensity*CoalOreDensity);
                MakeOreVeins(Block.Coal, density);
                ReportProgress(58, "Depositing iron...");
                density = (int)Math.Round(genParams.OreDensity*IronOreDensity);
                MakeOreVeins(Block.IronOre, density);
                ReportProgress(61, "Depositing gold...");
                density = (int)Math.Round(genParams.OreDensity*GoldOreDensity);
                MakeOreVeins(Block.GoldOre, density);
                if (Canceled) return null;

                ReportProgress(65, "Watering...");
                Water();
                if (Canceled) return null;

                ReportProgress(75, "Melting...");
                Melt();
                if (Canceled) return null;

                ReportProgress(80, "Growing...");
                Grow();
                if (Canceled) return null;

                if (genParams.AddFlowers) {
                    ReportProgress(90, "Planting flowers...");
                    PlantFlowers();
                    if (Canceled) return null;
                }

                if (genParams.AddMushrooms) {
                    ReportProgress(93, "Planting shrooms...");
                    PlantShrooms();
                    if (Canceled) return null;
                }

                if (genParams.AddTrees) {
                    ReportProgress(96, "Planting trees...");
                    PlantTrees();
                    if (Canceled) return null;
                }

                Result = map;
                return map;
            } finally {
                ReportProgress(100, Canceled ? "Canceled" : "Finished");
                Finished = true;
            }
        }


        // create the base heightmap
        void Raise() {
            Random raiseRand = new Random(random.Next());
            FilteredNoise raiseNoise1 = new FilteredNoise(new PerlinNoise(raiseRand, genParams.TerrainDetailOctaves),
                                                          new PerlinNoise(raiseRand, genParams.TerrainDetailOctaves));
            FilteredNoise raiseNoise2 = new FilteredNoise(new PerlinNoise(raiseRand, genParams.TerrainDetailOctaves),
                                                          new PerlinNoise(raiseRand, genParams.TerrainDetailOctaves));
            PerlinNoise raiseNoise3 = new PerlinNoise(raiseRand, genParams.TerrainFeatureOctaves);

            const double scale = 1.3;
            for (int x = 0; x < genParams.MapWidth; x++) {
                for (int y = 0; y < genParams.MapLength; y++) {
                    double d2 = raiseNoise1.GetNoise(x*scale, y*scale)/6 - 4;
                    double d3 = raiseNoise2.GetNoise(x*scale, y*scale)/5 + 10 - 4;
                    double d4 = raiseNoise3.GetNoise(x, y)/8;
                    if (d4 > 0) d3 = d2;
                    double elevation = Math.Max(d2, d3)/2;
                    if (elevation < 0) elevation *= 0.8;
                    heightmap[(x + y*genParams.MapWidth)] = (int)elevation;
                }
            }
        }


        // apply erosion effect on the heightmap
        void Erode() {
            Random erodeRand = new Random(random.Next());
            FilteredNoise erodeNoise1 = new FilteredNoise(new PerlinNoise(erodeRand, genParams.TerrainDetailOctaves),
                                                          new PerlinNoise(erodeRand, genParams.TerrainDetailOctaves));
            FilteredNoise erodeNoise2 = new FilteredNoise(new PerlinNoise(erodeRand, genParams.TerrainDetailOctaves),
                                                          new PerlinNoise(erodeRand, genParams.TerrainDetailOctaves));
            for (int x = 0; x < genParams.MapWidth; x++) {
                for (int y = 0; y < genParams.MapLength; y++) {
                    double d1 = erodeNoise1.GetNoise(x*2, y*2)/8;
                    int i7 = erodeNoise2.GetNoise(x*2, y*2) > 0 ? 1 : 0;
                    if (d1 <= 2) continue;
                    int i19 = ((heightmap[(x + y*genParams.MapWidth)] - i7)/2*2) + i7;
                    heightmap[(x + y*genParams.MapWidth)] = i19;
                }
            }
        }


        // fill the map with blocks based on the heightmap
        void Soil() {
            Random soilRand = new Random(random.Next());
            PerlinNoise soilNoise1 = new PerlinNoise(soilRand, 8);
            for (int x = 0; x < genParams.MapWidth; x++) {
                for (int y = 0; y < genParams.MapLength; y++) {
                    int i7 = (int)(soilNoise1.GetNoise(x, y)/24) - 4;
                    int i19 = heightmap[(x + y*genParams.MapWidth)] + waterLevel;
                    int i21 = i19 + i7;
                    heightmap[(x + y*genParams.MapWidth)] = Math.Max(i19, i21);
                    if (heightmap[(x + y*genParams.MapWidth)] > genParams.MapHeight - 2) heightmap[(x + y*genParams.MapWidth)] = (genParams.MapHeight - 2);
                    if (heightmap[(x + y*genParams.MapWidth)] < 1) heightmap[(x + y*genParams.MapWidth)] = 1;
                    for (int z = 0; z < genParams.MapHeight; z++) {
                        Block block = Block.Air;
                        if (z <= i19) block = Block.Dirt;
                        if (z <= i21) block = Block.Stone;
                        if (z == 0) block = Block.Lava;
                        int index = (z*genParams.MapLength + y)*genParams.MapWidth + x;
                        blocks[index] = (byte)block;
                    }
                }
            }
        }


        // fill everything at water level with water
        void Water() {
            Random waterRand = new Random(random.Next());
            for (int x = 0; x < genParams.MapWidth; x++) {
                FloodFill(x, 0, waterLevel - 1, Block.StillWater);
                FloodFill(x, genParams.MapLength - 1, waterLevel - 1, Block.StillWater);
            }
            for (int y = 0; y < genParams.MapLength; y++) {
                FloodFill(0, y, waterLevel - 1, Block.StillWater);
                FloodFill(genParams.MapWidth - 1, y, waterLevel - 1, Block.StillWater);
            }
            int maxWaterSpawns = genParams.MapWidth*genParams.MapLength/genParams.WaterSpawnDensity;
            for (int waterSpawn = 0; waterSpawn < maxWaterSpawns; waterSpawn++) {
                int x = waterRand.Next(genParams.MapWidth);
                int y = waterRand.Next(genParams.MapLength);
                int z = waterLevel - 1 - waterRand.Next(2);
                if (blocks[((z*genParams.MapLength + y)*genParams.MapWidth + x)] != (byte)Block.Air) continue;
                FloodFill(x, y, z, Block.StillWater);
            }
        }


        // randomly spreads lava underground
        void Melt() {
            Random meltRand = new Random(random.Next());
            int lavaSpawns = genParams.MapWidth*genParams.MapLength*genParams.MapHeight/genParams.LavaSpawnDensity;
            for (int lavaSpawn = 0; lavaSpawn < lavaSpawns; lavaSpawn++) {
                int x = meltRand.Next(genParams.MapWidth);
                int y = meltRand.Next(genParams.MapLength);
                // probability of lava spawning increases towards bottom of the map
                int z = (int)(meltRand.NextDouble()*meltRand.NextDouble()*(waterLevel - 3));
                if (blocks[((z*genParams.MapLength + y)*genParams.MapWidth + x)] != (byte)Block.Air) continue;
                FloodFill(x, y, z, Block.StillLava);
            }
        }


        // replaces dirt with sand, grass, or gravel
        void Grow() {
            PerlinNoise growNoise1 = new PerlinNoise(random, 8);
            PerlinNoise growNoise2 = new PerlinNoise(random, 8);
            for (int x = 0; x < genParams.MapWidth; x++) {
                for (int y = 0; y < genParams.MapLength; y++) {
                    int elevation = heightmap[(x + y*genParams.MapWidth)];
                    Block blockAbove = (Block)blocks[(((elevation + 1)*genParams.MapLength + y)*genParams.MapWidth + x)];
                    int index = (elevation*genParams.MapLength + y)*genParams.MapWidth + x;

                    if (blockAbove == Block.Air) {
                        bool placeSand = growNoise1.GetNoise(x, y) > 8;
                        if ((elevation <= waterLevel - 1) && placeSand) {
                            blocks[index] = (byte)Block.Sand;
                        } else {
                            blocks[index] = (byte)Block.Grass;
                        }
                    } else if (((blockAbove == Block.Water) || (blockAbove == Block.StillWater)) &&
                               (elevation <= waterLevel - 1)) {
                        bool placeGravel = growNoise2.GetNoise(x, y) > 12;
                        if (placeGravel) {
                            blocks[index] = (byte)Block.Gravel;
                        }
                    }
                }
            }
        }


        void PlantFlowers() {
            Random flowerRand = new Random(random.Next());
            int maxFlowers = genParams.MapWidth*genParams.MapLength/genParams.FlowerClusterDensity;
            for (int cluster = 0; cluster < maxFlowers; cluster++) {
                int flowerType = flowerRand.Next(2);
                int clusterX = flowerRand.Next(genParams.MapWidth);
                int clusterY = flowerRand.Next(genParams.MapLength);
                for (int flower = 0; flower < genParams.FlowerChainsPerCluster; flower++) {
                    int x = clusterX;
                    int y = clusterY;
                    for (int hop = 0; hop < genParams.FlowersPerChain; hop++) {
                        x += flowerRand.Next(genParams.FlowerSpread) - flowerRand.Next(genParams.FlowerSpread);
                        y += flowerRand.Next(genParams.FlowerSpread) - flowerRand.Next(genParams.FlowerSpread);
                        if ((x < 0) || (y < 0) || (x >= genParams.MapWidth) || (y >= genParams.MapLength)) continue;

                        int z = heightmap[(x + y*genParams.MapWidth)] + 1;
                        int index = Index(x, y, z);

                        Block blockAbove = (Block)blocks[index];
                        if (blockAbove != Block.Air) continue;
                        Block blockUnder = (Block)blocks[Index(x, y, z - 1)];
                        if (blockUnder != Block.Grass) continue;

                        if (flowerType == 0) {
                            blocks[index] = (byte)Block.YellowFlower;
                        } else {
                            blocks[index] = (byte)Block.RedFlower;
                        }
                    }
                }
            }
        }


        void PlantShrooms() {
            Random shroomRand = new Random(random.Next());
            int maxShrooms = genParams.MapWidth*genParams.MapLength*genParams.MapHeight/genParams.ShroomClusterDensity;
            for (int cluster = 0; cluster < maxShrooms; cluster++) {
                int shroomType = shroomRand.Next(2);
                int clusterX = shroomRand.Next(genParams.MapWidth);
                int clusterY = shroomRand.Next(genParams.MapLength);
                int clusterZ = shroomRand.Next(genParams.MapHeight);
                for (int shroom = 0; shroom < genParams.ShroomChainsPerCluster; shroom++) {
                    int x = clusterX;
                    int y = clusterY;
                    int z = clusterZ;
                    for (int hop = 0; hop < genParams.ShroomHopsPerChain; hop++) {
                        x += shroomRand.Next(genParams.ShroomSpreadHorizontal) -
                             shroomRand.Next(genParams.ShroomSpreadHorizontal);
                        y += shroomRand.Next(genParams.ShroomSpreadHorizontal) -
                             shroomRand.Next(genParams.ShroomSpreadHorizontal);
                        z += shroomRand.Next(genParams.ShroomSpreadVertical) -
                             shroomRand.Next(genParams.ShroomSpreadVertical);
                        if ((x < 0) || (y < 0) || (z < 1) || (x >= genParams.MapWidth) || (y >= genParams.MapLength) ||
                            (z >= heightmap[(x + y*genParams.MapWidth)] - 1)) continue;

                        int index = Index(x, y, z);
                        Block blockAbove = (Block)blocks[index];
                        if (blockAbove != Block.Air) continue;
                        Block blockUnder = (Block)blocks[Index(x, y, z - 1)];
                        if (blockUnder != Block.Stone) continue;

                        if (shroomType == 0) {
                            blocks[index] = (byte)Block.BrownMushroom;
                        } else {
                            blocks[index] = (byte)Block.RedMushroom;
                        }
                    }
                }
            }
        }


        void PlantTrees() {
            Random treeRand = new Random(random.Next());
            int maxTrees = genParams.MapWidth*genParams.MapLength/genParams.TreeClusterDensity;
            for (int cluster = 0; cluster < maxTrees; cluster++) {
                int clusterX = treeRand.Next(genParams.MapWidth);
                int clusterY = treeRand.Next(genParams.MapLength);
                for (int tree = 0; tree < genParams.TreeChainsPerCluster; tree++) {
                    int x = clusterX;
                    int y = clusterY;
                    for (int hop = 0; hop < genParams.TreeHopsPerChain; hop++) {
                        x += treeRand.Next(genParams.TreeSpread) - treeRand.Next(genParams.TreeSpread);
                        y += treeRand.Next(genParams.TreeSpread) - treeRand.Next(genParams.TreeSpread);
                        if ((x < 0) || (y < 0) || (x >= genParams.MapWidth) || (y >= genParams.MapLength)) continue;
                        if (treeRand.Next(genParams.TreePlantRatio) != 0) continue;
                        int z = heightmap[(x + y*genParams.MapWidth)] + 1;
                        GrowTree(treeRand, x, y, z);
                    }
                }
            }
        }


        // Plant a single tree - Based on Minecraft Classic's "com.mojang.minecraft.level.maybeGrowTree"
        void GrowTree([NotNull] Random treeRand, int startX, int startY, int startZ) {
            if (treeRand == null) throw new ArgumentNullException("treeRand");
            int treeHeight = treeRand.Next(3) + 4;

            Block blockUnder = map.GetBlock(startX, startY, startZ - 1);
            if ((blockUnder != Block.Grass) || (startZ >= map.Height - treeHeight - 1)) return;

            for (int z = startZ; z <= startZ + 1 + treeHeight; z++) {
                int extent = 1;
                if (z == startZ) extent = 0;
                if (z >= startZ + 1 + treeHeight - 2) extent = 2;
                for (int x = startX - extent; (x <= startX + extent); x++) {
                    for (int y = startY - extent; (y <= startY + extent); y++) {
                        if ((x >= 0) && (z >= 0) && (y >= 0) && (x < map.Width) && (z < map.Height) && (y < map.Length)) {
                            if (map.GetBlock(x, y, z) != Block.Air) return;
                        } else {
                            return;
                        }
                    }
                }
            }

            map.SetBlock(startX, startY, startZ - 1, Block.Dirt);

            for (int z = startZ - 3 + treeHeight; z <= startZ + treeHeight; z++) {
                int n = z - (startZ + treeHeight);
                int foliageExtent = 1 - n/2;
                for (int x = startX - foliageExtent; x <= startX + foliageExtent; x++) {
                    int j = x - startX;
                    for (int y = startY - foliageExtent; y <= startY + foliageExtent; y++) {
                        int i3 = y - startY;
                        if ((Math.Abs(j) == foliageExtent) && (Math.Abs(i3) == foliageExtent) &&
                            ((treeRand.Next(2) == 0) || (n == 0))) continue;
                        map.SetBlock(x, y, z, Block.Leaves);
                    }
                }
            }
            for (int z = 0; z < treeHeight; z++) {
                map.SetBlock(startX, startY, startZ + z, Block.Log);
            }
        }


        // Carve some caves underground. I have a very vague idea of how this works.
        void Carve() {
            Random carveRand = new Random(random.Next());
            int caveDensity = (int)Math.Round(BaseCaveDensity/genParams.CaveDensity);
            int maxCaves = genParams.MapWidth*genParams.MapLength*genParams.MapHeight/caveDensity/64*2;
            for (int i = 0; i < maxCaves; i++) {
                double startX = carveRand.NextDouble()*genParams.MapWidth;
                double startY = carveRand.NextDouble()*genParams.MapLength;
                double startZ = carveRand.NextDouble()*genParams.MapHeight;
                double f9 = carveRand.NextDouble()*Math.PI*2;
                double f10 = 0;
                double f11 = carveRand.NextDouble()*Math.PI*2;
                double f12 = 0;
                double f13 = carveRand.NextDouble()*carveRand.NextDouble();
                int caveLength = (int)((carveRand.NextDouble() + carveRand.NextDouble())*200);
                for (int step = 0; step < caveLength; step++) {
                    startX += Math.Sin(f9)*Math.Cos(f11);
                    startY += Math.Cos(f9)*Math.Cos(f11);
                    startZ += Math.Sin(f11);
                    f9 += f10*0.2;
                    f10 = f10*0.9 + (carveRand.NextDouble() - carveRand.NextDouble());
                    f11 = (f11 + f12*0.5)*0.5;
                    f12 = f12*0.75 + (carveRand.NextDouble() - carveRand.NextDouble());
                    if (carveRand.NextDouble() < 0.25) continue;
                    double f1 = startX + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f2 = startZ + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f5 = startY + (carveRand.NextDouble()*4 - 2)*0.2;
                    double f6 = (genParams.MapHeight - f2)/genParams.MapHeight;
                    f6 = 1.2 + (f6*3.5 + 1)*f13;
                    f6 = Math.Sin(step*Math.PI/caveLength)*f6;
                    for (int x = (int)(f1 - f6); x <= (int)(f1 + f6); x++) {
                        for (int z = (int)(f2 - f6); z <= (int)(f2 + f6); z++) {
                            for (int y = (int)(f5 - f6); y <= (int)(f5 + f6); y++) {
                                double f14 = x - f1;
                                double f15 = z - f2;
                                double f16 = y - f5;
                                f14 = f14*f14 + f15*f15*2 + f16*f16;
                                if ((f14 >= f6*f6) ||
                                    (x < 1) || (z < 1) || (y < 1) ||
                                    (x >= genParams.MapWidth - 1) || (z >= genParams.MapHeight - 1) ||
                                    (y >= genParams.MapLength - 1)) {
                                    continue;
                                }
                                int index = Index(x, y, z);
                                if ((Block)blocks[index] == Block.Stone) {
                                    blocks[index] = (byte)Block.Air;
                                }
                            }
                        }
                    }
                }
            }
        }


        void MakeOreVeins(Block oreTile, int density) {
            if (density < 1 || density > 500) {
                throw new ArgumentOutOfRangeException("density", "Ore density must be between 1 and 500");
            }
            Random oreVeinRand = new Random(random.Next());
            int maxVeins = genParams.MapWidth*genParams.MapLength*genParams.MapHeight/256/64*density/100;

            for (int vein = 0; vein < maxVeins; vein++) {
                double startX = oreVeinRand.NextDouble()*genParams.MapWidth;
                double startY = oreVeinRand.NextDouble()*genParams.MapLength;
                double startZ = oreVeinRand.NextDouble()*genParams.MapHeight;
                double f4 = oreVeinRand.NextDouble()*Math.PI*2;
                double f5 = 0;
                double f6 = oreVeinRand.NextDouble()*Math.PI*2;
                double f7 = 0;
                int m = (int)((oreVeinRand.NextDouble() + oreVeinRand.NextDouble())*75*density/100);
                for (int n = 0; n < m; n++) {
                    startX += Math.Sin(f4)*Math.Cos(f6);
                    startY += Math.Cos(f4)*Math.Sin(f6);
                    startZ += Math.Sin(f6);
                    f4 += f5*0.2;
                    f5 = (f5*0.9) + (oreVeinRand.NextDouble() - oreVeinRand.NextDouble());
                    f6 = (f6 + f7*0.5)*0.5;
                    f7 = (f7*0.9) + (oreVeinRand.NextDouble() - oreVeinRand.NextDouble());
                    double f8 = Math.Sin(n*Math.PI/m)*density/100 + 1;
                    for (int x = (int)(startX - f8); x <= (int)(startX + f8); x++) {
                        for (int z = (int)(startZ - f8); z <= (int)(startZ + f8); z++) {
                            for (int y = (int)(startY - f8); y <= (int)(startY + f8); y++) {
                                double f9 = x - startX;
                                double f10 = z - startZ;
                                double f11 = y - startY;
                                f9 = f9*f9 + f10*f10*2 + f11*f11;
                                if ((f9 >= f8*f8) || (x < 1) || (z < 1) || (y < 1) ||
                                    (x >= genParams.MapWidth - 1) || (z >= genParams.MapHeight - 1) ||
                                    (y >= genParams.MapLength - 1)) continue;
                                int index = Index(x, y, z);
                                if ((Block)blocks[index] == Block.Stone) {
                                    blocks[index] = (byte)oreTile;
                                }
                            }
                        }
                    }
                }
            }
        }


        void FloodFill(int x, int y, int z, Block newBlock) {
            if (blocks[Index(x, y, z)] != (byte)Block.Air) return;
            Vector3I coord = new Vector3I(x, y, z);
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push(coord);
            while (stack.Count > 0) {
                coord = stack.Pop();
                blocks[Index(coord.X, coord.Y, coord.Z)] = (byte)newBlock;
                if (coord.X + 1 < genParams.MapWidth &&
                    blocks[Index(coord.X + 1, coord.Y, coord.Z)] == (byte)Block.Air) {
                    stack.Push(new Vector3I(coord.X + 1, coord.Y, coord.Z));
                }
                if (coord.X - 1 >= 0 && blocks[Index(coord.X - 1, coord.Y, coord.Z)] == (byte)Block.Air) {
                    stack.Push(new Vector3I(coord.X - 1, coord.Y, coord.Z));
                }
                if (coord.Y + 1 < genParams.MapLength &&
                    blocks[Index(coord.X, coord.Y + 1, coord.Z)] == (byte)Block.Air) {
                    stack.Push(new Vector3I(coord.X, coord.Y + 1, coord.Z));
                }
                if (coord.Y - 1 >= 0 && blocks[Index(coord.X, coord.Y - 1, coord.Z)] == (byte)Block.Air) {
                    stack.Push(new Vector3I(coord.X, coord.Y - 1, coord.Z));
                }
                if (coord.Z - 1 >= 0 && blocks[Index(coord.X, coord.Y, coord.Z - 1)] == (byte)Block.Air) {
                    stack.Push(new Vector3I(coord.X, coord.Y, coord.Z - 1));
                }
            }
        }


        int Index(int x, int y, int z) {
            return (z*genParams.MapLength + y)*genParams.MapWidth + x;
        }
    }
}
