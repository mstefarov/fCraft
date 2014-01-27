// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    // Small extension of PictureBox, that switches between HighQualityBicubic and NearestNeighbor
    // interpolation depending on image scale, to make the map images appear as sharp as possible.
    // Used by AddWorldPopup.
    sealed class CustomPictureBox : PictureBox {
        protected override void OnPaint( PaintEventArgs pe ) {
            if( Image != null ) {
                pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                pe.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                if( Image.Height * 3 > Height || Image.Width * 3 > Width ) {
                    pe.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                } else {
                    pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                }
            }
            base.OnPaint( pe );
        }
    }
}