// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    /// <summary> Represents a class that provides a GUI for choosing map generation parameters.
    /// Associated with specific IMapGenerator, by name. </summary>
    public interface IMapGeneratorGuiProvider {
        string Name { get; }
        string GeneratorName { get; }
        bool IsCompatible( Version generatorVersion );

        UserControl CreateGUI();

        void SetParameters( IMapGeneratorParameters generatorParameters );
        IMapGeneratorParameters GetParameters();
    }
}