// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary>
    /// Integer 3D vector, used by Forester.
    /// </summary>
    public struct Vector3i : IEquatable<Vector3i>, IComparable<Vector3i>, IComparable<Vector3f> {
        public int X, Y, Z;


        public Vector3i( int x, int y, int h ) {
            X = x;
            Z = y;
            Y = h;
        }

        public Vector3i( Vector3i other ) {
            X = other.X;
            Z = other.Z;
            Y = other.Y;
        }

        public Vector3i( Vector3f other ) {
            X = (int)other.X;
            Z = (int)other.Y;
            Y = (int)other.Z;
        }


        public float Length {
            get {
                return (float)Math.Sqrt( X * X + Y * Y + Z * Z );
            }
        }

        public int LengthSquared {
            get {
                return X * X + Y * Y + Z * Z;
            }
        }


        public int this[int i] {
            get {
                switch( i ) {
                    case 0: return X;
                    case 1: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case 0: X = value; return;
                    case 1: Y = value; return;
                    default: Z = value; return;
                }
            }
        }


        #region Operations

        public static Vector3i operator +( Vector3i a, Vector3i b ) {
            return new Vector3i( a.X + b.X, a.Z + b.Z, a.Y + b.Y );
        }

        public static Vector3i operator +( Vector3i a, int scalar ) {
            return new Vector3i( a.X + scalar, a.Z + scalar, a.Y + scalar );
        }

        public static Vector3i operator -( Vector3i a, Vector3i b ) {
            return new Vector3i( a.X - b.X, a.Z - b.Z, a.Y - b.Y );
        }

        public static Vector3i operator -( Vector3i a, int scalar ) {
            return new Vector3i( a.X - scalar, a.Z - scalar, a.Y - scalar );
        }

        public static Vector3i operator *( Vector3i a, double scalar ) {
            return new Vector3i( (int)(a.X * scalar), (int)(a.Z * scalar), (int)(a.Y * scalar) );
        }

        public static Vector3i operator /( Vector3i a, double scalar ) {
            return new Vector3i( (int)(a.X / scalar), (int)(a.Z / scalar), (int)(a.Y / scalar) );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3i ) {
                return this.Equals( (Vector3i)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3i other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3i a, Vector3i b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3i a, Vector3i b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return X + Z * 1625 + Y * 2642245;
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3i other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }

        public int CompareTo( Vector3f other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }


        public static bool operator >( Vector3i a, Vector3i b ) {
            return a.LengthSquared > b.LengthSquared;
        }

        public static bool operator <( Vector3i a, Vector3i b ) {
            return a.LengthSquared < b.LengthSquared;
        }

        public static bool operator >=( Vector3i a, Vector3i b ) {
            return a.LengthSquared >= b.LengthSquared;
        }

        public static bool operator <=( Vector3i a, Vector3i b ) {
            return a.LengthSquared <= b.LengthSquared;
        }

        #endregion


        public int GetLargestComponent() {
            int maxVal = Math.Max( Math.Abs( X ), Math.Max( Math.Abs( Y ), Math.Abs( Z ) ) );
            if( maxVal == Math.Abs( X ) ) return 0;
            if( maxVal == Math.Abs( Y ) ) return 1;
            return 2;
        }

        public override string ToString() {
            return String.Format( "Vector({0},{1},{2})", X, Y, Z );
        }
    }


    /// <summary>
    /// Floating-point (float) 3D vector, used by Forester
    /// </summary>
    public struct Vector3f : IEquatable<Vector3f>, IComparable<Vector3i>, IComparable<Vector3f> {

        public float X, Y, Z;


        public Vector3f( float x, float y, float z ) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3f( Vector3f other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3f( Vector3i other ) {
            X = other.X;
            Y = other.Z;
            Z = other.Y;
        }


        public float Length {
            get {
                return (float)Math.Sqrt( X * X + Y * Y + Z * Z );
            }
        }

        public float LengthSquared {
            get {
                return X * X + Y * Y + Z * Z;
            }
        }


        public float this[int i] {
            get {
                switch( i ) {
                    case 0: return X;
                    case 1: return Z;
                    default: return Y;
                }
            }
            set {
                switch( i ) {
                    case 0: X = value; return;
                    case 1: Z = value; return;
                    default: Y = value; return;
                }
            }
        }


        #region Operators

        public static Vector3f operator +( Vector3f a, Vector3f b ) {
            return new Vector3f( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3f operator +( Vector3f a, float scalar ) {
            return new Vector3f( a.X + scalar, a.Y + scalar, a.Z + scalar );
        }

        public static Vector3f operator -( Vector3f a, Vector3f b ) {
            return new Vector3f( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3f operator -( Vector3f a, float scalar ) {
            return new Vector3f( a.X - scalar, a.Y - scalar, a.Z - scalar );
        }

        public static Vector3f operator *( Vector3f a, float scalar ) {
            return new Vector3f( (a.X * scalar), (a.Y * scalar), (a.Z * scalar) );
        }

        public static Vector3f operator /( Vector3f a, double scalar ) {
            return new Vector3f( (float)(a.X / scalar), (float)(a.Y / scalar), (float)(a.Z / scalar) );
        }


        public static Vector3f operator +( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X + b.X, a.Z + b.Y, a.Y + b.Z );
        }

        public static Vector3f operator +( Vector3f a, Vector3i b ) {
            return new Vector3f( a.X + b.X, a.Y + b.Z, a.Z + b.Y );
        }

        public static Vector3f operator -( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X - b.X, a.Z - b.Y, a.Y - b.Z );
        }

        public static Vector3f operator -( Vector3f a, Vector3i b ) {
            return new Vector3f( a.X - b.X, a.Y - b.Z, a.Z - b.Y );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3f ) {
                return this.Equals( (Vector3f)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3f other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3f a, Vector3f b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3f a, Vector3f b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return (int)(X + Y * 1625 + Z * 2642245);
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3i other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }

        public int CompareTo( Vector3f other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }


        public static bool operator >( Vector3f a, Vector3f b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) > (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator <( Vector3f a, Vector3f b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) < (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator >=( Vector3f a, Vector3f b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) >= (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator <=( Vector3f a, Vector3f b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) <= (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        #endregion


        public override string ToString() {
            return String.Format( "Vector({0},{1},{2})", X, Y, Z );
        }
    }
}