// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Contains parameters for advanced map generation. </summary>
    public sealed partial class RealisticMapGenParameters : MapGeneratorParameters {
        const int FormatVersion = 2;

        public int   Seed { get; set; }

        public int   MaxHeight { get; set; }
        public int   MaxDepth { get; set; }
        public int   MaxHeightVariation { get; set; }
        public int   MaxDepthVariation { get; set; }

        public bool  AddWater { get; set; }
        public bool  CustomWaterLevel { get; set; }
        public bool  MatchWaterCoverage { get; set; }
        public int   WaterLevel { get; set; }
        public float WaterCoverage { get; set; }

        public bool  UseBias { get; set; }
        public bool  DelayBias { get; set; }
        public float Bias { get; set; }
        public int   RaisedCorners { get; set; }
        public int   LoweredCorners { get; set; }
        public int   MidPoint { get; set; }

        public int   DetailScale { get; set; }
        public int   FeatureScale { get; set; }
        public float Roughness { get; set; }
        public bool  LayeredHeightmap { get; set; }
        public bool  MarbledHeightmap { get; set; }
        public bool  InvertHeightmap { get; set; }
        public float AboveFuncExponent { get; set; }
        public float BelowFuncExponent { get; set; }

        public bool  AddTrees { get; set; }
        public bool  AddGiantTrees { get; set; }
        public int   TreeSpacingMin { get; set; }
        public int   TreeSpacingMax { get; set; }
        public int   TreeHeightMin { get; set; }
        public int   TreeHeightMax { get; set; }

        public bool  AddCaves { get; set; }
        public bool  AddOre { get; set; }
        public bool  AddCaveWater { get; set; }
        public bool  AddCaveLava { get; set; }
        public float CaveDensity { get; set; }
        public float CaveSize { get; set; }

        public bool  AddSnow { get; set; }
        public int   SnowAltitude { get; set; }
        public int   SnowTransition { get; set; }

        public bool  AddCliffs { get; set; }
        public bool  CliffSmoothing { get; set; }
        public float CliffThreshold { get; set; }
        public float CliffsideBlockThreshold { get; set; }

        public bool  AddBeaches { get; set; }
        public int   BeachExtent { get; set; }
        public int   BeachHeight { get; set; }

        public bool  AddFloodBarrier { get; set; }

        // block selection for voxelization
        public RealisticMapGenTheme Theme { get; set; }


        /// <summary> Checks constraints on all the parameters' values, throws ArgumentException if there are any violations. </summary>
        public void Validate() {
            if( !Map.IsValidDimension( MapWidth ) || !Map.IsValidDimension( MapLength ) ||
                !Map.IsValidDimension( MapHeight ) ) {
                throw new ArgumentException( "One or more of the map dimensions is not valid." );
            }

            if( AddWater ) {
                if( CustomWaterLevel && (WaterLevel < 0 || WaterLevel > MapHeight) ) {
                    throw new ArgumentException( "WaterLevel must be between 0 and MapHeight (inclusive)." );
                }

                if( WaterCoverage < 0 || WaterCoverage > 1 ) {
                    throw new ArgumentException( "WaterCoverage must be between 0 and 1 (inclusive)." );
                }
            }

            if( UseBias &&
                (RaisedCorners < 0 || RaisedCorners > 4 || LoweredCorners < 0 || RaisedCorners > 4 ||
                 RaisedCorners + LoweredCorners > 4) ) {
                throw new ArgumentException(
                    "The sum of RaisedCorners and LoweredCorners must be between 0 and 4 (inclusive)." );
            }

            if( DetailScale < 0 || FeatureScale < 0 ) {
                throw new ArgumentException( "DetailScale and FeatureScale must be greater than 0." );
            }

            if( DetailScale < FeatureScale ) {
                throw new ArgumentException( "DetailScale must be equal to or greater than FeatureScale." );
            }

            if( AddTrees ) {
                if( TreeSpacingMax < 1 ) {
                    throw new ArgumentException( "TreeSpacingMax must be greater than 0." );
                }

                if( TreeSpacingMin < 1 || TreeSpacingMin > TreeSpacingMax ) {
                    throw new ArgumentException(
                        "TreeSpacingMin must be greater than 0, and no greater than TreeSpacingMax." );
                }

                if( TreeHeightMax < 1 ) {
                    throw new ArgumentException( "TreeHeightMax must be greater than 0." );
                }

                if( TreeHeightMin < 1 || TreeHeightMin > TreeHeightMax ) {
                    throw new ArgumentException(
                        "TreeHeightMin must be greater than 0, and no greater than TreeHeightMax." );
                }
            }

            if( AddCaves && (CaveDensity < 0 || CaveSize < 0) ) {
                throw new ArgumentException( "CaveDensity and CaveSize must not be negative." );
            }

            if( AddSnow && (SnowAltitude < 0 || SnowAltitude > MapHeight) ) {
                throw new ArgumentException( "SnowAltitude must be between 0 and MapHeight (inclusive)." );
            }

            if( AddCliffs && (CliffThreshold < 0 || CliffThreshold > 1) ) {
                throw new ArgumentException( "CliffThreshold must be between 0 and 1 (inclusive)." );
            }

            if( AddBeaches && (BeachExtent < 0 || BeachHeight < 0) ) {
                throw new ArgumentException( "BeachExtent and BeachHeight must not be negative." );
            }
        }


        public void ApplyDefaults() {
            Theme = new RealisticMapGenTheme( MapGenTheme.Forest );
            Seed = (new Random()).Next();

            // default map dimensions
            MapWidth = 256;
            MapLength = 256;
            MapHeight = 96;

            // default terrain elevation / depth
            MaxHeight = 20;
            MaxDepth = 12;
            MaxHeightVariation = 4;
            MaxDepthVariation = 0;

            // water defaults: 50% water level, approx 50% coverage
            AddWater = true;
            CustomWaterLevel = false;
            MatchWaterCoverage = false;
            WaterLevel = 48;
            WaterCoverage = .5f;

            // bias defaults (no bias at all)
            UseBias = false;
            DelayBias = false;
            Bias = 0;
            RaisedCorners = 0;
            LoweredCorners = 0;
            MidPoint = 0;

            // default heightmap filtering options
            DetailScale = 7;
            FeatureScale = 1;
            Roughness = .5f;
            LayeredHeightmap = false;
            MarbledHeightmap = false;
            InvertHeightmap = false;
            AboveFuncExponent = 1;
            BelowFuncExponent = 1;

            // default tree params (small tress only)
            AddTrees = true;
            AddGiantTrees = false;
            TreeSpacingMin = 7;
            TreeSpacingMax = 11;
            TreeHeightMin = 5;
            TreeHeightMax = 7;

            // default cave/ore params (all off)
            AddCaves = false;
            AddOre = false;
            AddCaveWater = false;
            AddCaveLava = false;
            CaveDensity = 2;
            CaveSize = 1;

            // default snow params (off)
            AddSnow = false;
            SnowAltitude = 70;
            SnowTransition = 7;

            // default cliff params (on)
            AddCliffs = true;
            CliffSmoothing = true;
            CliffThreshold = 1;
            CliffsideBlockThreshold = 0.01f;

            // default beach params (off)
            AddBeaches = false;
            BeachExtent = 6;
            BeachHeight = 2;
        }


        public RealisticMapGenParameters() {
            Generator = RealisticMapGen.Instance;
            ApplyDefaults();
        }


        public RealisticMapGenParameters( [NotNull] XElement root )
            : this() {
            if( root == null ) throw new ArgumentNullException( "root" );

            XAttribute versionTag = root.Attribute( "version" );
            int version = 0;
            if( versionTag != null && !String.IsNullOrEmpty(versionTag.Value) ) {
                version = Int32.Parse( versionTag.Value );
            }

            XElement el = root.Element( "theme" );
            if( el != null ) {
                string themeVal = el.Value;
                MapGenTheme theme;
                if( EnumUtil.TryParse( themeVal, out theme, true ) ) {
                    // for old versions of MapGen templates, use enum
                    Theme = new RealisticMapGenTheme( theme );
                } else {
                    // for newer versions, use the whole custom thing
                    Theme = new RealisticMapGenTheme( el );
                }
            }

            Seed = Int32.Parse( root.Element( "seed" ).Value );
            MapWidth = Int32.Parse( root.Element( "dimX" ).Value );
            MapLength = Int32.Parse( root.Element( "dimY" ).Value );
            MapHeight = Int32.Parse( root.Element( "dimH" ).Value );
            MaxHeight = Int32.Parse( root.Element( "maxHeight" ).Value );
            MaxDepth = Int32.Parse( root.Element( "maxDepth" ).Value );

            AddWater = Boolean.Parse( root.Element( "addWater" ).Value );
            if( root.Element( "customWaterLevel" ) != null ) CustomWaterLevel = Boolean.Parse( root.Element( "customWaterLevel" ).Value );
            MatchWaterCoverage = Boolean.Parse( root.Element( "matchWaterCoverage" ).Value );
            WaterLevel = Int32.Parse( root.Element( "waterLevel" ).Value );
            WaterCoverage = float.Parse( root.Element( "waterCoverage" ).Value );

            UseBias = Boolean.Parse( root.Element( "useBias" ).Value );
            if( root.Element( "delayBias" ) != null ) DelayBias = Boolean.Parse( root.Element( "delayBias" ).Value );
            Bias = float.Parse( root.Element( "bias" ).Value );
            RaisedCorners = Int32.Parse( root.Element( "raisedCorners" ).Value );
            LoweredCorners = Int32.Parse( root.Element( "loweredCorners" ).Value );
            MidPoint = Int32.Parse( root.Element( "midPoint" ).Value );

            if( version == 0 ) {
                DetailScale = Int32.Parse( root.Element( "minDetailSize" ).Value );
                FeatureScale = Int32.Parse( root.Element( "maxDetailSize" ).Value );
            } else {
                DetailScale = Int32.Parse( root.Element( "detailScale" ).Value );
                FeatureScale = Int32.Parse( root.Element( "featureScale" ).Value );
            }
            Roughness = float.Parse( root.Element( "roughness" ).Value );
            LayeredHeightmap = Boolean.Parse( root.Element( "layeredHeightmap" ).Value );
            MarbledHeightmap = Boolean.Parse( root.Element( "marbledHeightmap" ).Value );
            InvertHeightmap = Boolean.Parse( root.Element( "invertHeightmap" ).Value );
            if( root.Element( "aboveFuncExponent" ) != null ) AboveFuncExponent = float.Parse( root.Element( "aboveFuncExponent" ).Value );
            if( root.Element( "belowFuncExponent" ) != null ) BelowFuncExponent = float.Parse( root.Element( "belowFuncExponent" ).Value );

            AddTrees = Boolean.Parse( root.Element( "addTrees" ).Value );
            TreeSpacingMin = Int32.Parse( root.Element( "treeSpacingMin" ).Value );
            TreeSpacingMax = Int32.Parse( root.Element( "treeSpacingMax" ).Value );
            TreeHeightMin = Int32.Parse( root.Element( "treeHeightMin" ).Value );
            TreeHeightMax = Int32.Parse( root.Element( "treeHeightMax" ).Value );

            if( root.Element( "addCaves" ) != null ) {
                AddCaves = Boolean.Parse( root.Element( "addCaves" ).Value );
                AddCaveLava = Boolean.Parse( root.Element( "addCaveLava" ).Value );
                AddCaveWater = Boolean.Parse( root.Element( "addCaveWater" ).Value );
                AddOre = Boolean.Parse( root.Element( "addOre" ).Value );
                CaveDensity = float.Parse( root.Element( "caveDensity" ).Value );
                CaveSize = float.Parse( root.Element( "caveSize" ).Value );
            }

            if( root.Element( "addSnow" ) != null ) AddSnow = Boolean.Parse( root.Element( "addSnow" ).Value );
            if( root.Element( "snowAltitude" ) != null ) SnowAltitude = Int32.Parse( root.Element( "snowAltitude" ).Value );
            if( root.Element( "snowTransition" ) != null ) SnowTransition = Int32.Parse( root.Element( "snowTransition" ).Value );

            if( root.Element( "addCliffs" ) != null ) AddCliffs = Boolean.Parse( root.Element( "addCliffs" ).Value );
            if( root.Element( "cliffSmoothing" ) != null ) CliffSmoothing = Boolean.Parse( root.Element( "cliffSmoothing" ).Value );
            if( root.Element( "cliffThreshold" ) != null ) CliffThreshold = float.Parse( root.Element( "cliffThreshold" ).Value );

            if( root.Element( "addBeaches" ) != null ) AddBeaches = Boolean.Parse( root.Element( "addBeaches" ).Value );
            if( root.Element( "beachExtent" ) != null ) BeachExtent = Int32.Parse( root.Element( "beachExtent" ).Value );
            if( root.Element( "beachHeight" ) != null ) BeachHeight = Int32.Parse( root.Element( "beachHeight" ).Value );

            if( root.Element( "maxHeightVariation" ) != null ) MaxHeightVariation = Int32.Parse( root.Element( "maxHeightVariation" ).Value );
            if( root.Element( "maxDepthVariation" ) != null ) MaxDepthVariation = Int32.Parse( root.Element( "maxDepthVariation" ).Value );

            if( root.Element( "addGiantTrees" ) != null ) AddGiantTrees = Boolean.Parse( root.Element( "addGiantTrees" ).Value );

            Validate();
        }


        const string LegacyRootTagName = "fCraftMapGeneratorArgs";


        public override void Save( XElement root ) {
            root.Add( new XAttribute( "version", FormatVersion ) );

            root.Add( new XElement( "theme", Theme ) );
            root.Add( new XElement( "seed", Seed ) );
            root.Add( new XElement( "dimX", MapWidth ) );
            root.Add( new XElement( "dimY", MapLength ) );
            root.Add( new XElement( "dimH", MapHeight ) );
            root.Add( new XElement( "maxHeight", MaxHeight ) );
            root.Add( new XElement( "maxDepth", MaxDepth ) );

            root.Add( new XElement( "addWater", AddWater ) );
            root.Add( new XElement( "customWaterLevel", CustomWaterLevel ) );
            root.Add( new XElement( "matchWaterCoverage", MatchWaterCoverage ) );
            root.Add( new XElement( "waterLevel", WaterLevel ) );
            root.Add( new XElement( "waterCoverage", WaterCoverage ) );

            root.Add( new XElement( "useBias", UseBias ) );
            root.Add( new XElement( "delayBias", DelayBias ) );
            root.Add( new XElement( "raisedCorners", RaisedCorners ) );
            root.Add( new XElement( "loweredCorners", LoweredCorners ) );
            root.Add( new XElement( "midPoint", MidPoint ) );
            root.Add( new XElement( "bias", Bias ) );

            root.Add( new XElement( "detailScale", DetailScale ) );
            root.Add( new XElement( "featureScale", FeatureScale ) );
            root.Add( new XElement( "roughness", Roughness ) );
            root.Add( new XElement( "layeredHeightmap", LayeredHeightmap ) );
            root.Add( new XElement( "marbledHeightmap", MarbledHeightmap ) );
            root.Add( new XElement( "invertHeightmap", InvertHeightmap ) );
            root.Add( new XElement( "aboveFuncExponent", AboveFuncExponent ) );
            root.Add( new XElement( "belowFuncExponent", BelowFuncExponent ) );

            root.Add( new XElement( "addTrees", AddTrees ) );
            root.Add( new XElement( "addGiantTrees", AddGiantTrees ) );
            root.Add( new XElement( "treeSpacingMin", TreeSpacingMin ) );
            root.Add( new XElement( "treeSpacingMax", TreeSpacingMax ) );
            root.Add( new XElement( "treeHeightMin", TreeHeightMin ) );
            root.Add( new XElement( "treeHeightMax", TreeHeightMax ) );

            root.Add( new XElement( "addCaves", AddCaves ) );
            root.Add( new XElement( "addCaveLava", AddCaveLava ) );
            root.Add( new XElement( "addCaveWater", AddCaveWater ) );
            root.Add( new XElement( "addOre", AddOre ) );
            root.Add( new XElement( "caveDensity", CaveDensity ) );
            root.Add( new XElement( "caveSize", CaveSize ) );

            root.Add( new XElement( "addSnow", AddSnow ) );
            root.Add( new XElement( "snowAltitude", SnowAltitude ) );
            root.Add( new XElement( "snowTransition", SnowTransition ) );

            root.Add( new XElement( "addCliffs", AddCliffs ) );
            root.Add( new XElement( "cliffSmoothing", CliffSmoothing ) );
            root.Add( new XElement( "cliffThreshold", CliffThreshold ) );

            root.Add( new XElement( "addBeaches", AddBeaches ) );
            root.Add( new XElement( "beachExtent", BeachExtent ) );
            root.Add( new XElement( "beachHeight", BeachHeight ) );

            root.Add( new XElement( "maxHeightVariation", MaxHeightVariation ) );
            root.Add( new XElement( "maxDepthVariation", MaxDepthVariation ) );
        }


        public override object Clone() {
            return new RealisticMapGenParameters {
                AboveFuncExponent = AboveFuncExponent,
                AddBeaches = AddBeaches,
                AddCaveLava = AddCaveLava,
                AddCaves = AddCaves,
                AddCaveWater = AddCaveWater,
                AddCliffs = AddCliffs,
                AddGiantTrees = AddGiantTrees,
                AddOre = AddOre,
                AddSnow = AddSnow,
                AddTrees = AddTrees,
                AddWater = AddWater,
                BeachExtent = BeachExtent,
                BeachHeight = BeachHeight,
                BelowFuncExponent = BelowFuncExponent,
                Bias = Bias,
                CaveDensity = CaveDensity,
                CaveSize = CaveSize,
                CliffSmoothing = CliffSmoothing,
                CliffThreshold = CliffThreshold,
                CustomWaterLevel = CustomWaterLevel,
                DelayBias = DelayBias,
                DetailScale = DetailScale,
                FeatureScale = FeatureScale,
                Generator = Generator,
                InvertHeightmap = InvertHeightmap,
                LayeredHeightmap = LayeredHeightmap,
                LoweredCorners = LoweredCorners,
                MapHeight = MapHeight,
                MapLength = MapLength,
                MapWidth = MapWidth,
                MarbledHeightmap = MarbledHeightmap,
                MatchWaterCoverage = MatchWaterCoverage,
                MaxDepth = MaxDepth,
                MaxDepthVariation = MaxDepthVariation,
                MaxHeight = MaxHeight,
                MaxHeightVariation = MaxHeightVariation,
                MidPoint = MidPoint,
                RaisedCorners = RaisedCorners,
                Roughness = Roughness,
                Seed = Seed,
                SnowAltitude = SnowAltitude,
                SnowTransition = SnowTransition,
                Theme = Theme,
                TreeHeightMax = TreeHeightMax,
                TreeHeightMin = TreeHeightMin,
                TreeSpacingMax = TreeSpacingMax,
                TreeSpacingMin = TreeSpacingMin,
                UseBias = UseBias,
                WaterCoverage = WaterCoverage,
                WaterLevel = WaterLevel
            };
        }


        public override MapGeneratorState CreateGenerator() {
            return new RealisticMapGenState( this );
        }
    }
}