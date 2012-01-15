// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// With contributions by Conrad "Redshift" Morgan
using System;
using fCraft.Drawing;

namespace fCraft {
    /// <summary> Floating-point (single precision) 3D vector. </summary>
    public struct Vector3F : IEquatable<Vector3F>, IComparable<Vector3I>, IComparable<Vector3F> {

        /// <summary> Cartesian unit vector representing the Origin (0, 0, 0). </summary>
        public static readonly Vector3F Zero = new Vector3F( 0, 0, 0 );
        /// <summary> Cartesian unit vector represnting up. </summary>
        public static readonly Vector3F Up = new Vector3F( 0, 0, 1 );
        /// <summary> Cartesian unit vectore representing down. </summary>
        public static readonly Vector3F Down = new Vector3F( 0, 0, -1 );

        /// <summary> The X component of this vector. </summary>
        public float X;
        /// <summary> The Y component of this vector. </summary>
        public float Y;
        /// <summary> The Z component of this vector. </summary>
        public float Z;

        /// <summary> The X component of this vector squared. </summary>
        public float X2 { get { return X * X; } }
        /// <summary> The Y component of this vector squared. </summary>
        public float Y2 { get { return Y * Y; } }
        /// <summary> The Z component of this vector squared. </summary>
        public float Z2 { get { return Z * Z; } }

