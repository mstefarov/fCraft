// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary> Struct representing a position AND orientation. Takes up 8 bytes of memory.
    /// Use Vector3I if you just need X/Y/Z coordinates without orientation.
    /// Note that, as a struct, Positions are COPIED when assigned or passed as an argument. </summary>
    public struct Position : IEquatable<Position> {

        /// <summary> The Zero marker for all positions. </summary>
        public readonly static Position Zero = new Position( 0, 0, 0 );

        /// <summary> (Notch's X). Corresponds to "Width". </summary>
        public short X;
        /// <summary> (Notch's Z). Corresponds to "Length". </summary>
        public short Y;
        /// <summary>  (Notch's Y). Corresponds to "Height". </summary>
        public short Z;

        /// <summary> Rotation, 0 = 0 degrees,
        ///  127 = 180 degrees, and 
        /// -128 = -180 degrees. </summary>
        public byte R;

        /// <summary>
        /// Look, 0 = Horizon (as defined by the player's height),
        ///  127 = 180 degrees above horizon, and
        ///  -128 = 180 degrees below the horizon.
        /// </summary>
        public byte L;

        /// <summary> Creates a new intance of position using X/Y/Z and Rotation angle , and Look angle. </summary>
        /// <param name="x"> (Notch's X). Corresponds to "Width". </param>
        /// <param name="y"> (Notch's Z). Corresponds to "Length". </param>
        /// <param name="z"> (Notch's Y). Corresponds to "Height". </param>
        /// <param name="r"> Rotation angle about origin. </param>
        /// <param name="l"> Look angle about horizon. </param>
        public Position( short x, short y, short z, byte r, byte l ) {
            X = x;
            Y = y;
            Z = z;
            R = r;
            L = l;
        }

        /// <summary> Creates a new intance of position using X/Y/Z. </summary>
        /// <param name="x"> (Notch's X). Corresponds to "Width". </param>
        /// <param name="y"> (Notch's Z). Corresponds to "Length". </param>
        /// <param name="z"> (Notch's Y). Corresponds to "Height". </param>
        public Position( int x, int y, int z ) {
            X = (short)x;
            Y = (short)y;
            Z = (short)z;
            R = 0;
            L = 0;
        }

        internal bool FitsIntoMoveRotatePacket {
            get {
                return X >= SByte.MinValue && X <= SByte.MaxValue &&
                       Y >= SByte.MinValue && Y <= SByte.MaxValue &&
                       Z >= SByte.MinValue && Z <= SByte.MaxValue;
            }
        }


        /// <summary> Whether all components of this Position are zero. </summary>
        public bool IsZero {
            get {
                return X == 0 && Y == 0 && Z == 0 && R == 0 && L == 0;
            }
        }


        /// <summary> Adjust for bugs in position-reporting in Minecraft client by offsetting Z by -22 units. </summary>
        public Position GetFixed() {
            return new Position {
                X = X,
                Y = Y,
                Z = (short)(Z - 22),
                R = R,
                L = L
            };
        }

        /// <summary> Gets the distance to another position in squared format (i.e. no squareroot). </summary>
        /// <param name="other"> Position to compare to. </param>
        /// <returns> Squared distance between positions. </returns>
        public int DistanceSquaredTo( Position other ) {
            return (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (Z - other.Z) * (Z - other.Z);
        }


        #region Equality

        public static bool operator ==( Position a, Position b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Position a, Position b ) {
            return !a.Equals( b );
        }

        public bool Equals( Position other ) {
            return (X == other.X) && (Y == other.Y) && (Z == other.Z) && (R == other.R) && (L == other.L);
        }

        public override bool Equals( object obj ) {
            return obj is Position && Equals( (Position)obj );
        }

        public override int GetHashCode() {
            return (X + Y * short.MaxValue) ^ (R + L * short.MaxValue) + Z;
        }

        #endregion


        public override string ToString() {
            return String.Format( "Position({0},{1},{2} @{3},{4})", X, Y, Z, R, L );
        }

        public static explicit operator Vector3I( Position a ) {
            return new Vector3I( a.X, a.Y, a.Z );
        }

        /// <summary> Gets this position to a Vector3I. </summary>
        /// <returns> X/Y/Z of this position as a Vector3I. </returns>
        public Vector3I ToVector3I() {
            return new Vector3I( X, Y, Z );
        }

        /// <summary> Gets this position as block coordinates. </summary>
        /// <returns> X/Y/Z of this position in block coordinates. </returns>
        public Vector3I ToBlockCoords() {
            return new Vector3I( (X - 16) / 32, (Y - 16) / 32, (Z - 16) / 32 );
        }
    }
}