// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft.MapRenderer {
    /// <summary> Holds all information related to a single map-rendering task. </summary>
    class RenderTask {
        public Byte[] Result { get; set; }
        public Map Map { get; set; }
        public Exception Exception { get; set; }
        public string MapPath { get; private set; }
        public string TargetPath { get; private set; }
        public string RelativeName { get; private set; }

        public RenderTask( string mapPath, string targetPath, string relativeName ) {
            MapPath = mapPath;
            TargetPath = targetPath;
            RelativeName = relativeName;
        }
    }
}