using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    public class Noise {

        int c0, c1, c2;
        double c3;

        public Noise( Random rand ) {
            c0 = rand.Next( 10000, 100000 );
            c1 = rand.Next( 100000, 1000000 );
            c2 = rand.Next( 1000000, 10000000 );
            c3 = rand.Next( 10000000, 100000000 );
        }


        public static float InterpolateLinear( float v0, float v1, float x ) {
            return v0 * (1 - x) + v1 * x;
        }

        public static float InterpolateLinear( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateLinear( InterpolateLinear( v00, v10, x ),
                                      InterpolateLinear( v01, v11, x ),
                                      y );
        }


        public static float InterpolateCosine( float v0, float v1, float x ) {
            double f = (1 - Math.Cos( x * Math.PI )) * .5;
            return (float)(v0 * (1 - f) + v1 * f);
        }

        public static float InterpolateCosine( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateCosine( InterpolateCosine( v00, v10, x ),
                                      InterpolateCosine( v01, v11, x ),
                                      y );
        }


        public static float InterpolateCubic( float v0, float v1, float v2, float v3, float x ) {
            float P = (v3 - v2) - (v0 - v1);
            float Q = (v0 - v1) - P;
            float R = v2 - v0;
            return (((P * x) + Q) * x + R) * x + v1; // Px^3 + Qx^2 + Rx + v1
        }


        public float StaticNoise( int x, int y ) {
            int n = x + y * 2053;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n * (n * n * c0 + c1) + c2) & 0x7fffffff) / c3);
        }


        public float InterpolatedNoise( float x, float y ) {
            int xInt = (int)x;
            float xFloat = x - xInt;

            int yInt = (int)y;
            float yFloat = y - yInt;


            float[,] points = new float[4, 4];
            for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                    points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                }
            }

            float p0 = InterpolateCubic( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
            float p1 = InterpolateCubic( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
            float p2 = InterpolateCubic( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
            float p3 = InterpolateCubic( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
            return InterpolateCubic( p0, p1, p2, p3, yFloat );

            /* for bilinear/cosine
            float p00 = StaticNoise( xInt, yInt );
            float p01 = StaticNoise( xInt, yInt + 1 );
            float p10 = StaticNoise( xInt+1, yInt );
            float p11 = StaticNoise( xInt+1, yInt+1 );

            return InterpolateCosine( InterpolateCosine( p00, p10, xFloat ), InterpolateCosine( p01, p11, xFloat ), yFloat );
            //return InterpolateLinear( InterpolateLinear( p00, p10, xFloat ), InterpolateLinear( p01, p11, xFloat ), yFloat );*/
        }


        public float PerlinNoise( float x, float y, int startOctave, int endOctave, float decay ) {
            float total = 0;
            int frequency = 1;
            float amplitude = 1;
            for( int n = 0; n < startOctave; n++ ) {
                frequency *= 2;
                amplitude *= decay;
            }
            for( int n = startOctave; n < endOctave; n++ ) {
                total += InterpolatedNoise( x * frequency + frequency, y * frequency + frequency ) * amplitude;
                frequency *= 2;
                amplitude *= decay;
            }
            return total;
        }


        public void PerlinNoiseMap( float[,] heightmap, int startOctave, int endOctave, float decay ) {
            float maxDim = 1f / Math.Max( heightmap.GetLength( 0 ), heightmap.GetLength( 1 ) );
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    heightmap[x, y] += PerlinNoise( x * maxDim + 10, y * maxDim + 10, startOctave, endOctave, decay );
                }
            }
        }


        public static void Normalize( float[,] map ) {
            Normalize( map, 0, 1 );
        }

        public static void Normalize( float[,] map, float low, float high ) {
            float min = float.MaxValue, max = float.MinValue;
            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    min = Math.Min( min, map[x, y] );
                    max = Math.Max( max, map[x, y] );
                }
            }

            float multiplier = (high - low) / (max - min);
            float constant = -min * (high - low) / (max - min) + low;

            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map[x, y] = map[x, y] * multiplier + constant;
                }
            }
        }


        // assumes normalized input
        public static void Marble( float[,] map ) {
            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map[x, y] = Math.Abs( map[x, y] * 2 - 1 );
                }
            }
        }

        // assumes normalized input
        public static void Blend( float[,] map1, float[,] map2, float[,] blendMap ) {
            for( int x = map1.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map1.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map1[x, y] = map1[x, y] * blendMap[x, y] + map2[x, y] * (1 - blendMap[x, y]);
                }
            }
        }

        public static void Add( float[,] map1, float[,] map2 ) {
            for( int x = map1.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map1.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map1[x, y] += map2[x, y];
                }
            }
        }

        public static void ApplyBias( float[,] heightmap, float c00, float c01, float c10, float c11, float midpoint ) {
            float maxX = 2f / heightmap.GetLength( 0 );
            float maxY = 2f / heightmap.GetLength( 1 );
            int offsetX = heightmap.GetLength( 0 ) / 2;
            int offsetY = heightmap.GetLength( 1 ) / 2;

            for( int x = offsetX - 1; x >= 0; x-- ) {
                for( int y = offsetY - 1; y >= 0; y-- ) {
                    heightmap[x, y] += InterpolateCosine( c00, (c00 + c01) / 2, (c00 + c10) / 2, midpoint, x * maxX, y * maxY );
                    heightmap[x + offsetX, y] += InterpolateCosine( (c00 + c10) / 2, midpoint, c10, (c11 + c10) / 2, x * maxX, y * maxY );
                    heightmap[x, y + offsetY] += InterpolateCosine( (c00 + c01) / 2, c01, midpoint, (c01 + c11) / 2, x * maxX, y * maxY );
                    heightmap[x + offsetX, y + offsetY] += InterpolateCosine( midpoint, (c01 + c11) / 2, (c11 + c10) / 2, c11, x * maxX, y * maxY );
                }
            }
        }
    }
}