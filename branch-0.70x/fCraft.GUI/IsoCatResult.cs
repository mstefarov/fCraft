// Part of fCraft | Copyright 2009-2012 Matvei Stefarov <me@matvei.org> | MIT License
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