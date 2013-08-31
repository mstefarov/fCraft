// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using fCraft.MapGeneration;

namespace fCraft.GUI {
    public partial class DefaultMapGenGui : MapGeneratorGui {
        MapGeneratorParameters args;

        public DefaultMapGenGui() {
            InitializeComponent();
        }

        public override void SetParameters( MapGeneratorParameters generatorParameters ) {
            args = generatorParameters;
            pgGrid.SelectedObject = args;
        }

        public override MapGeneratorParameters GetParameters() {
            return args;
        }

        public override void OnMapDimensionChange( int width, int length, int height ) {
            args.MapWidth = width;
            args.MapLength = length;
            args.MapHeight = height;
        }
    }


    /// <summary> Represents a class that provides a fallback GUI for any MapGenerator.
    /// Creates DefaultMapGenGui instances on demand. </summary>
    public class DefaultMapGenGuiProvider : IMapGeneratorGuiProvider {
        DefaultMapGenGuiProvider() { }

        public static readonly DefaultMapGenGuiProvider Instance = new DefaultMapGenGuiProvider();

        public string Name {
            get { return "Default"; }
        }

        static readonly Version StaticVersion = new Version( 1, 0 );

        public Version Version {
            get { return StaticVersion; }
        }

        public bool IsCompatible( string generatorName, Version generatorVersion ) {
            return true;
        }

        public MapGeneratorGui CreateGui() {
            return new DefaultMapGenGui();
        }
    }
}