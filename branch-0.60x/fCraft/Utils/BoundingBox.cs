// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary>
    /// Defines a 3D bounding box, in integer cartesian coordinates
    /// </summary>
    public sealed class BoundingBox {
        public static readonly BoundingBox Empty = new BoundingBox( 0, 0, 0, 0, 0, 0 );
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        public int XMin, YMin, ZMin, XMax, YMax, ZMax;
        // ReSharper restore FieldCanBeMadeReadOnly.Global


        /// <summary> Constructs a bounding box using two positions as opposite corners. </summary>
        public BoundingBox( Position p1, Position p2 ) :
            this( p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z ) {
        }


        /// <summary> Constructs a bounding box using two vectors as opposite corners. </summary>
        public BoundingBox( Vector3I p1, Vector3I p2 ) :
            this( p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z ) {
        }


        /// <summary> Constructs a bounding box at a given origin, with given dimensions. </summary>
        public BoundingBox( Position pos, int width, int length, int height ) :
            this( pos.X, pos.Y, pos.Z,
                  pos.X + width -1,
                  pos.Y + length -1,
                  pos.Z + height -1 ) {
        }


        /// <summary> Constructs a bounding box between two given coordinates. </summary>
        public BoundingBox( int x1, int y1, int z1, int x2, int y2, int z2 ) {
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            ZMin = Math.Min( z1, z2 );
            ZMax = Math.Max( z1, z2 );
        }


        /// <summary> Checks whether this bounding box intersects/touches another one. </summary>
        public bool Insersects( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return XMin > other.XMax || XMax < other.XMin ||
                   YMin > other.YMax || YMax < other.YMin ||
                   ZMin > other.ZMax || ZMax < other.ZMin;
        }


        /// <summary> Checks if another bounding box is wholly contained inside this one. </summary>
        public bool Contains( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return XMin >= other.XMin && XMax <= other.XMax &&
                   YMin >= other.YMin && YMax <= other.YMax &&
                   ZMin >= other.ZMin && ZMax <= other.ZMax;
        }


        /// <summary> Checks if a given point is inside this bounding box. </summary>
        public bool Contains( int x, int y, int z ) {
            return x >= XMin && x <= XMax &&
                   y >= YMin && y <= YMax &&
                   z >= ZMin && z <= ZMax;
        }


        /// <summary> Returns a BoundingBox object that describes the space shared between this and another box. </summary>
        /// <returns> Intersecting volume, or BoundingBox.Empty if there is no overlap. </returns>
        public BoundingBox GetIntersection( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            if( Insersects( other ) ) {
                return new BoundingBox( Math.Max( XMin, other.XMin ),
                                        Math.Max( YMin, other.YMin ),
                                        Math.Max( ZMin, other.ZMin ),
                                        Math.Min( XMax, other.XMax ),
                                        Math.Min( YMax, other.YMax ),
                                        Math.Min( ZMax, other.ZMax ) );
            } else {
                return Empty;
            }
        }


        public int Volume {
            get { return (XMax - XMin + 1) * (YMax - YMin + 1) * (ZMax - ZMin + 1); }
        }

        public int Width {
            get { return (XMax - XMin + 1); }
        }

        public int Length {
            get { return (YMax - YMin + 1); }
        }

        public int Height {
            get { return (ZMax - ZMin + 1); }
        }

        /// <summary> Returns the vertex closest to the origin, opposite MaxVertex. </summary>
        public Position MinVertex {
            get { return new Position( XMin, YMin, ZMin ); }
        }

        public Vector3I MinVertexV {
            get { return new Vector3I( XMin, YMin, ZMin ); }
        }

        /// <summary> Returns the vertex farthest from the origin, opposite MinVertex. </summary>
        public Position MaxVertex {
            get { return new Position( XMax, YMax, ZMax ); }
        }

        public Position Center {
            get { return new Position( (XMax - XMin) / 2, (YMax - YMin) / 2, (ZMax - ZMin) / 2 ); }
        }

        public Vector3I CenterV {
            get { return new Vector3I( (XMax - XMin) / 2, (YMax - YMin) / 2, (ZMax - ZMin) / 2 ); }
        }                       


        #region Serialization

        public const string XmlRootElementName = "BoundingBox";

        public BoundingBox( [NotNull] XElement root ) {
            if( root == null ) throw new ArgumentNullException( "root" );
            string[] coords = root.Value.Split( ' ' );
            int x1 = Int32.Parse( coords[0] );
            int x2 = Int32.Parse( coords[1] );
            int y1 = Int32.Parse( coords[2] );
            int y2 = Int32.Parse( coords[3] );
            int z1 = Int32.Parse( coords[4] );
            int z2 = Int32.Parse( coords[5] );
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            ZMin = Math.Min( z1, z2 );
            ZMax = Math.Max( z1, z2 );
        }

        public XElement Serialize( string tagName ) {
            string data = String.Format( "{0} {1} {2} {3} {4} {5}",
                                         XMin, XMax, YMin, YMax, ZMin, ZMax );
            return new XElement( tagName, data );
        }

        public XElement Serialize() {
            return Serialize( XmlRootElementName );
        }

        #endregion
    }
}