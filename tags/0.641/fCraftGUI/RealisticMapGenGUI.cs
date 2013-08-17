// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;

namespace fCraft.GUI {
    public partial class RealisticMapGenGui : MapGeneratorGui {
        int mapWidth,
            mapLength,
            mapHeight;

        RealisticMapGenParameters genParameters = new RealisticMapGenParameters();


        public RealisticMapGenGui() {
            InitializeComponent();

            cTheme.Items.AddRange( Enum.GetNames( typeof( MapGenTheme ) ) );
        }

        public override void SetParameters( MapGeneratorParameters generatorParameters ) {
            genParameters = (RealisticMapGenParameters)generatorParameters;
            LoadGeneratorArgs();
        }

        public override MapGeneratorParameters GetParameters() {
            if( !xSeed.Checked ) {
                nSeed.Value = GetRandomSeed();
            }
            SaveGeneratorArgs();
            return genParameters;
        }

        public override void OnMapDimensionChange( int width, int length, int height ) {
            mapWidth = width;
            mapLength = length;
            mapHeight = height;
        }


        readonly Random rand = new Random();

        int GetRandomSeed() {
            return rand.Next() - rand.Next();
        }


        void LoadGeneratorArgs() {
            cTheme.SelectedIndex = (int)genParameters.Theme.Theme;

            sDetailScale.Value = genParameters.DetailScale;
            sFeatureScale.Value = genParameters.FeatureScale;

            xLayeredHeightmap.Checked = genParameters.LayeredHeightmap;
            xMarbledMode.Checked = genParameters.MarbledHeightmap;
            xMatchWaterCoverage.Checked = genParameters.MatchWaterCoverage;
            xInvert.Checked = genParameters.InvertHeightmap;

            nMaxDepth.Value = genParameters.MaxDepth;
            nMaxHeight.Value = genParameters.MaxHeight;
            sRoughness.Value = (int)(genParameters.Roughness*100);
            nSeed.Value = genParameters.Seed;
            xAddWater.Checked = genParameters.AddWater;

            if( genParameters.UseBias ) sBias.Value = (int)(genParameters.Bias*100);
            else sBias.Value = 0;
            xDelayBias.Checked = genParameters.DelayBias;

            sWaterCoverage.Value = (int)(100*genParameters.WaterCoverage);
            cMidpoint.SelectedIndex = genParameters.MidPoint + 1;
            nRaisedCorners.Value = genParameters.RaisedCorners;
            nLoweredCorners.Value = genParameters.LoweredCorners;

            xAddTrees.Checked = genParameters.AddTrees;
            xGiantTrees.Checked = genParameters.AddGiantTrees;
            nTreeHeight.Value = (genParameters.TreeHeightMax + genParameters.TreeHeightMin)/2m;
            nTreeHeightVariation.Value = (genParameters.TreeHeightMax - genParameters.TreeHeightMin)/2m;
            nTreeSpacing.Value = (genParameters.TreeSpacingMax + genParameters.TreeSpacingMin)/2m;
            nTreeSpacingVariation.Value = (genParameters.TreeSpacingMax - genParameters.TreeSpacingMin)/2m;

            xAddCaves.Checked = genParameters.AddCaves;
            xCaveLava.Checked = genParameters.AddCaveLava;
            xCaveWater.Checked = genParameters.AddCaveWater;
            xAddOre.Checked = genParameters.AddOre;
            sCaveDensity.Value = (int)(genParameters.CaveDensity*100);
            sCaveSize.Value = (int)(genParameters.CaveSize*100);

            xWaterLevel.Checked = genParameters.CustomWaterLevel;
            nWaterLevel.Maximum = genParameters.MapHeight;
            nWaterLevel.Value = Math.Min( genParameters.WaterLevel, genParameters.MapHeight );

            xAddSnow.Checked = genParameters.AddSnow;

            nSnowAltitude.Value = genParameters.SnowAltitude -
                                  (genParameters.CustomWaterLevel ? genParameters.WaterLevel : genParameters.MapHeight/2);
            nSnowTransition.Value = genParameters.SnowTransition;

            xAddCliffs.Checked = genParameters.AddCliffs;
            sCliffThreshold.Value = (int)(genParameters.CliffThreshold*100);
            xCliffSmoothing.Checked = genParameters.CliffSmoothing;

            xAddBeaches.Checked = genParameters.AddBeaches;
            nBeachExtent.Value = genParameters.BeachExtent;
            nBeachHeight.Value = genParameters.BeachHeight;

            sAboveFunc.Value = ExponentToTrackBar( sAboveFunc, genParameters.AboveFuncExponent );
            sBelowFunc.Value = ExponentToTrackBar( sBelowFunc, genParameters.BelowFuncExponent );

            nMaxHeightVariation.Value = genParameters.MaxHeightVariation;
            nMaxDepthVariation.Value = genParameters.MaxDepthVariation;

            xAddFloodBarrier.Checked = genParameters.AddFloodBarrier;
        }

