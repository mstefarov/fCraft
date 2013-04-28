// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.GUI {
    class RealisticMapGenGuiProvider : IMapGeneratorGuiProvider {
        public string Name {
            get { return "RealisticMapGen GUI"; }
        }

        public Version Version {
            get { return new Version( 2, 1 ); }
        }

        public string GeneratorName {
            get { return RealisticMapGen.Instance.Name; }
        }

        public bool IsCompatible( Version generatorVersion ) {
            return (generatorVersion.Major == 2 && generatorVersion.Minor == 1);
        }

        public MapGeneratorGui CreateGUI() {
            return new RealisticMapGenGui();
        }
    }
}