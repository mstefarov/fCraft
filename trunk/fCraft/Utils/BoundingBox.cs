// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml.Linq;

namespace fCraft {

    /// <summary>
    /// Defines a 3D bounding box, in integer cartesian coordinates
    /// </summary>
    public sealed class BoundingBox {
        public int xMin, yMin, hMin, xMax, yMax, hMax;

        public BoundingBox( Position p1, Position p2 ) :
            this( p1.x, p1.y, p1.h, p2.x, p2.y, p2.h ) {
        }

        public BoundingBox( Position pos, int widthX, int widthY, int height ) :
            this( pos.x, pos.y, pos.h, pos.x + widthX, pos.y + widthY, pos.h + height ) {
        }

        public BoundingBox( int x1, int y1, int h1, int x2, int y2, int h2 ) {
            xMin = Math.Min( x1, x2 );
            xMax = Math.Max( x1, x2 );
            yMin = Math.Min( y1, y2 );
            yMax = Math.Max( y1, y2 );
            hMin = Math.Min( h1, h2 );
            hMax = Math.Max( h1, h2 );
        }

        public bool Insersects( BoundingBox other ) {
            return xMin > other.xMax || xMax < other.xMin ||
                   yMin > other.yMax || yMax < other.yMin ||
                   hMin > other.hMax || hMax < other.hMin;
        }

        public bool Contains( BoundingBox other ) {
            return xMin >= other.xMin && xMax <= other.xMax &&
                   yMin >= other.yMin && yMax <= other.yMax &&
                   hMin >= other.hMin && hMax <= other.hMax;
        }

        public bool Contains( int x, int y, int h ) {
            return x >= xMin && x <= xMax &&
                   y >= yMin && y <= yMax &&
                   h >= hMin && h <= hMax;
        }


        public int GetVolume() {
            return (xMax - xMin + 1) * (yMax - yMin + 1) * (hMax - hMin + 1);
        }

        public int GetWidthX() {
            return (xMax - xMin + 1);
        }

        public int GetWidthY() {
            return (yMax - yMin + 1);
        }

        public int GetHeight() {
            return (hMax - hMin + 1);
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
            xMin = Math.Min( x1, x2 );
            xMax = Math.Max( x1, x2 );
            yMin = Math.Min( y1, y2 );
            yMax = Math.Max( y1, y2 );
            hMin = Math.Min( h1, h2 );
            hMax = Math.Max( h1, h2 );
        }

        public XElement Serialize() {
            return new XElement( XmlRootElementName,
                                 String.Format( "{0} {1} {2} {3} {4} {5}",
                                                xMin, xMax, yMin, yMax, hMin, hMax ) );
        }

    }
}