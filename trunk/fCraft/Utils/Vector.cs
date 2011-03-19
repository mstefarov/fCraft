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

        public Vector3i( int _x, int _y, int _h ) {
            X = _x;
            Z = _y;
            Y = _h;
        }
        public Vector3i( Vector3i other ) {
            X = other.X;
            Z = other.Z;
            Y = other.Y;
        }
        public Vector3i( Vector3f other ) {
            X = (int)other.x;
            Z = (int)other.y;
            Y = (int)other.h;
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
        public float x, y, h;

        public float GetLength() {
            return (float)Math.Sqrt( x * x + y * y + h * h );
        }

        public static bool operator >( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y > b.x * b.x + b.h * b.h + b.y * b.y;
        }
        public static bool operator <( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y < b.x * b.x + b.h * b.h + b.y * b.y;
        }
        public static bool operator >=( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y >= b.x * b.x + b.h * b.h + b.y * b.y;
        }
        public static bool operator <=( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y <= b.x * b.x + b.h * b.h + b.y * b.y;
        }
        public static bool operator ==( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y == b.x * b.x + b.h * b.h + b.y * b.y;
        }
        public static bool operator !=( Vector3f a, Vector3f b ) {
            return a.x * a.x + a.h * a.h + a.y * a.y != b.x * b.x + b.h * b.h + b.y * b.y;
        }

        public static Vector3f operator +( Vector3f a, Vector3f b ) {
            return new Vector3f( a.x + b.x, a.y + b.y, a.h + b.h );
        }
        public static Vector3f operator +( Vector3f a, float scalar ) {
            return new Vector3f( a.x + scalar, a.y + scalar, a.h + scalar );
        }
        public static Vector3f operator -( Vector3f a, Vector3f b ) {
            return new Vector3f( a.x - b.x, a.y - b.y, a.h - b.h );
        }
        public static Vector3f operator -( Vector3f a, float scalar ) {
            return new Vector3f( a.x - scalar, a.y - scalar, a.h - scalar );
        }
        public static Vector3f operator *( Vector3f a, float scalar ) {
            return new Vector3f( (a.x * scalar), (a.y * scalar), (a.h * scalar) );
        }
        public static Vector3f operator /( Vector3f a, double scalar ) {
            return new Vector3f( (float)(a.x / scalar), (float)(a.y / scalar), (float)(a.h / scalar) );
        }

        public static Vector3f operator +( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X + b.x, a.Z + b.y, a.Y + b.h );
        }
        public static Vector3f operator +( Vector3f a, Vector3i b ) {
            return new Vector3f( a.x + b.X, a.y + b.Z, a.h + b.Y );
        }
        public static Vector3f operator -( Vector3i a, Vector3f b ) {
            return new Vector3f( a.X - b.x, a.Z - b.y, a.Y - b.h );
        }
        public static Vector3f operator -( Vector3f a, Vector3i b ) {
            return new Vector3f( a.x - b.X, a.y - b.Z, a.h - b.Y );
        }

        public float this[int i] {
            get {
                switch( i ) {
                    case 0: return x;
                    case 1: return h;
                    default: return y;
                }
            }
            set {
                switch( i ) {
                    case 0: x = value; return;
                    case 1: h = value; return;
                    default: y = value; return;
                }
            }
        }

        public Vector3f( float _x, float _y, float _h ) {
            x = _x;
            y = _y;
            h = _h;
        }
        public Vector3f( Vector3f other ) {
            x = other.x;
            y = other.y;
            h = other.h;
        }
        public Vector3f( Vector3i other ) {
            x = other.X;
            y = other.Z;
            h = other.Y;
        }

        public override int GetHashCode() {
            return (int)(x + y * 1625 + h * 2642245);
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
