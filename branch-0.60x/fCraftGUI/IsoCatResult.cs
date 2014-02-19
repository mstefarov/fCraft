// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System.Drawing;
using JetBrains.Annotations;

namespace fCraft.GUI {
    public sealed class IsoCatResult {
        internal IsoCatResult( bool cancelled, [CanBeNull] Bitmap bitmap, Rectangle cropRectangle ) {
            Cancelled = cancelled;
            Bitmap = bitmap;
            CropRectangle = cropRectangle;
        }

        public bool Cancelled { get; private set; }

        [CanBeNull]
        public Bitmap Bitmap { get; private set; }

        public Rectangle CropRectangle { get; private set; }
    }
}
