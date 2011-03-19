// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    
    /// <summary>
    /// Integer 3D vector, used by Forester.
    /// </summary>
    public struct Vector3i {
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
            return a.X * a.X + a.Z * a.Z + a.Y * a.Y == b.X * b.X + b.Z * b.Z + b.Y * b.Y;
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
                return this == (Vector3i)obj;
            } else {
                return base.Equals( obj );
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
            Y = (int)other.H;
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
    public struct Vector3f {
        public float X, Y, H;

        public float GetLength() {
            return (float)Math.Sqrt( X * X + Y * Y + H * H );
        }

        public static bool operator >( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y > b.X * b.X + b.H * b.H + b.Y * b.Y;
        }
        public static bool operator <( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y < b.X * b.X + b.H * b.H + b.Y * b.Y;
        }
        public static bool operator >=( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y >= b.X * b.X + b.H * b.H + b.Y * b.Y;
        }
        public static bool operator <=( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y <= b.X * b.X + b.H * b.H + b.Y * b.Y;
        }
        public static bool operator ==( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y == b.X * b.X + b.H * b.H + b.Y * b.Y;
        }
        public static bool operator !=( Vector3f a, Vector3f b ) {
            return a.X * a.X + a.H * a.H + a.Y * a.Y != b.X * b.X + b.H * b.H + b.Y * b.Y;
        }

        public static Vector3f operator +( Vector3f a, Vector3f b ) {
            return new Vector3f( a.X + b.X, a.Y + b.Y, a.H + b.H );
        }
        public static Vector3f operator +( Vector3f a, float scalar ) {
            return new Vector3f( a.X + scalar, a.Y + scalar, a.H + scalar );
        }
        public static Vector3f operator -( Vector3f a, Vector3f b ) {
            return new Vector3f( a.X - b.X, a.Y - b.Y, a.H - b.H );
        }
        public static Vector3f operator -( Vector3f a, float scalar ) {
            return new Vector3f( a.X - scalar, a.Y - scalar, a.H - scalar );
        }
        public static Vector3f operator *( Vector3f a, float scalar ) {
            return new Vector3f( (a.X * scalar), (a.Y * scalar), (a.H * scalar) );
        }
        public static Vector3f operator /( Vector3f a, double scalar ) {
            return new Vector3f( (float)(a.X / scalar), (float)(a.Y / scalar), (float)(a.H / scalar) );
        }

        public static Vector3f operator +( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X + b.X, a.Z + b.Y, a.Y + b.H );
        }
        public static Vector3f operator +( Vector3f a, Vector3i b ) {
            return new Vector3f( a.X + b.X, a.Y + b.Z, a.H + b.Y );
        }
        public static Vector3f operator -( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X - b.X, a.Z - b.Y, a.Y - b.H );
        }
        public static Vector3f operator -( Vector3f a, Vector3i b ) {
            return new Vector3f( a.X - b.X, a.Y - b.Z, a.H - b.Y );
        }

        public float this[int i] {
            get {
                switch( i ) {
                    case 0: return X;
                    case 1: return H;
                    default: return Y;
                }
            }
            set {
                switch( i ) {
                    case 0: X = value; return;
                    case 1: H = value; return;
                    default: Y = value; return;
                }
            }
        }

        public Vector3f( float x, float y, float h ) {
            X = x;
            Y = y;
            H = h;
        }
        public Vector3f( Vector3f other ) {
            X = other.X;
            Y = other.Y;
            H = other.H;
        }
        public Vector3f( Vector3i other ) {
            X = other.X;
            Y = other.Z;
            H = other.Y;
        }

        public override int GetHashCode() {
            return (int)(X + Y * 1625 + H * 2642245);
        }

        public override bool Equals( object obj ) {
            if( obj is Vector3f ) {
                return this == (Vector3f)obj;
            } else {
                return base.Equals( obj );
            }
        }
    }
}
