// Originally part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// Based in part on Minecraft 0.30 bytecode, copyright 2009 Markus Persson / Mojang AB
using System;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    // Based on Minecraft Classic's "com.mojang.minecraft.level.a.a.a"
    internal sealed class FilteredNoise {
        readonly PerlinNoise noise1, noise2;

        public FilteredNoise( [NotNull] PerlinNoise noise1, [NotNull] PerlinNoise noise2 ) {
            if( noise1 == null ) throw new ArgumentNullException( "noise1" );
            if( noise2 == null ) throw new ArgumentNullException( "noise2" );
            this.noise1 = noise1;
            this.noise2 = noise2;
        }

        public double GetNoise( double x, double y ) {
            return noise1.GetNoise( x + noise2.GetNoise( x, y ), y );
        }
    }


    // Based on Minecraft Classic's "com.mojang.minecraft.level.a.a.b"
    internal sealed class PerlinNoise {
        readonly ImprovedNoise[] noiseLayers;
        readonly int octaves;

        public PerlinNoise( [NotNull] Random rand, int octaves ) {
            if( rand == null ) throw new ArgumentNullException( "rand" );
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


    /// <summary> Improved Perlin Noise implementation,
    /// based on reference Java implementation (Copyright 2002 Ken Perlin).
    /// Original: http://mrl.nyu.edu/~perlin/noise/ </summary>
    public sealed class ImprovedNoise {
        readonly int[] p = new int[512];

        public ImprovedNoise( [NotNull] Random random ) {
            if( random == null ) throw new ArgumentNullException( "random" );
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
            int intX = (int)Math.Floor( x ) & 255, // FIND UNIT CUBE THAT
                intY = (int)Math.Floor( y ) & 255, // CONTAINS POINT.
                intZ = (int)Math.Floor( z ) & 255;
            x -= Math.Floor( x ); // FIND RELATIVE X,Y,Z
            y -= Math.Floor( y ); // OF POINT IN CUBE.
            z -= Math.Floor( z );
            double u = Fade( x ), // COMPUTE FADE CURVES
                   v = Fade( y ), // FOR EACH OF X,Y,Z.
                   w = Fade( z );
            int a = p[intX] + intY,
                aa = p[a] + intZ,
                ab = p[a + 1] + intZ, // HASH COORDINATES OF
                b = p[intX + 1] + intY,
                ba = p[b] + intZ,
                bb = p[b + 1] + intZ; // THE 8 CUBE CORNERS,

            return Lerp( w,
                         Lerp( v,
                               Lerp( u,
                                     Grad( p[aa], x, y, z ), // AND ADD
                                     Grad( p[ba], x - 1, y, z ) ), // BLENDED
                               Lerp( u,
                                     Grad( p[ab], x, y - 1, z ), // RESULTS
                                     Grad( p[bb], x - 1, y - 1, z ) ) ), // FROM  8
                         Lerp( v,
                               Lerp( u,
                                     Grad( p[aa + 1], x, y, z - 1 ), // CORNERS
                                     Grad( p[ba + 1], x - 1, y, z - 1 ) ), // OF CUBE
                               Lerp( u,
                                     Grad( p[ab + 1], x, y - 1, z - 1 ),
                                     Grad( p[bb + 1], x - 1, y - 1, z - 1 ) ) ) );
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
    }
}