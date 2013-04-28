// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.GUI {
    public partial class RealisticMapGenGui : MapGeneratorGui {
        public RealisticMapGenGui() {
            InitializeComponent();
        }

        public override void SetParameters( IMapGeneratorParameters generatorParameters ) {
            throw new NotImplementedException();
        }

        public override IMapGeneratorParameters GetParameters() {
            throw new NotImplementedException();
        }
    }
}
