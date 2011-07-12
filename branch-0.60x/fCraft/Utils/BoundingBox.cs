// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml.Linq;

namespace fCraft {

    /// <summary>
    /// Defines a 3D bounding box, in integer cartesian coordinates
    /// </summary>
    public sealed class BoundingBox {
        public static readonly BoundingBox Empty = new BoundingBox( 0, 0, 0, 0, 0, 0 );
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        public int XMin, YMin, HMin, XMax, YMax, HMax;
        // ReSharper restore FieldCanBeMadeReadOnly.Global


        /// <summary> Constructs a bounding box using two positions as opposite corners. </summary>
        public BoundingBox( Position p1, Position p2 ) :
            this( p1.X, p1.Y, p1.H, p2.X, p2.Y, p2.H ) {
        }


        /// <summary> Constructs a bounding box at a given origin, with given dimensions. </summary>
        public BoundingBox( Position pos, int widthX, int widthY, int height ) :
            this( pos.X, pos.Y, pos.H,
                  pos.X + widthX -1,
                  pos.Y + widthY -1,
                  pos.H + height -1 ) {
        }


        /// <summary> Constructs a bounding box between two given coordinates. </summary>
        public BoundingBox( int x1, int y1, int h1, int x2, int y2, int h2 ) {
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            HMin = Math.Min( h1, h2 );
            HMax = Math.Max( h1, h2 );
        }


        /// <summary> Checks whether this bounding box intersects/touches another one. </summary>
        public bool Insersects( BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return XMin > other.XMax || XMax < other.XMin ||
                   YMin > other.YMax || YMax < other.YMin ||
                   HMin > other.HMax || HMax < other.HMin;
        }


        /// <summary> Checks if another bounding box is wholly contained inside this one. </summary>
        public bool Contains( BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return XMin >= other.XMin && XMax <= other.XMax &&
                   YMin >= other.YMin && YMax <= other.YMax &&
                   HMin >= other.HMin && HMax <= other.HMax;
        }


        /// <summary> Checks if a given point is inside this bounding box. </summary>
        public bool Contains( int x, int y, int h ) {
            return x >= XMin && x <= XMax &&
                   y >= YMin && y <= YMax &&
                   h >= HMin && h <= HMax;
        }


        /// <summary> Returns a BoundingBox object that describes the space shared between this and another box. </summary>
        /// <returns> Intersecting volume, or BoundingBox.Empty if there is no overlap. </returns>
        public BoundingBox GetIntersection( BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            if( Insersects( other ) ) {
                return new BoundingBox( Math.Max( XMin, other.XMin ),
                                        Math.Max( YMin, other.YMin ),
                                        Math.Max( HMin, other.HMin ),
                                        Math.Min( XMax, other.XMax ),
                                        Math.Min( YMax, other.YMax ),
                                        Math.Min( HMax, other.HMax ) );
            } else {
                return Empty;
            }
        }


        public int Volume {
            get { return (XMax - XMin + 1) * (YMax - YMin + 1) * (HMax - HMin + 1); }
        }

        public int WidthX {
            get { return (XMax - XMin + 1); }
        }

        public int WidthY {
            get { return (YMax - YMin + 1); }
        }

        public int Height {
            get { return (HMax - HMin + 1); }
        }

        /// <summary> Returns the vertex closest to the origin, opposite MaxVertex. </summary>
        public Position MinVertex {
            get { return new Position( XMin, YMin, HMin ); }
        }

        /// <summary> Returns the vertex farthest from the origin, opposite MinVertex. </summary>
        public Position MaxVertex {
            get { return new Position( XMax, YMax, HMax ); }
        }


        #region Serialization

        public const string XmlRootElementName = "BoundingBox";

        public BoundingBox( XElement root ) {
            if( root == null ) throw new ArgumentNullException( "root" );
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

        public XElement Serialize( string tagName ) {
            string data = String.Format( "{0} {1} {2} {3} {4} {5}",
                                         XMin, XMax, YMin, YMax, HMin, HMax );
            return new XElement( tagName, data );
        }

        public XElement Serialize() {
            return Serialize( XmlRootElementName );
        }

        #endregion
    }
}