        void SaveGeneratorArgs() {
            genParameters = new RealisticMapGenParameters {
                DetailScale = sDetailScale.Value,
                FeatureScale = sFeatureScale.Value,
                MapHeight = mapHeight,
                MapWidth = mapWidth,
                MapLength = mapLength,
                LayeredHeightmap = xLayeredHeightmap.Checked,
                MarbledHeightmap = xMarbledMode.Checked,
                MatchWaterCoverage = xMatchWaterCoverage.Checked,
                MaxDepth = (int)nMaxDepth.Value,
                MaxHeight = (int)nMaxHeight.Value,
                AddTrees = xAddTrees.Checked,
                AddGiantTrees = xGiantTrees.Checked,
                Roughness = sRoughness.Value/100f,
                Seed = (int)nSeed.Value,
                Theme = new RealisticMapGenBlockTheme( (MapGenTheme)cTheme.SelectedIndex ),
                TreeHeightMax = (int)(nTreeHeight.Value + nTreeHeightVariation.Value),
                TreeHeightMin = (int)(nTreeHeight.Value - nTreeHeightVariation.Value),
                TreeSpacingMax = (int)(nTreeSpacing.Value + nTreeSpacingVariation.Value),
                TreeSpacingMin = (int)(nTreeSpacing.Value - nTreeSpacingVariation.Value),
                UseBias = (sBias.Value != 0),
                DelayBias = xDelayBias.Checked,
                WaterCoverage = sWaterCoverage.Value/100f,
                Bias = sBias.Value/100f,
                MidPoint = cMidpoint.SelectedIndex - 1,
                RaisedCorners = (int)nRaisedCorners.Value,
                LoweredCorners = (int)nLoweredCorners.Value,
                InvertHeightmap = xInvert.Checked,
                AddWater = xAddWater.Checked,
                AddCaves = xAddCaves.Checked,
                AddOre = xAddOre.Checked,
                AddCaveLava = xCaveLava.Checked,
                AddCaveWater = xCaveWater.Checked,
                CaveDensity = sCaveDensity.Value/100f,
                CaveSize = sCaveSize.Value/100f,
                CustomWaterLevel = xWaterLevel.Checked,
                WaterLevel = (int)(xWaterLevel.Checked ? nWaterLevel.Value : mapHeight/2),
                AddSnow = xAddSnow.Checked,
                SnowTransition = (int)nSnowTransition.Value,
                SnowAltitude = (int)(nSnowAltitude.Value + (xWaterLevel.Checked ? nWaterLevel.Value : mapHeight/2)),
                AddCliffs = xAddCliffs.Checked,
                CliffThreshold = sCliffThreshold.Value/100f,
                CliffSmoothing = xCliffSmoothing.Checked,
                AddBeaches = xAddBeaches.Checked,
                BeachExtent = (int)nBeachExtent.Value,
                BeachHeight = (int)nBeachHeight.Value,
                AboveFuncExponent = TrackBarToExponent( sAboveFunc ),
                BelowFuncExponent = TrackBarToExponent( sBelowFunc ),
                MaxHeightVariation = (int)nMaxHeightVariation.Value,
                MaxDepthVariation = (int)nMaxDepthVariation.Value,
                AddFloodBarrier = xAddFloodBarrier.Checked
            };
        }


        void xAddCaves_CheckedChanged( object sender, EventArgs e ) {
            gCaves.Visible = xAddCaves.Checked && xAdvanced.Checked;
        }

        void sCaveDensity_ValueChanged( object sender, EventArgs e ) {
            lCaveDensityDisplay.Text = sCaveDensity.Value + "%";
        }

        void sCaveSize_ValueChanged( object sender, EventArgs e ) {
            lCaveSizeDisplay.Text = sCaveSize.Value + "%";
        }

        void xWaterLevel_CheckedChanged( object sender, EventArgs e ) {
            nWaterLevel.Enabled = xWaterLevel.Checked;
        }

        void xAddTrees_CheckedChanged( object sender, EventArgs e ) {
            gTrees.Visible = xAddTrees.Checked;
        }

        void xAddWater_CheckedChanged( object sender, EventArgs e ) {
            xAddBeaches.Enabled = xAddWater.Checked;
        }

        void sAboveFunc_ValueChanged( object sender, EventArgs e ) {
            lAboveFuncUnits.Text = (1/TrackBarToExponent( sAboveFunc )).ToString( "0.0%" );
        }

        void sBelowFunc_ValueChanged( object sender, EventArgs e ) {
            lBelowFuncUnits.Text = (1/TrackBarToExponent( sBelowFunc )).ToString( "0.0%" );
        }

        static float TrackBarToExponent( TrackBar bar ) {
            if( bar.Value >= bar.Maximum/2 ) {
                float normalized = (bar.Value - bar.Maximum/2f)/(bar.Maximum/2f);
                return 1 + normalized*normalized*3;
            } else {
                float normalized = (bar.Value/(bar.Maximum/2f));
                return normalized*.75f + .25f;
            }
        }

