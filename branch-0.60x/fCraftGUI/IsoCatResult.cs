// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Drawing;

namespace fCraft.GUI {
    public class IsoCatResult {
        internal IsoCatResult( bool canceled, Bitmap bitmap, Rectangle cropRectangle ) {
            Canceled = canceled;
            Bitmap = bitmap;
            CropRectangle = cropRectangle;
        }

        public bool Canceled { get; private set; }
        public Bitmap Bitmap { get; private set; }
        public Rectangle CropRectangle { get; set; }
    }
}