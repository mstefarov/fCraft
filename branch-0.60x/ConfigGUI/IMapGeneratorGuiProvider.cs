// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    interface IMapGeneratorGuiProvider {
        string Name { get; }
        string GeneratorName { get; }
        bool IsCompatible( Version generatorVersion );

        Panel CreateGUI( WorldListEntry world );
    }
}
