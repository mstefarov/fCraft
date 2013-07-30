// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.GUI {
    public partial class FlatMapGenGui : MapGeneratorGui {
        MapGeneratorParameters genParams;

        public FlatMapGenGui() {
            InitializeComponent();
        }

        public override void SetParameters( MapGeneratorParameters generatorParameters ) {
            genParams = generatorParameters;
            pgDetails.SelectedObject = genParams;
            lPreset.Text = "Preset: " + genParams.ToString().Split( ' ' )[1];
        }

        public override MapGeneratorParameters GetParameters() {
            return genParams;
        }

        public override void OnMapDimensionChange( int width, int length, int height ) {
            genParams.MapWidth = width;
            genParams.MapLength = length;
            genParams.MapHeight = height;
        }

        private void xCustom_CheckedChanged( object sender, EventArgs e ) {
            pgDetails.Visible = xCustom.Checked;
        }

        private void pgDetails_PropertyValueChanged( object s, PropertyValueChangedEventArgs e ) {
            lPreset.Text = "Preset: " + genParams.ToString().Split( ' ' )[1] +" (Modified)";
        }
    }


    /// <summary> Represents a class that provides a fallback GUI for any MapGenerator.
    /// Creates DefaultMapGenGui instances on demand. </summary>
    public class FlatMapGenGuiProvider : IMapGeneratorGuiProvider {
        FlatMapGenGuiProvider() { }

        public static readonly FlatMapGenGuiProvider Instance = new FlatMapGenGuiProvider();

        public string Name {
            get { return "Flat"; }
        }

        static readonly Version StaticVersion = new Version( 1, 0 );

        public Version Version {
            get { return StaticVersion; }
        }

        public bool IsCompatible( string generatorName, Version generatorVersion ) {
            return true;
        }

        public MapGeneratorGui CreateGui() {
            return new FlatMapGenGui();
        }
    }
}
