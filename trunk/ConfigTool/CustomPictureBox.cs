﻿using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ConfigTool {
    class CustomPictureBox : PictureBox {
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