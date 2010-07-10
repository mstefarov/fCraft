// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace ConfigTool {
    internal partial class ColorPicker : Form {
        public static Dictionary<int, ColorPair> colors = new Dictionary<int, ColorPair>();
        static ColorPicker() {
            colors.Add( 0, new ColorPair( Color.White, Color.Black ) );
            colors.Add( 8, new ColorPair( Color.White, Color.DimGray ) );
            colors.Add( 1, new ColorPair( Color.White, Color.Navy ) );
            colors.Add( 9, new ColorPair( Color.White, Color.RoyalBlue ) );
            colors.Add( 2, new ColorPair( Color.White, Color.Green ) );
            colors.Add( 10, new ColorPair( Color.Black, Color.Lime ) );
            colors.Add( 3, new ColorPair( Color.White, Color.Teal ) );
            colors.Add( 11, new ColorPair( Color.Black, Color.Aqua ) );
            colors.Add( 4, new ColorPair( Color.White, Color.Maroon ) );
            colors.Add( 12, new ColorPair( Color.White, Color.Red ) );
            colors.Add( 5, new ColorPair( Color.White, Color.Purple ) );
            colors.Add( 13, new ColorPair( Color.Black, Color.Magenta ) );
            colors.Add( 6, new ColorPair( Color.White, Color.Olive ) );
            colors.Add( 14, new ColorPair( Color.Black, Color.Yellow ) );
            colors.Add( 7, new ColorPair( Color.Black, Color.Silver ) );
            colors.Add( 15, new ColorPair( Color.Black, Color.White ) );
        }

        public int color;
        public ColorPicker( string title, int oldColor ) {
            InitializeComponent();
            Text = title;
            color = oldColor;
            StartPosition = FormStartPosition.CenterParent;

            b0.Click += delegate( Object o, EventArgs a ) { color = 0; Close(); };
            b1.Click += delegate( Object o, EventArgs a ) { color = 1; Close(); };
            b2.Click += delegate( Object o, EventArgs a ) { color = 2; Close(); };
            b3.Click += delegate( Object o, EventArgs a ) { color = 3; Close(); };
            b4.Click += delegate( Object o, EventArgs a ) { color = 4; Close(); };
            b5.Click += delegate( Object o, EventArgs a ) { color = 5; Close(); };
            b6.Click += delegate( Object o, EventArgs a ) { color = 6; Close(); };
            b7.Click += delegate( Object o, EventArgs a ) { color = 7; Close(); };
            b8.Click += delegate( Object o, EventArgs a ) { color = 8; Close(); };
            b9.Click += delegate( Object o, EventArgs a ) { color = 9; Close(); };
            ba.Click += delegate( Object o, EventArgs a ) { color = 10; Close(); };
            bb.Click += delegate( Object o, EventArgs a ) { color = 11; Close(); };
            bc.Click += delegate( Object o, EventArgs a ) { color = 12; Close(); };
            bd.Click += delegate( Object o, EventArgs a ) { color = 13; Close(); };
            be.Click += delegate( Object o, EventArgs a ) { color = 14; Close(); };
            bf.Click += delegate( Object o, EventArgs a ) { color = 15; Close(); };
        }

        private void b0_Click( object sender, EventArgs e ) {
            color = 0;
            Close();
        }

        private void bCancel_Click( object sender, EventArgs e ) {
            Close();
        }
    }

    internal struct ColorPair {
        public ColorPair( Color _foreground, Color _background ) {
            foreground = _foreground;
            background = _background;
        }
        public Color foreground;
        public Color background;
    }
}
