// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {

    /// <summary>
    /// Struct representing a position (with orientation) in the world. Takes up 8 bytes of memory.
    /// Note that, as a struct, Position objects are COPIED when assigned or passed as an argument.
    /// </summary>
    public struct Position : IEquatable<Position> {
        public readonly static Position Zero = new Position( 0, 0, 0 );

        public short X, Y, H;
        public byte R, L;

        public Position( int x, int y, int h ) {
            X = (short)x;
            Y = (short)y;
            H = (short)h;
            R = 0;
            L = 0;
        }

        public bool FitsIntoByte() {
            return X >= SByte.MinValue && X <= SByte.MaxValue &&
                   Y >= SByte.MinValue && Y <= SByte.MaxValue &&
                   H >= SByte.MinValue && H <= SByte.MaxValue;
        }

        public bool IsZero() {
            return X == 0 && Y == 0 && H == 0 && R == 0 && L == 0;
        }

        // adjust for bugs in position-reporting in Minecraft client
        public Position GetFixed() {
            return new Position {
                X = (X),
                Y = (Y),
                H = (short)(H - 22),
                R = R,
                L = L
            };
        }

        public int DistanceSquaredTo( Position other ) {
            return (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y) + (H - other.H) * (H - other.H);
        }

        public static bool operator ==( Position a, Position b ) {
            return a.Equals( b );
        }

        public static bool operator !=( Position a, Position b ) {
            return !a.Equals( b );
        }

        public bool Equals( Position other ) {
            return (X == other.X) && (Y == other.Y) && (H == other.H) && (R == other.R) && (L == other.R);
        }

        public override bool Equals( object obj ) {
            if( obj is Position ) {
                return Equals( (Position)obj );
            } else {
                return base.Equals( obj );
            }
        }

        public override int GetHashCode() {
            return (X + Y * short.MaxValue) ^ (R + L * short.MaxValue) + H;
        }

        public override string ToString() {
            return String.Format( "Position({0},{1},{2},{3},{4})", X, Y, H, R, L );
        }
    }
}
