// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml.Linq;

namespace fCraft {

    /// <summary>
    /// Defines a 3D bounding box, in integer cartesian coordinates
    /// </summary>
    public sealed class BoundingBox {
        public int XMin, YMin, HMin, XMax, YMax, HMax;

        public BoundingBox( Position p1, Position p2 ) :
            this( p1.X, p1.Y, p1.H, p2.X, p2.Y, p2.H ) {
        }

        public BoundingBox( Position pos, int widthX, int widthY, int height ) :
            this( pos.X, pos.Y, pos.H, pos.X + widthX, pos.Y + widthY, pos.H + height ) {
        }

        public BoundingBox( int x1, int y1, int h1, int x2, int y2, int h2 ) {
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            HMin = Math.Min( h1, h2 );
            HMax = Math.Max( h1, h2 );
        }

        public bool Insersects( BoundingBox other ) {
            return XMin > other.XMax || XMax < other.XMin ||
                   YMin > other.YMax || YMax < other.YMin ||
                   HMin > other.HMax || HMax < other.HMin;
        }

        public bool Contains( BoundingBox other ) {
            return XMin >= other.XMin && XMax <= other.XMax &&
                   YMin >= other.YMin && YMax <= other.YMax &&
                   HMin >= other.HMin && HMax <= other.HMax;
        }

        public bool Contains( int x, int y, int h ) {
            return x >= XMin && x <= XMax &&
                   y >= YMin && y <= YMax &&
                   h >= HMin && h <= HMax;
        }

        public BoundingBox GetIntersection( BoundingBox other ) {
            return new BoundingBox( Math.Max( XMin, other.XMin ),
                                    Math.Max( YMin, other.YMin ),
                                    Math.Max( HMin, other.HMin ),
                                    Math.Min( XMax, other.XMax ),
                                    Math.Min( YMax, other.YMax ),
                                    Math.Min( HMax, other.HMax ) );
        }

        public int GetVolume() {
            return (XMax - XMin + 1) * (YMax - YMin + 1) * (HMax - HMin + 1);
        }

        public int GetWidthX() {
            return (XMax - XMin + 1);
        }

        public int GetWidthY() {
            return (YMax - YMin + 1);
        }

        public int GetHeight() {
            return (HMax - HMin + 1);
        }

        public const string XmlRootElementName = "BoundingBox";

        public BoundingBox( XElement root ) {
            string[] coords = root.Value.Split( ' ' );
            int x1 = Int32.Parse( coords[0] );
            int x2 = Int32.Parse( coords[1] );
            int y1 = Int32.Parse( coords[2] );
            int y2 = Int32.Parse( coords[3] );
            int h1 = Int32.Parse( coords[4] );
            int h2 = Int32.Parse( coords[5] );
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            HMin = Math.Min( h1, h2 );
            HMax = Math.Max( h1, h2 );
        }

        public XElement Serialize() {
            return new XElement( XmlRootElementName,
                                 String.Format( "{0} {1} {2} {3} {4} {5}",
                                                XMin, XMax, YMin, YMax, HMin, HMax ) );
        }

    }
}