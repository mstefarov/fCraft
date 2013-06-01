using System;

namespace fCraft.MapRenderer {
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