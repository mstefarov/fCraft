﻿// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.GUI {
    /// <summary> Represents a class that provides a GUI for chosen map generation parameters.
    /// Creates IMapGeneratorGui on demand.
    /// Associated with specific MapGenerator, by name. </summary>
    public interface IMapGeneratorGuiProvider {
        string Name { get; }
        Version Version { get; }
        bool IsCompatible( string generatorName, Version generatorVersion );

        MapGeneratorGui CreateGui();
    }
}