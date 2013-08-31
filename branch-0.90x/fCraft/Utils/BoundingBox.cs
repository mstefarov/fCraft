// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Defines a 3D bounding box, in integer cartesian coordinates.
    /// Coordinates are always inclusive, so note that even a bounding box with coinciding vertices is a 1x1x1 cube,
    /// and has non-zero volume and non-zero dimensions. </summary>
    public sealed class BoundingBox : IEquatable<BoundingBox>, ICloneable {
        /// <summary> Empty BoundingBox (1x1x1), at origin (0,0,0). </summary>
        public static readonly BoundingBox Empty = new BoundingBox( 0, 0, 0, 0, 0, 0 );

        public int XMin, YMin, ZMin, XMax, YMax, ZMax;


        /// <summary> Constructs a bounding box using two vectors as opposite corners. </summary>
        public BoundingBox( Vector3I p1, Vector3I p2 ) :
            this( p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z ) {}


        /// <summary> Creates a copy of the given BoundingBox. </summary>
        public BoundingBox( BoundingBox other ) :
            this( other.XMin, other.YMin, other.ZMin, other.XMax, other.YMax, other.ZMax ) {}


        /// <summary> Constructs a bounding box at a given origin, with given dimensions. </summary>
        /// <param name="origin"> Origin point of the bounding box. </param>
        /// <param name="width"> Width (X-axis, horizontal). May be negative. </param>
        /// <param name="length"> Length (Y-axis, horizontal). May be negative. </param>
        /// <param name="height"> Width (Z-axis, vertical). May be negative. </param>
        public BoundingBox( Vector3I origin, int width, int length, int height ) :
            this( origin.X, origin.Y, origin.Z,
                  origin.X + width - 1,
                  origin.Y + length - 1,
                  origin.Z + height - 1 ) {}


        /// <summary> Constructs a bounding box between two given coordinates. </summary>
        public BoundingBox( int x1, int y1, int z1, int x2, int y2, int z2 ) {
            XMin = Math.Min( x1, x2 );
            XMax = Math.Max( x1, x2 );
            YMin = Math.Min( y1, y2 );
            YMax = Math.Max( y1, y2 );
            ZMin = Math.Min( z1, z2 );
            ZMax = Math.Max( z1, z2 );
        }


        #region Collision Detection

        /// <summary> Checks whether this bounding box intersects/touches another one. </summary>
        /// <exception cref="ArgumentNullException"> other is null. </exception>
        [Pure]
        public bool Intersects( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return !( XMax < other.XMin || XMin > other.XMax ||
                      YMax < other.YMin || YMin > other.YMax ||
                      ZMax < other.ZMin || ZMin > other.ZMax );
        }


        /// <summary> Checks if another bounding box is wholly contained inside this one. </summary>
        /// <exception cref="ArgumentNullException"> other is null. </exception>
        [Pure]
        public bool Contains( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return XMin <= other.XMin && XMax >= other.XMax &&
                   YMin <= other.YMin && YMax >= other.YMax &&
                   ZMin <= other.ZMin && ZMax >= other.ZMax;
        }


        /// <summary> Checks if a given point is inside this bounding box. </summary>
        [Pure]
        public bool Contains( int x, int y, int z ) {
            return x >= XMin && x <= XMax &&
                   y >= YMin && y <= YMax &&
                   z >= ZMin && z <= ZMax;
        }


        /// <summary> Checks if a given point is inside this bounding box. </summary>
        [Pure]
        public bool Contains( Vector3I point ) {
            return point.X >= XMin && point.X <= XMax &&
                   point.Y >= YMin && point.Y <= YMax &&
                   point.Z >= ZMin && point.Z <= ZMax;
        }


        /// <summary> Returns a BoundingBox object that describes the space shared between this and another box. </summary>
        /// <returns> Intersecting volume, or BoundingBox.Empty if there is no overlap. </returns>
        /// <exception cref="ArgumentNullException"> other is null. </exception>
        [Pure]
        public BoundingBox GetIntersection( [NotNull] BoundingBox other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            if( Contains( other ) ) {
                return other;
            } else if( other.Contains( this ) ) {
                return this;
            } else if( Intersects( other ) ) {
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

        #endregion


        /// <summary> Returns volume of this bounding box. Guaranteed to be at least 1. </summary>
        public int Volume {
            get { return ( XMax - XMin + 1 ) * ( YMax - YMin + 1 ) * ( ZMax - ZMin + 1 ); }
        }


        /// <summary> Gets a vector of the box's dimensions: (width,length,height) - that's (x,y,z).
        /// Guaranteed to be non-zero in every direction. </summary>
        public Vector3I Dimensions {
            get {
                return new Vector3I( XMax - XMin + 1,
                                     YMax - YMin + 1,
                                     ZMax - ZMin + 1 );
            }
        }


        /// <summary> Width of the bounding box (XMax - XMin + 1). Inclusive, and always at least 1. </summary>
        public int Width {
            get { return ( XMax - XMin + 1 ); }
        }

        /// <summary> Width of the bounding box (YMax - YMin + 1). Notch's Z. Inclusive, and always at least 1. </summary>
        public int Length {
            get { return ( YMax - YMin + 1 ); }
        }

        /// <summary> Width of the bounding box (ZMax - ZMin + 1). Notch's Y. Inclusive, and always at least 1. </summary>
        public int Height {
            get { return ( ZMax - ZMin + 1 ); }
        }


        /// <summary> Gets or sets the vertex closest to the coordinate origin, opposite MaxVertex.
        /// When setting, MaxVertex may be adjusted as well, to keep minimums/maximums consistent. </summary>
        public Vector3I MinVertex {
            get { return new Vector3I( XMin, YMin, ZMin ); }
            set {
                XMin = value.X;
                YMin = value.Y;
                ZMin = value.Z;
                XMax = Math.Max( value.X, XMax );
                YMax = Math.Max( value.Y, YMax );
                ZMax = Math.Max( value.Z, ZMax );
            }
        }


        /// <summary> Gets or sets the vertex farthest from the origin, opposite MinVertex.
        /// When setting, MaxVertex may be adjusted as well, to keep minimums/maximums consistent. </summary>
        public Vector3I MaxVertex {
            get { return new Vector3I( XMax, YMax, ZMax ); }
            set {
                XMin = Math.Min( value.X, XMin );
                YMin = Math.Min( value.Y, YMin );
                ZMin = Math.Min( value.Z, ZMin );
                XMax = value.X;
                YMax = value.Y;
                ZMax = value.Z;
            }
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


        public XElement Serialize( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );
            string data = String.Format( "{0} {1} {2} {3} {4} {5}",
                                         XMin, XMax, YMin, YMax, ZMin, ZMax );
            return new XElement( tagName, data );
        }


        public XElement Serialize() {
            return Serialize( XmlRootElementName );
        }

        #endregion


        public bool Equals( [NotNull] BoundingBox other ) {
            return XMin == other.XMin && XMax == other.XMax &&
                   YMin == other.YMin && YMax == other.YMax &&
                   ZMin == other.ZMin && ZMax == other.ZMax;
        }


        public override string ToString() {
            return "BoundingBox" + Dimensions;
        }


        public object Clone() {
            return new BoundingBox( XMin, YMin, ZMin, XMax, YMax, ZMax );
        }
    }
}