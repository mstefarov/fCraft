// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.GUI {
    public class RealisticMapGenGuiProvider : IMapGeneratorGuiProvider {
        RealisticMapGenGuiProvider() { }

        public static readonly RealisticMapGenGuiProvider Instance = new RealisticMapGenGuiProvider();

        public string Name {
            get { return "RealisticMapGen GUI"; }
        }

        public Version Version {
            get { return new Version( 2, 1 ); }
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