        /// <summary> Initialises a new instance of Vector3F using X/Y/Z. </summary>
        /// <param name="x"> X position. </param>
        /// <param name="y"> Y position. </param>
        /// <param name="z"> Z position. </param>
        public Vector3F( float x, float y, float z ) {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary> Initialises a new instance of Vector3F using a another Vector3F. </summary>
        /// <param name="other"> Other Vector3F</param>
        public Vector3F( Vector3F other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        /// <summary> Initialises a new instance of Vector3F using a another Vector3I. </summary>
        /// <param name="other"> Other Vector3I. </param>
        public Vector3F( Vector3I other ) {
            X = other.X;
            Y = other.Y;
            Z = other.Z;
        }

        /// <summary> Length of this vector (magnitude) from the origin. </summary>
        public float Length {
            get {
                return (float)Math.Sqrt( (double)X * X + (double)Y * Y + (double)Z * Z );
            }
        }
        
        /// <summary> Squared-length of this vector (non-squarerooted) from the origin. </summary>
        public float LengthSquared {
            get {
                return X * X + Y * Y + Z * Z;
            }
        }


        public float this[int i] {
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


        public float this[Axis i] {
            get {
                switch( i ) {
                    case Axis.X: return X;
                    case Axis.Y: return Y;
                    default: return Z;
                }
            }
            set {
                switch( i ) {
                    case Axis.X: X = value; return;
                    case Axis.Y: Y = value; return;
                    default: Z = value; return;
                }
            }
        }


        #region Operators

        public static Vector3F operator +( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator +( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }

        public static Vector3F operator +( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X + b.X, a.Y + b.Y, a.Z + b.Z );
        }


        public static Vector3F operator -( Vector3F a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3F operator -( Vector3I a, Vector3F b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }

        public static Vector3F operator -( Vector3F a, Vector3I b ) {
            return new Vector3F( a.X - b.X, a.Y - b.Y, a.Z - b.Z );
        }


        public static Vector3F operator *( Vector3F a, float scalar ) {
            return new Vector3F( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }

        public static Vector3F operator *( float scalar, Vector3F a ) {
            return new Vector3F( a.X * scalar, a.Y * scalar, a.Z * scalar );
        }

        public static Vector3F operator /( Vector3F a, float scalar ) {
            return new Vector3F( a.X / scalar, a.Y / scalar, a.Z / scalar );
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
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return ( X == other.X ) && ( Y == other.Y ) && ( Z == other.Z );
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }


        public static bool operator ==( Vector3F a, Vector3F b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Vector3F a, Vector3F b ) {
            return !a.Equals( b );
        }


        /// <summary> Returns the hash code for this instance. </summary>
        /// <returns> A 32-bit signed integer that is the hash code for this instance. </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode() {
            return (int)( X + Y * 1625 + Z * 2642245 );
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
            return a.LengthSquared > b.LengthSquared;
        }

        public static bool operator <( Vector3F a, Vector3F b ) {
            return a.LengthSquared < b.LengthSquared;
        }

        public static bool operator >=( Vector3F a, Vector3F b ) {
            return a.LengthSquared >= b.LengthSquared;
        }

        public static bool operator <=( Vector3F a, Vector3F b ) {
            return a.LengthSquared <= b.LengthSquared;
        }

        #endregion

        /// <summary> Calculates the dot product (scalar product) of this vector and the specified vector. </summary>
        /// <param name="b"> Other Vector3I. </param>
        /// <returns> Dot product of this and the specified vector. </returns>
        public float Dot( Vector3I b ) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }

        // <summary> Calculates the dot product (scalar product) of this vector and the specified vector. </summary>
        /// <param name="b"> Other Vector3F. </param>
        /// <returns> Dot product of this and the specified vector. </returns>
        public float Dot( Vector3F b ) {
            return ( X * b.X ) + ( Y * b.Y ) + ( Z * b.Z );
        }

        /// <summary> Calculates the cross product (cofactor expansion) of this vector and the specified vector. </summary>
        /// <param name="b"> Other Vector3I. </param>
        /// <returns> Cross product of this and the specified vector. </returns>
        public Vector3F Cross( Vector3I b ) {
            return new Vector3F( ( Y * b.Z ) - ( Z * b.Y ),
                                 ( Z * b.X ) - ( X * b.Z ),
                                 ( X * b.Y ) - ( Y * b.X ) );
        }

        /// <summary> Calculates the cross product (cofactor expansion) of this vector and the specified vector. </summary>
        /// <param name="b"> Other Vector3F. </param>
        /// <returns> Cross product of this and the specified vector. </returns>
        public Vector3F Cross( Vector3F b ) {
            return new Vector3F( ( Y * b.Z ) - ( Z * b.Y ),
                                 ( Z * b.X ) - ( X * b.Z ),
                                 ( X * b.Y ) - ( Y * b.X ) );
        }

        /// <summary> The the major axis (longest). </summary>
        public Axis LongestComponent {
            get {
                float maxVal = Math.Max( Math.Abs( X ), Math.Max( Math.Abs( Y ), Math.Abs( Z ) ) );
                if( maxVal == Math.Abs( X ) ) return Axis.X;
                if( maxVal == Math.Abs( Y ) ) return Axis.Y;
                return Axis.Z;
            }
        }

        /// <summary> The minor axis (shortest). </summary>
        public Axis ShortestComponent {
            get {
                float minVal = Math.Min( Math.Abs( X ), Math.Min( Math.Abs( Y ), Math.Abs( Z ) ) );
                if( minVal == Math.Abs( X ) ) return Axis.X;
                if( minVal == Math.Abs( Y ) ) return Axis.Y;
                return Axis.Z;
            }
        }

        /// <summary> This vector but with the absolute values of the coordinates. </summary>
        /// <returns> This vector with all positive coordinates. </returns>
        public Vector3F Abs() {
            return new Vector3F( Math.Abs( X ), Math.Abs( Y ), Math.Abs( Z ) );
        }

        /// <summary> Calculates the unit vector of this vector. </summary>
        /// <returns> The unit vector of this vector. </returns>
        public Vector3F Normalize() {
            if( X == 0 && Y == 0 && Z == 0 ) return Zero;
            double len = Math.Sqrt( (double)X * X + (double)Y * Y + (double)Z * Z );
            return new Vector3F( (float)(X / len), (float)(Y / len), (float)(Z / len) );
        }

        public override string ToString() {
            return String.Format( "({0},{1},{2})", X, Y, Z );
        }


        #region Conversion

        public static explicit operator Vector3I( Vector3F a ) {
            return new Vector3I( (int)a.X, (int)a.Y, (int)a.Z );
        }

        /// <summary> This vector but with the coordinates rounded to the nearest integer. </summary>
        /// <returns> This vector with all the coordinates rounded. </returns>
        public Vector3I Round() {
            return new Vector3I( (int)Math.Round( X ), (int)Math.Round( Y ), (int)Math.Round( Z ) );
        }

        /// <summary> This vector but with the coordinates floored (rounded down to the nearest integer). </summary>
        /// <returns> This vector with all the coordinates floored. </returns>
        public Vector3I RoundDown() {
            return new Vector3I( (int)Math.Floor( X ), (int)Math.Floor( Y ), (int)Math.Floor( Z ) );
        }

        /// <summary> This vector but with the coordinates ceiled (rounded up to the nearest integer). </summary>
        /// <returns> This vector with all the coordinates ceiled. </returns>
        public Vector3I RoundUp() {
            return new Vector3I( (int)Math.Ceiling( X ), (int)Math.Ceiling( Y ), (int)Math.Ceiling( Z ) );
        }

        /// <summary> Converts the vector into player position coordinates. </summary>
        /// <returns> Player position coordinates. </returns>
        public Position ToPlayerCoords() {
            return new Position( (int)(X * 32), (int)(Y * 32), (int)(Z * 32) );
        }

        #endregion
    }
}