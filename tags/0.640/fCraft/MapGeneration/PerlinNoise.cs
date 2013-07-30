// Part of FemtoCraft | Copyright 2012-213 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Original Java code copyright 2009 Markus Persson / Mojang AB
using System;

namespace fCraft {
    // Based on Minecraft Classic's "com.mojang.minecraft.level.a.a.a"
    public sealed class FilteredNoise {
        readonly PerlinNoise noise1, noise2;

        public FilteredNoise( PerlinNoise noise1, PerlinNoise noise2 ) {
            this.noise1 = noise1;
            this.noise2 = noise2;
        }

        public double GetNoise( double x, double y ) {
            return noise1.GetNoise( x + noise2.GetNoise( x, y ), y );
        }
    }


    // Based on Minecraft Classic's "com.mojang.minecraft.level.a.a.b"
    public sealed class PerlinNoise {
        readonly ImprovedNoise[] noiseLayers;
        readonly int octaves;

        public PerlinNoise( Random rand, int octaves ) {
            this.octaves = octaves;
            noiseLayers = new ImprovedNoise[octaves];
            for( int i = 0; i < octaves; i++ ) {
                noiseLayers[i] = new ImprovedNoise( rand );
            }
        }

        public double GetNoise( double x, double y ) {
            double sum = 0;
            double scale = 1;
            for( int i = 0; i < octaves; i++ ) {
                sum += noiseLayers[i].Noise( x/scale, y/scale, 0 )*scale;
                scale *= 2;
            }
            return sum;
        }
    }


    // Based on: http://mrl.nyu.edu/~perlin/noise/
    // JAVA REFERENCE IMPLEMENTATION OF IMPROVED NOISE - COPYRIGHT 2002 KEN PERLIN.
    public sealed class ImprovedNoise {
        public ImprovedNoise( Random random ) {
            for( int i = 0; i < 256; i++ ) {
                p[i] = i;
            }
            for( int i = 0; i < 256; i++ ) {
                int i1 = random.Next( 0, 256 );
                int i2 = random.Next( 0, 256 );
                int temp = p[i1];
                p[i1] = p[i2];
                p[i2] = temp;
                p[i1 + 256] = p[i2];
                p[i2 + 256] = temp;
            }
        }


        public double Noise( double x, double y, double z ) {
            int X = (int)Math.Floor( x ) & 255, // FIND UNIT CUBE THAT
                Y = (int)Math.Floor( y ) & 255, // CONTAINS POINT.
                Z = (int)Math.Floor( z ) & 255;
            x -= Math.Floor( x ); // FIND RELATIVE X,Y,Z
            y -= Math.Floor( y ); // OF POINT IN CUBE.
            z -= Math.Floor( z );
            double u = Fade( x ), // COMPUTE FADE CURVES
                   v = Fade( y ), // FOR EACH OF X,Y,Z.
                   w = Fade( z );
            int A = p[X] + Y,
                AA = p[A] + Z,
                AB = p[A + 1] + Z, // HASH COORDINATES OF
                B = p[X + 1] + Y,
                BA = p[B] + Z,
                BB = p[B + 1] + Z; // THE 8 CUBE CORNERS,

            return Lerp( w,
                         Lerp( v,
                               Lerp( u,
                                     Grad( p[AA], x, y, z ), // AND ADD
                                     Grad( p[BA], x - 1, y, z ) ), // BLENDED
                               Lerp( u,
                                     Grad( p[AB], x, y - 1, z ), // RESULTS
                                     Grad( p[BB], x - 1, y - 1, z ) ) ), // FROM  8
                         Lerp( v,
                               Lerp( u,
                                     Grad( p[AA + 1], x, y, z - 1 ), // CORNERS
                                     Grad( p[BA + 1], x - 1, y, z - 1 ) ), // OF CUBE
                               Lerp( u,
                                     Grad( p[AB + 1], x, y - 1, z - 1 ),
                                     Grad( p[BB + 1], x - 1, y - 1, z - 1 ) ) ) );
        }


        static double Fade( double t ) {
            return t*t*t*(t*(t*6 - 15) + 10);
        }


        static double Lerp( double t, double a, double b ) {
            return a + t*(b - a);
        }


        static double Grad( int hash, double x, double y, double z ) {
            int h = hash & 15; // CONVERT LOW 4 BITS OF HASH CODE
            double u = h < 8 ? x : y, // INTO 12 GRADIENT DIRECTIONS.
                   v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        readonly int[] p = new int[512];
    }
}