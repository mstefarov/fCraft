// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using fCraft.MapConversion;
using fCraft.MapRendering;

namespace fCraft.MapRenderer {
    /// <summary> Holds all parameters for MapRenderer program, and sets some defaults. </summary>
    internal class MapRendererParams {
        public int Angle { get; set; }
        public IsoCatMode Mode { get; set; }
        public ImageFormat ExportFormat { get; set; }
        public string ImageFileExtension { get; set; }
        public BoundingBox Region { get; set; }
        public int JpegQuality { get; set; }
        public IMapImporter MapImporter { get; set; }
        public ImageCodecInfo ImageEncoder { get; set; }
        public bool DirectoryMode { get; set; }
        public int ThreadCount { get; set; }
        public string[] InputPathList { get; set; }
        public bool NoGradient { get; set; }
        public bool NoShadows { get; set; }
        public bool SeeThroughWater { get; set; }
        public bool SeeThroughLava { get; set; }
        public bool Recursive { get; set; }
        public bool AlwaysOverwrite { get; set; }
        public bool Uncropped { get; set; }
        public string InputFilter { get; set; }
        public bool OutputDirGiven { get; set; }
        public string OutputDirName { get; set; }
        public bool TryHard { get; set; }
        public bool UseRegex { get; set; }
        public Regex FilterRegex { get; set; }

        public MapRendererParams() {
            Mode = IsoCatMode.Normal;
            Region = BoundingBox.Empty;
            JpegQuality = 90;
            ThreadCount = Environment.ProcessorCount;
            ExportFormat = ImageFormat.Png;
            ImageFileExtension = ".png";
        }
    }
}
