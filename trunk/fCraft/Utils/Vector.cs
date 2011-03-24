// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    
    /// <summary>
    /// Integer 3D vector, used by Forester.
    /// </summary>
    public struct Vector3i : IEquatable<Vector3i>, IComparable<Vector3i>, IComparable<Vector3f> {
        public int X, Z, Y;

        public double GetLength() {
            return Math.Sqrt( X * X + Z * Z + Y * Y );
        }

        public static bool operator >( Vector3i a, Vector3i b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y > b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator <( Vector3i a, Vector3i b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y < b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator >=( Vector3i a, Vector3i b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y >= b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator <=( Vector3i a, Vector3i b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y <= b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator ==( Vector3i a, Vector3i b ) {
            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z);
        }
        public static bool operator !=( Vector3i a, Vector3i b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y != b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }

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

        public override int GetHashCode() {
            return X + Z * 1625 + Y * 2642245;
        }

        public override bool Equals( object obj ) {
            if( obj is Vector3i ) {
                return (this == (Vector3i)obj);
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3i other ) {
            return (this == other);
        }

        public int CompareTo( Vector3i other ) {
            return Math.Sign( GetLength() - other.GetLength() );
        }

        public int CompareTo( Vector3f other ) {
            return Math.Sign( GetLength() - other.GetLength() );
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

        public int GetLargestComponent() {
            int maxVal = Math.Max( Math.Abs( X ), Math.Max( Math.Abs( Y ), Math.Abs( Z ) ) );
            if( maxVal == Math.Abs( X ) ) return 0;
            if( maxVal == Math.Abs( Y ) ) return 1;
            return 2;
        }
    }


    /// <summary>
    /// Floating-point (float) 3D vector, used by Forester
    /// </summary>
    public struct Vector3f : IEquatable<Vector3f>, IComparable<Vector3i>, IComparable<Vector3f> {
        public float X, Y, Z;

        public float GetLength() {
            return (float)Math.Sqrt( X * X + Y * Y + Z * Z );
        }

        public static bool operator >( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y > b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator <( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y < b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator >=( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y >= b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator <=( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y <= b.X * b.X + b.Z * b.Z + b.Y * b.Y;
        }
        public static bool operator ==( Vector3f a, Vector3f b ) {
            return (a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z);
        }
        public static bool operator !=( Vector3f a, Vector3f b ) {
            return (a.X != b.X) || (a.Y != b.Y) || (a.Z != b.Z);
        }

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

        public Vector3f( float x, float y, float h ) {
            X = x;
            Y = y;
            Z = h;
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

        public override int GetHashCode() {
            return (int)(X + Y * 1625 + Z * 2642245);
        }

        public override bool Equals( object obj ) {
            if( obj is Vector3f ) {
                return (this == (Vector3f)obj);
            } else {
                return base.Equals( obj );
            }
        }

        public bool Equals( Vector3f other ) {
            return (this == other);
        }

        public int CompareTo( Vector3i other ) {
            return Math.Sign( GetLength() - other.GetLength() );
        }

        public int CompareTo( Vector3f other ) {
            return Math.Sign( GetLength() - other.GetLength() );
        }
    }
}
