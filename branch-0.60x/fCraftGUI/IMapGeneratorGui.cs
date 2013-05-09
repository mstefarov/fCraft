// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.GUI {
    /// <summary> Class that provides a GUI (UserControl) for adjusting map parameters. </summary>
    /// <remarks>It would make sense for this class to be abstract, but VisualStudio's Designer
    /// does not support controls derived from abstract classes.
    /// See http://stackoverflow.com/questions/2764757/ </remarks>
    public class MapGeneratorGui : UserControl {
        public virtual void SetParameters( IMapGeneratorParameters generatorParameters ) {
            throw new NotImplementedException();
        }

        public virtual IMapGeneratorParameters GetParameters() {
            throw new NotImplementedException();
        }
    }
}