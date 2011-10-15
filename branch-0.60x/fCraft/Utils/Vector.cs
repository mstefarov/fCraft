// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary> Integer 3D vector, used by Forester. </summary>
    public struct Vector3I : IEquatable<Vector3I>, IComparable<Vector3I>, IComparable<Vector3F> {
        public static readonly Vector3I Zero = new Vector3I( 0, 0, 0 );
        public static readonly Vector3I Down = new Vector3I( 0, 0, -1 );

        public int X, Y, Z;

        public Vector3I( int x, int y, int z ) {
            X = x;
            Z = z;
            Y = y;
        }

        public Vector3I( Vector3I other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3I( Vector3F other ) {
            X = (int)other.X;
            Y = (int)other.Z;
            Z = (int)other.Y;
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

        public static Vector3I operator +( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3I operator +( Vector3I a, int scalar ) {
            return new Vector3I( a.X + scalar, a.Y + scalar, a.Z + scalar );
        }

        public static Vector3I operator -( Vector3I a, Vector3I b ) {
            return new Vector3I( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3I operator -( Vector3I a, int scalar ) {
            return new Vector3I( a.X - scalar, a.Y - scalar, a.Z - scalar );
        }

        public static Vector3I operator *( Vector3I a, double scalar ) {
            return new Vector3I( (int)(a.X * scalar), (int)(a.Y * scalar), (int)(a.Z * scalar) );
        }

        public static Vector3I operator /( Vector3I a, double scalar ) {
            return new Vector3I( (int)(a.X / scalar), (int)(a.Y / scalar), (int)(a.Z / scalar) );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3I ) {
                return Equals( (Vector3I)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3I other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3I a, Vector3I b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3I a, Vector3I b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return X + Z * 1625 + Y * 2642245;
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3I other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }

        public int CompareTo( Vector3F other ) {
            return Math.Sign( LengthSquared - other.LengthSquared );
        }


        public static bool operator >( Vector3I a, Vector3I b ) {
            return a.LengthSquared > b.LengthSquared;
        }

        public static bool operator <( Vector3I a, Vector3I b ) {
            return a.LengthSquared < b.LengthSquared;
        }

        public static bool operator >=( Vector3I a, Vector3I b ) {
            return a.LengthSquared >= b.LengthSquared;
        }

        public static bool operator <=( Vector3I a, Vector3I b ) {
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
            return String.Format( "({0},{1},{2})", X, Y, Z );
        }

        public Position ToPosition() {
            return new Position( X, Y, Z );
        }

        public Vector3I Abs() {
            return new Vector3I( Math.Abs( X ), Math.Abs( Y ), Math.Abs( Z ) );
        }
    }


    /// <summary>
    /// Floating-point (float) 3D vector, used by Forester
    /// </summary>
    public struct Vector3F : IEquatable<Vector3F>, IComparable<Vector3I>, IComparable<Vector3F> {
        public static readonly Vector3F Down = new Vector3F( 0, 0, -1 );
        public static readonly Vector3F Zero = new Vector3F( 0, 0, 0 );

        public float X, Y, Z;
        public float X2 { get { return X * X; } }
        public float Y2 { get { return Y * Y; } }
        public float Z2 { get { return Z * Z; } }


        public Vector3F( float x, float y, float z ) {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3F( Vector3F other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        public Vector3F( Vector3I other ) {
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

        public static Vector3F operator +( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator +( Vector3F a, float scalar ) {
            return new Vector3F( a.X + scalar, a.Y + scalar, a.Z + scalar );
        }

        public static Vector3F operator -( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3F operator -( Vector3F a, float scalar ) {
            return new Vector3F( a.X - scalar, a.Y - scalar, a.Z - scalar );
        }

        public static Vector3F operator *( Vector3F a, float scalar ) {
            return new Vector3F( (a.X * scalar), (a.Y * scalar), (a.Z * scalar) );
        }

        public static Vector3F operator /( Vector3F a, double scalar ) {
            return new Vector3F( (float)(a.X / scalar), (float)(a.Y / scalar), (float)(a.Z / scalar) );
        }


        public static Vector3F operator +( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Z + b.Y, a.Y + b.Z );
        }

        public static Vector3F operator +( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Z, a.Z + b.Y );
        }

        public static Vector3F operator -( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Z - b.Y, a.Y - b.Z );
        }

        public static Vector3F operator -( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Z, a.Z - b.Y );
        }

        #endregion


        #region Equality

        public override bool Equals( object obj ) {
            if( obj is Vector3F ) {
                return Equals( (Vector3F)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3F other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z);
        }


        public static bool operator ==( Vector3F a, Vector3F b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3F a, Vector3F b ) {
            return !a.Equals( b );
        }


        public override int GetHashCode() {
            return (int)(X + Y * 1625 + Z * 2642245);
        }

        #endregion


        #region Comparison

        public int CompareTo( Vector3I other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }

        public int CompareTo( Vector3F other ) {
            return Math.Sign( LengthSquared - LengthSquared );
        }


        public static bool operator >( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) > (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator <( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) < (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator >=( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) >= (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        public static bool operator <=( Vector3F a, Vector3F b ) {
            return (a.X * a.X + a.Z * a.Z + a.Y * a.Y) <= (b.X * b.X + b.Z * b.Z + b.Y * b.Y);
        }

        #endregion


        public override string ToString() {
            return String.Format( "({0},{1},{2})", X, Y, Z );
        }
    }
}