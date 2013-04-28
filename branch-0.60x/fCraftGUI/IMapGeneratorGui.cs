// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Windows.Forms;

namespace fCraft.GUI {
    public abstract class MapGeneratorGui : UserControl {
        public abstract void SetParameters( IMapGeneratorParameters generatorParameters );
        public abstract IMapGeneratorParameters GetParameters();
    }
}