        static int ExponentToTrackBar( TrackBar bar, float val ) {
            if( val >= 1 ) {
                float normalized = (float)Math.Sqrt( (val - 1)/3f );
                return (int)(bar.Maximum/2f + normalized*(bar.Maximum/2f));
            } else {
                float normalized = (val - .25f)/.75f;
                return (int)(normalized*bar.Maximum/2f);
            }
        }

        void sCliffThreshold_ValueChanged( object sender, EventArgs e ) {
            lCliffThresholdUnits.Text = sCliffThreshold.Value + "%";
        }

        void xAddSnow_CheckedChanged( object sender, EventArgs e ) {
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
        }

        void xAddCliffs_CheckedChanged( object sender, EventArgs e ) {
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
        }

        void xAddBeaches_CheckedChanged( object sender, EventArgs e ) {
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }




        void xAdvanced_CheckedChanged( object sender, EventArgs e ) {
            gTerrainFeatures.Visible = xAdvanced.Checked;
            gHeightmapCreation.Visible = xAdvanced.Checked;
            gTrees.Visible = xAdvanced.Checked && xAddTrees.Checked;
            gCaves.Visible = xAddCaves.Checked && xAdvanced.Checked;
            gSnow.Visible = xAdvanced.Checked && xAddSnow.Checked;
            gCliffs.Visible = xAdvanced.Checked && xAddCliffs.Checked;
            gBeaches.Visible = xAdvanced.Checked && xAddBeaches.Checked;
        }


        void MapDimensionChanged( object sender, EventArgs e ) {
            sFeatureScale.Maximum = (int)Math.Log( (double)Math.Max( mapWidth, mapLength ), 2 );
            int value = sDetailScale.Maximum - sDetailScale.Value;
            sDetailScale.Maximum = sFeatureScale.Maximum;
            sDetailScale.Value = sDetailScale.Maximum - value;

            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
        }


        void sFeatureScale_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sFeatureScale.Maximum - sFeatureScale.Value);
            lFeatureSizeDisplay.Text = resolution + "×" + resolution;
            if( sDetailScale.Value < sFeatureScale.Value ) {
                sDetailScale.Value = sFeatureScale.Value;
            }
        }


        void sDetailScale_ValueChanged( object sender, EventArgs e ) {
            int resolution = 1 << (sDetailScale.Maximum - sDetailScale.Value);
            lDetailSizeDisplay.Text = resolution + "×" + resolution;
            if( sFeatureScale.Value > sDetailScale.Value ) {
                sFeatureScale.Value = sDetailScale.Value;
            }
        }


        void xMatchWaterCoverage_CheckedChanged( object sender, EventArgs e ) {
            sWaterCoverage.Enabled = xMatchWaterCoverage.Checked;
        }


        void sWaterCoverage_ValueChanged( object sender, EventArgs e ) {
            lMatchWaterCoverageDisplay.Text = sWaterCoverage.Value + "%";
        }


        void sBias_ValueChanged( object sender, EventArgs e ) {
            lBiasDisplay.Text = sBias.Value + "%";
            bool useBias = (sBias.Value != 0);

            nRaisedCorners.Enabled = useBias;
            nLoweredCorners.Enabled = useBias;
            cMidpoint.Enabled = useBias;
            xDelayBias.Enabled = useBias;
        }


        void sRoughness_ValueChanged( object sender, EventArgs e ) {
            lRoughnessDisplay.Text = sRoughness.Value + "%";
        }


        void xSeed_CheckedChanged( object sender, EventArgs e ) {
            nSeed.Enabled = xSeed.Checked;
        }


        void nRaisedCorners_ValueChanged( object sender, EventArgs e ) {
            nLoweredCorners.Value = Math.Min( 4 - nRaisedCorners.Value, nLoweredCorners.Value );
        }


        void nLoweredCorners_ValueChanged( object sender, EventArgs e ) {
            nRaisedCorners.Value = Math.Min( 4 - nLoweredCorners.Value, nRaisedCorners.Value );
        }
    }


    public class RealisticMapGenGuiProvider : IMapGeneratorGuiProvider {
        RealisticMapGenGuiProvider() { }

        public static readonly RealisticMapGenGuiProvider Instance = new RealisticMapGenGuiProvider();

        public string Name {
            get { return "RealisticMapGen GUI"; }
        }

        static readonly Version StaticVersion = new Version( 2, 1 );
        public Version Version {
            get { return StaticVersion; }
        }

        public bool IsCompatible( string generatorName, Version generatorVersion ) {
            return generatorName == RealisticMapGen.Instance.Name &&
                   generatorVersion.Major == 2 &&
                   generatorVersion.Minor == 1;
        }

        public MapGeneratorGui CreateGui() {
            return new RealisticMapGenGui();
        }
    }
}