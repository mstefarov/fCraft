// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.GUI {
    /// <summary> Represents a class that provides a fallback GUI for any MapGenerator.
    /// Creates DefaultMapGenGui instances on demand. </summary>
    public class DefaultMapGenGuiProvider : IMapGeneratorGuiProvider {
        DefaultMapGenGuiProvider() {}

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