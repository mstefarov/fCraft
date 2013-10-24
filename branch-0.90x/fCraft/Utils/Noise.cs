// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Interpolation mode for perlin noise. </summary>
    public enum NoiseInterpolationMode {
        /// <summary> Bilinear (LERP) interpolation (fastest). </summary>
        Linear,

        /// <summary> Cosine interpolation (fast). </summary>
        Cosine,

        /// <summary> Bicubic interpolation (slow). </summary>
        Bicubic,

        /// <summary> Spline interpolation (slowest). </summary>
        Spline
    }


    /// <summary> Class for generating and filtering 2D and 3D noise, extensively used by RealisticMapGenState and Cloudy brush. </summary>
    public sealed class Noise {
        /// <summary> Number used to seed the PRNGs. Set in constructor. </summary>
        public int Seed { get; private set; }

        /// <summary> Interpolation mode used to create smooth noise. Set in constructor. </summary>
        public NoiseInterpolationMode InterpolationMode { get; private set; }

        /// <summary> Creates a new Noise class using the given seed and interpolation method. </summary>
        public Noise( int seed, NoiseInterpolationMode interpolationMode ) {
            Seed = seed;
            InterpolationMode = interpolationMode;
        }


        /// <summary> 1D linear interpolation (LERP) </summary>
        public static float InterpolateLinear( float v0, float v1, float x ) {
            return v0*(1 - x) + v1*x;
        }


        /// <summary> 2D linear interpolation (LERP) </summary>
        public static float InterpolateLinear( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateLinear( InterpolateLinear( v00, v10, x ),
                                      InterpolateLinear( v01, v11, x ),
                                      y );
        }


        /// <summary> 1D cosine interpolation </summary>
        public static float InterpolateCosine( float v0, float v1, float x ) {
            double f = (1 - Math.Cos( x*Math.PI ))*.5;
            return (float)(v0*(1 - f) + v1*f);
        }


        /// <summary> 2D cosine interpolation </summary>
        public static float InterpolateCosine( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateCosine( InterpolateCosine( v00, v10, x ),
                                      InterpolateCosine( v01, v11, x ),
                                      y );
        }


        /// <summary> 1D Cubic Spline interpolation method, based on work by Paul Bourke.
        /// Interpolates on the curve formed by values v0 through v3, between point v1 and v2. </summary>
        public static float InterpolateCubic( float v0, float v1, float v2, float v3, float mu ) {
            float mu2 = mu*mu;
            float a0 = v3 - v2 - v0 + v1;
            float a1 = v0 - v1 - a0;
            float a2 = v2 - v0;
            float a3 = v1;
            return (a0*mu*mu2 + a1*mu2 + a2*mu + a3);
        }


        /// <summary> 2D Catmull-Rom Spline interpolation method, based on work by Paul Bourkee.
        /// Interpolates on the curve formed by values v0 through v3, between point v1 and v2. </summary>
        public static float InterpolateSpline( float v0, float v1, float v2, float v3, float mu ) {
            float mu2 = mu*mu;
            float a0 = -0.5f*v0 + 1.5f*v1 - 1.5f*v2 + 0.5f*v3;
            float a1 = v0 - 2.5f*v1 + 2*v2 - 0.5f*v3;
            float a2 = -0.5f*v0 + 0.5f*v2;
            float a3 = v1;
            return (a0*mu*mu2 + a1*mu2 + a2*mu + a3);
        }


        /// <summary> Gets random value at given 2D coordinate. Coordinates can be in any range.
        /// Result is normalized to 0.0-1.0. Guaranteed to produce same result for same coordinates between calls. </summary>
        public float StaticNoise( int x, int y ) {
            int n = Seed + x + y*short.MaxValue;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n*(n*n*15731 + 789221) + 1376312589) & 0x7FFFFFFF)/1073741824d);
        }


        /// <summary> Gets random value at given 3D coordinate. Coordinates can be in any range.
        /// Result is normalized to 0.0-1.0. Guaranteed to produce same result for same coordinates between calls. </summary>
        public float StaticNoise( int x, int y, int z ) {
            int n = Seed + x + y*1625 + z*2642245;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n*(n*n*15731 + 789221) + 1376312589) & 0x7FFFFFFF)/1073741824d);
        }


        readonly float[,] points = new float[4, 4];

        /// <summary> Gets noise for given 2D floating-point coordinate, using chosen InterpolationMode. 
        /// Coordinates must be castable to integer (i.e. between Int32.MinValue and Int64.MaxValue).
        /// Result is normalized to 0.0-1.0. Guaranteed to produce same result for same coordinates between calls. </summary>
        public float InterpolatedNoise( float x, float y ) {
            int xInt = (int)Math.Floor( x );
            float xFloat = x - xInt;

            int yInt = (int)Math.Floor( y );
            float yFloat = y - yInt;

            float p00, p01, p10, p11;

            switch( InterpolationMode ) {
                case NoiseInterpolationMode.Linear:
                    p00 = StaticNoise( xInt, yInt );
                    p01 = StaticNoise( xInt, yInt + 1 );
                    p10 = StaticNoise( xInt + 1, yInt );
                    p11 = StaticNoise( xInt + 1, yInt + 1 );
                    return InterpolateLinear( InterpolateLinear( p00, p10, xFloat ),
                                              InterpolateLinear( p01, p11, xFloat ),
                                              yFloat );

                case NoiseInterpolationMode.Cosine:
                    p00 = StaticNoise( xInt, yInt );
                    p01 = StaticNoise( xInt, yInt + 1 );
                    p10 = StaticNoise( xInt + 1, yInt );
                    p11 = StaticNoise( xInt + 1, yInt + 1 );
                    return InterpolateCosine( InterpolateCosine( p00, p10, xFloat ),
                                              InterpolateCosine( p01, p11, xFloat ),
                                              yFloat );

                case NoiseInterpolationMode.Bicubic:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateCubic( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateCubic( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateCubic( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateCubic( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateCubic( p00, p01, p10, p11, yFloat );

                case NoiseInterpolationMode.Spline:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateSpline( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateSpline( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateSpline( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateSpline( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateSpline( p00, p01, p10, p11, yFloat );

                default:
                    throw new ArgumentException();
            }
        }

        //readonly float[, ,] points3D = new float[4, 4, 4];
        /// <summary> Gets noise for given 2D floating-point coordinate, using chosen InterpolationMode.
        /// Only Linear and Cosine interpolation is currently supported.
        /// Coordinates must be castable to integer (i.e. between Int32.MinValue and Int64.MaxValue).
        /// Result is normalized to 0.0-1.0. Guaranteed to produce same result for same coordinates between calls. </summary>
        public float InterpolatedNoise( float x, float y, float z ) {
            int xInt = (int)Math.Floor( x );
            float xFloat = x - xInt;

            int yInt = (int)Math.Floor( y );
            float yFloat = y - yInt;

            int zInt = (int)Math.Floor( z );
            float zFloat = z - zInt;

            float p000,
                  p001,
                  p010,
                  p011,
                  p100,
                  p101,
                  p110,
                  p111;

            switch( InterpolationMode ) {
                case NoiseInterpolationMode.Linear:
                    p000 = StaticNoise( xInt, yInt, zInt );
                    p001 = StaticNoise( xInt, yInt, zInt + 1 );
                    p010 = StaticNoise( xInt, yInt + 1, zInt );
                    p011 = StaticNoise( xInt, yInt + 1, zInt + 1 );
                    p100 = StaticNoise( xInt + 1, yInt, zInt );
                    p101 = StaticNoise( xInt + 1, yInt, zInt + 1 );
                    p110 = StaticNoise( xInt + 1, yInt + 1, zInt );
                    p111 = StaticNoise( xInt + 1, yInt + 1, zInt + 1 );
                    return InterpolateLinear(
                        InterpolateLinear( InterpolateLinear( p000, p100, xFloat ),
                                           InterpolateLinear( p010, p110, xFloat ),
                                           yFloat ),
                        InterpolateLinear( InterpolateLinear( p001, p101, xFloat ),
                                           InterpolateLinear( p011, p111, xFloat ),
                                           yFloat ),
                        zFloat );

                case NoiseInterpolationMode.Cosine:
                    p000 = StaticNoise( xInt, yInt, zInt );
                    p001 = StaticNoise( xInt, yInt, zInt + 1 );
                    p010 = StaticNoise( xInt, yInt + 1, zInt );
                    p011 = StaticNoise( xInt, yInt + 1, zInt + 1 );
                    p100 = StaticNoise( xInt + 1, yInt, zInt );
                    p101 = StaticNoise( xInt + 1, yInt, zInt + 1 );
                    p110 = StaticNoise( xInt + 1, yInt + 1, zInt );
                    p111 = StaticNoise( xInt + 1, yInt + 1, zInt + 1 );
                    return InterpolateCosine(
                        InterpolateCosine( InterpolateCosine( p000, p100, xFloat ),
                                           InterpolateCosine( p010, p110, xFloat ),
                                           yFloat ),
                        InterpolateCosine( InterpolateCosine( p001, p101, xFloat ),
                                           InterpolateCosine( p011, p111, xFloat ),
                                           yFloat ),
                        zFloat );

                    /*
                case NoiseInterpolationMode.Bicubic: TODO
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateCubic( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateCubic( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateCubic( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateCubic( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateCubic( p00, p01, p10, p11, yFloat );

                case NoiseInterpolationMode.Spline:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateSpline( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateSpline( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateSpline( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateSpline( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateSpline( p00, p01, p10, p11, yFloat );
                    */
                case NoiseInterpolationMode.Bicubic:
                case NoiseInterpolationMode.Spline:
                    throw new NotSupportedException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public static float PerlinNoiseMax( int startOctave, int endOctave, float decay ) {
            if( startOctave < 0 ) throw new ArgumentOutOfRangeException( "startOctave" );
            if( startOctave > endOctave ) throw new ArgumentOutOfRangeException( "endOctave" );
            return (float)(Math.Pow( decay, startOctave ) - Math.Pow( decay, endOctave + 1 ))/(1 - decay);
        }


        public float PerlinNoise( float x, float y, int startOctave, int endOctave, float decay ) {
            if( startOctave < 0 ) throw new ArgumentOutOfRangeException( "startOctave" );
            if( startOctave > endOctave ) throw new ArgumentOutOfRangeException( "endOctave" );
            float total = 0;

            float frequency = (float)Math.Pow( 2, startOctave );
            float amplitude = (float)Math.Pow( decay, startOctave );

            for( int n = startOctave; n <= endOctave; n++ ) {
                total += InterpolatedNoise( x*frequency + frequency, y*frequency + frequency )*amplitude;
                frequency *= 2;
                amplitude *= decay;
            }

            return total;
        }


        public float PerlinNoise( float x, float y, float z, int startOctave, int endOctave, float decay ) {
            if( startOctave < 0 ) throw new ArgumentOutOfRangeException( "startOctave" );
            if( startOctave > endOctave ) throw new ArgumentOutOfRangeException( "endOctave" );
            float total = 0;

            float frequency = (float)Math.Pow( 2, startOctave );
            float amplitude = (float)Math.Pow( decay, startOctave );

            for( int n = startOctave; n <= endOctave; n++ ) {
                total += InterpolatedNoise( x*frequency + frequency, y*frequency + frequency, z*frequency + frequency )*
                         amplitude;
                frequency *= 2;
                amplitude *= decay;
            }
            return total;
        }


        public void PerlinNoise( [NotNull] float[,] map, int startOctave, int endOctave, float decay, int offsetX,
                                 int offsetY ) {
            if( startOctave < 0 ) throw new ArgumentOutOfRangeException( "startOctave" );
            if( startOctave > endOctave ) throw new ArgumentOutOfRangeException( "endOctave" );
            if( map == null ) throw new ArgumentNullException( "map" );
            float maxDim = 1f/Math.Max( map.GetLength( 0 ), map.GetLength( 1 ) );
            float divisor = PerlinNoiseMax( startOctave, endOctave, decay );

            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map[x, y] = PerlinNoise( x*maxDim + offsetX, y*maxDim + offsetY, startOctave, endOctave, decay )/
                                divisor;
                }
            }
        }


        public void PerlinNoise( [NotNull] float[,,] map, int startOctave, int endOctave, float decay, int offsetX,
                                 int offsetY, int offsetZ ) {
            if( startOctave < 0 ) throw new ArgumentOutOfRangeException( "startOctave" );
            if( startOctave > endOctave ) throw new ArgumentOutOfRangeException( "endOctave" );
            if( map == null ) throw new ArgumentNullException( "map" );
            float maxDim = 1f/Math.Max( map.GetLength( 0 ), Math.Max( map.GetLength( 2 ), map.GetLength( 1 ) ) );
            float divisor = PerlinNoiseMax( startOctave, endOctave, decay );
            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = map.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        map[x, y, z] = PerlinNoise( x*maxDim + offsetX,
                                                    y*maxDim + offsetY,
                                                    z*maxDim + offsetZ,
                                                    startOctave,
                                                    endOctave,
                                                    decay )/divisor;
                    }
                }
            }
        }

        #region Normalization

        /// <summary> Adjusts all values in the given 2D array so that all values
        /// are between 0 and 1. Adjustment is done in-place. </summary>
        /// <param name="data"> Raw data to normalize. </param>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static void Normalize( [NotNull] float[,] data ) {
            Normalize( data, 0, 1 );
        }

        /// <summary> Adjusts all values in the given 3D array so that all values
        /// are between 0 and 1. Adjustment is done in-place. </summary>
        /// <param name="data"> Raw data to normalize. </param>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static void Normalize( [NotNull] float[,,] data ) {
            Normalize( data, 0, 1 );
        }

        /// <summary> Adjusts all values in the given 2D array so that lowest value in the array
        /// matches <paramref name="low"/> and the highest value matches <paramref name="high"/>.
        /// Adjustment is done in-place. </summary>
        /// <param name="data"> Raw data to normalize. </param>
        /// <param name="low"> Lowest desired value. </param>
        /// <param name="high"> Highest desired value. </param>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe void Normalize( [NotNull] float[,] data, float low, float high ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                NormalizeImpl( ptr, data.Length, low, high );
            }
        }

        /// <summary> Adjusts all values in the given 3D array so that lowest value in the array
        /// matches <paramref name="low"/> and the highest value matches <paramref name="high"/>.
        /// Adjustment is done in-place. </summary>
        /// <param name="data"> Raw data to normalize. </param>
        /// <param name="low"> Lowest desired value. </param>
        /// <param name="high"> Highest desired value. </param>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe void Normalize( [NotNull] float[,,] data, float low, float high ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                NormalizeImpl( ptr, data.Length, low, high );
            }
        }

        /// <summary> Adjusts all values in the given 2D array so that all values
        /// are between 0 and 1. Adjustment is done in-place.
        /// Stores computed normalization parameters. </summary>
        /// <param name="data"> Raw data to normalize. </param>
        /// <param name="multiplier"> Computed normalization multiplier (by which all values were multiplied). </param>
        /// <param name="constant"> Computed normalization constant (which is added to all values). </param>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe void Normalize( [NotNull] float[,,] data, out float multiplier, out float constant ) {
            fixed( float* ptr = data ) {
                CalculateNormalizationParams( ptr, data.Length, 0f, 1f, out multiplier, out constant );
                for( int i = 0; i < data.Length; i++ ) {
                    ptr[i] = ptr[i]*multiplier + constant;
                }
            }
        }

        static unsafe void NormalizeImpl( float* data, int dataLength, float low, float high ) {
            float multiplier, constant;
            CalculateNormalizationParams( data, dataLength, low, high, out multiplier, out constant );
            for( int i = 0; i < dataLength; i++ ) {
                data[i] = data[i]*multiplier + constant;
            }
        }

        static unsafe void CalculateNormalizationParams( [NotNull] float* ptr, int length, float low, float high,
                                                         out float multiplier, out float constant ) {
            if( ptr == null ) throw new ArgumentNullException( "ptr" );
            float min = float.MaxValue,
                  max = float.MinValue;

            for( int i = 0; i < length; i++ ) {
                min = Math.Min( min, ptr[i] );
                max = Math.Max( max, ptr[i] );
            }

            multiplier = (high - low)/(max - min);
            constant = -min*(high - low)/(max - min) + low;
        }

        #endregion

        #region Filters

        const float BoxBlurDivisor = 1/9f;
        const float GaussianBlurDivisor = 1/273f;
        const float SlopeDivisor = 1/20f;

        const int ThresholdSearchPasses = 10;

        // assumes normalized input
        public static unsafe void Marble( [NotNull] float[,] map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            fixed( float* ptr = map ) {
                for( int i = 0; i < map.Length; i++ ) {
                    ptr[i] = Math.Abs( ptr[i]*2 - 1 );
                }
            }
        }

        public static unsafe void Marble( [NotNull] float[,,] map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            fixed( float* ptr = map ) {
                for( int i = 0; i < map.Length; i++ ) {
                    ptr[i] = Math.Abs( ptr[i]*2 - 1 );
                }
            }
        }


        // assumes normalized input
        public static unsafe void Blend( [NotNull] float[,] data1, [NotNull] float[,] data2, [NotNull] float[,] blendMap ) {
            if( data1 == null ) throw new ArgumentNullException( "data1" );
            if( data2 == null ) throw new ArgumentNullException( "data2" );
            if( blendMap == null ) throw new ArgumentNullException( "blendMap" );
            if( data1.GetLength( 0 ) != data2.GetLength( 0 ) || data1.GetLength( 0 ) != blendMap.GetLength( 0 ) ||
                data1.GetLength( 1 ) != data2.GetLength( 1 ) || data1.GetLength( 1 ) != blendMap.GetLength( 1 ) ) {
                throw new ArgumentException( "Dimensions of data1, data2, and blendMap must all match." );
            }
            fixed( float* ptr1 = data1, ptr2 = data2, ptrBlend = blendMap ) {
                for( int i = 0; i < data1.Length; i++ ) {
                    ptr1[i] += ptr1[i]*ptrBlend[i] + ptr2[i]*(1 - ptrBlend[i]);
                }
            }
        }


        public static unsafe void Add( [NotNull] float[,] data1, [NotNull] float[,] data2 ) {
            if( data1 == null ) throw new ArgumentNullException( "data1" );
            if( data2 == null ) throw new ArgumentNullException( "data2" );
            if( data1.GetLength( 0 ) != data2.GetLength( 0 ) ||
                data1.GetLength( 1 ) != data2.GetLength( 1 ) ) {
                throw new ArgumentException( "Dimensions of data1 and data2 must match." );
            }
            fixed( float* ptr1 = data1, ptr2 = data2 ) {
                for( int i = 0; i < data1.Length; i++ ) {
                    ptr1[i] += ptr2[i];
                }
            }
        }


        public static void ApplyBias( [NotNull] float[,] data, float c00, float c01, float c10, float c11,
                                      float midpoint ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            float maxX = 2f/data.GetLength( 0 );
            float maxY = 2f/data.GetLength( 1 );
            int offsetX = data.GetLength( 0 )/2;
            int offsetY = data.GetLength( 1 )/2;

            for( int x = offsetX - 1; x >= 0; x-- ) {
                for( int y = offsetY - 1; y >= 0; y-- ) {
                    data[x, y] += InterpolateCosine( c00, (c00 + c01)/2, (c00 + c10)/2, midpoint, x*maxX, y*maxY );
                    data[x + offsetX, y] += InterpolateCosine( (c00 + c10)/2,
                                                               midpoint,
                                                               c10,
                                                               (c11 + c10)/2,
                                                               x*maxX,
                                                               y*maxY );
                    data[x, y + offsetY] += InterpolateCosine( (c00 + c01)/2,
                                                               c01,
                                                               midpoint,
                                                               (c01 + c11)/2,
                                                               x*maxX,
                                                               y*maxY );
                    data[x + offsetX, y + offsetY] += InterpolateCosine( midpoint,
                                                                         (c01 + c11)/2,
                                                                         (c11 + c10)/2,
                                                                         c11,
                                                                         x*maxX,
                                                                         y*maxY );
                }
            }
        }


        /// <summary> Scales all values in given 2D array by given amount,
        /// relative to 0.5, and clips the result to 0...1 range. Scaling is done in-place. </summary>
        /// <param name="data"> 2D array of values, normalized to 0...1 range. This array will be modified. </param>
        /// <param name="steepness"> Ratio by which the input will be scaled. </param>
        /// <exception cref="ArgumentNullException"> data is null. </exception>
        public static unsafe void ScaleAndClip( [NotNull] float[,] data, float steepness ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                ScaleAndClipImpl( ptr, data.Length, steepness );
            }
        }

        /// <summary> Scales all values in given 3D array by given amount,
        /// relative to 0.5, and clips the result to 0...1 range. Scaling is done in-place. </summary>
        /// <param name="data"> 3D array of values, normalized to 0...1 range. This array will be modified. </param>
        /// <param name="steepness"> Ratio by which the input will be scaled. </param>
        /// <exception cref="ArgumentNullException"> data is null. </exception>
        public static unsafe void ScaleAndClip( [NotNull] float[,,] data, float steepness ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                ScaleAndClipImpl( ptr, data.Length, steepness );
            }
        }

        static unsafe void ScaleAndClipImpl( float* data, int dataLength, float steepness ) {
            for( int i = 0; i < dataLength; i++ ) {
                data[i] = Math.Min( 1, Math.Max( 0, data[i]*steepness*2 - steepness ) );
            }
        }


        /// <summary> Inverts values in given 2D array. Assumes that data is normalized to 0...1 range. </summary>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe void Invert( [NotNull] float[,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                InvertImpl( ptr, data.Length );
            }
        }

        /// <summary> Inverts values in given 3D array. Assumes that data is normalized to 0...1 range. </summary>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe void Invert( [NotNull] float[,,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                InvertImpl( ptr, data.Length );
            }
        }

        static unsafe void InvertImpl( float* ptr, int length ) {
            for( int i = 0; i < length; i++ ) {
                ptr[i] = 1 - ptr[i];
            }
        }


        /// <summary> Creates a 2D array by applying a 3x3 box blur filter to the given 2D array. 
        /// Note that values at outer-most coordinates (x=0, y=0, x=Width-1, or y=Height-1) are copied untouched. </summary>
        /// <param name="data"> 2D array of input data. Not affected by this filter. </param>
        /// <returns> A new array of the same size as <paramref name="data"/>, with blurred data. </returns>
        /// <exception cref="ArgumentNullException"> data is null. </exception>
        /// <remarks>
        /// The coefficient matrix is:
        /// [1 1 1]
        /// [1 1 1]
        /// [1 1 1]
        /// The divisor is 9.
        /// </remarks>
        [NotNull]
        public static float[,] BoxBlur( [NotNull] float[,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            float[,] output = new float[data.GetLength( 0 ), data.GetLength( 1 )];
            for( int x = data.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = data.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( (x == 0) || (y == 0) || (x == data.GetLength( 0 ) - 1) ||
                        (y == data.GetLength( 1 ) - 1) ) {
                        output[x, y] = data[x, y];
                    } else {
                        output[x, y] = (data[x - 1, y - 1] + data[x - 1, y] + data[x - 1, y + 1] +
                                        data[x, y - 1] + data[x, y] + data[x, y + 1] +
                                        data[x + 1, y - 1] + data[x + 1, y] + data[x + 1, y + 1])*
                                       BoxBlurDivisor;
                    }
                }
            }
            return output;
        }


        /// <summary> Creates a 2D array by applying a 5x5 Gaussian blur filter to the given 2D array. 
        /// Note that values at outer-most coordinates (x=0, y=0, x=Width-1, or y=Height-1) are copied untouched.</summary>
        /// <param name="data"> 2D array of input data. Not affected by this filter. </param>
        /// <returns> A new array of the same dimensions as <paramref name="data"/>, with blurred data. </returns>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        /// <remarks>
        /// The coefficient matrix is:
        /// [1   4   7   4   1]
        /// [4  16  26  16   4]
        /// [7  26  41  26   7]
        /// [4  16  26  16   4]
        /// [1   4   7   4   1]
        /// The divisor is 273.
        /// </remarks>
        [NotNull]
        public static float[,] GaussianBlur5X5( [NotNull] float[,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            float[,] output = new float[data.GetLength( 0 ), data.GetLength( 1 )];
            for( int x = data.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = data.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( (x < 2) || (y < 2) || (x > data.GetLength( 0 ) - 3) ||
                        (y > data.GetLength( 1 ) - 3) ) {
                        output[x, y] = data[x, y];
                    } else {
                        output[x, y] = (data[x - 2, y - 2] + data[x - 1, y - 2]*4 + data[x, y - 2]*7 +
                                        data[x + 1, y - 2]*4 + data[x + 2, y - 2] + data[x - 1, y - 1]*4 +
                                        data[x - 1, y - 1]*16 + data[x, y - 1]*26 + data[x + 1, y - 1]*16 +
                                        data[x + 2, y - 1]*4 + data[x - 2, y]*7 + data[x - 1, y]*26 + data[x, y]*41 +
                                        data[x + 1, y]*26 + data[x + 2, y]*7 + data[x - 2, y + 1]*4 +
                                        data[x - 1, y + 1]*16 + data[x, y + 1]*26 + data[x + 1, y + 1]*16 +
                                        data[x + 2, y + 1]*4 + data[x - 2, y + 2] + data[x - 1, y + 2]*4 +
                                        data[x, y + 2]*7 + data[x + 1, y + 2]*4 + data[x + 2, y + 2])*
                                       GaussianBlurDivisor;
                    }
                }
            }
            return output;
        }


        /// <summary> Approximates "steepness" (magnitude of the gradient) for
        /// each coordinate the given 2D data set. Makes most sense when applied to heightmap data.
        /// Data should be to be normalized to 0...1 range. </summary>
        /// <param name="data"> 2D array of input data. Not affected by this filter. </param>
        /// <returns> A new array of the same dimensions as <paramref name="data"/>, with steepness for each coordinate. </returns>
        /// <exception cref="ArgumentNullException"> data is null. </exception>
        /// <remarks> Steepness at each coordinate is the weighted average of the magnitudes of differences between
        /// value at that coordinate and values at its neighbor coordinates, using this weighing matrix:
        /// [2 3 2]
        /// [3 - 3]
        /// [2 3 2]
        /// </remarks>
        [NotNull]
        public static float[,] CalculateSteepness( [NotNull] float[,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            float[,] output = new float[data.GetLength( 0 ), data.GetLength( 1 )];
            int width1 = data.GetLength( 0 ) - 1,
                height1 = data.GetLength( 1 ) - 1;

            for( int x = width1; x >= 0; x-- ) {
                for( int y = height1; y >= 0; y-- ) {
                    if( (x == 0) || (y == 0) || (x == width1) || (y == height1) ) {
                        output[x, y] = (Math.Abs( data[x, Math.Max( 0, y - 1 )] - data[x, y] )*3 +
                                        Math.Abs( data[x, Math.Min( height1, y + 1 )] - data[x, y] )*3 +
                                        Math.Abs( data[Math.Max( 0, x - 1 ), y] - data[x, y] )*3 +
                                        Math.Abs( data[Math.Min( width1, x + 1 ), y] - data[x, y] )*3 +
                                        Math.Abs( data[Math.Max( 0, x - 1 ), Math.Max( 0, y - 1 )] -
                                                  data[x, y] )*2 +
                                        Math.Abs( data[Math.Min( width1, x + 1 ), Math.Max( 0, y - 1 )] -
                                                  data[x, y] )*2 +
                                        Math.Abs( data[Math.Max( 0, x - 1 ), Math.Min( height1, y + 1 )] -
                                                  data[x, y] )*2 +
                                        Math.Abs( data[Math.Min( width1, x + 1 ), Math.Min( height1, y + 1 )] -
                                                  data[x, y] )*2)*SlopeDivisor;
                    } else {
                        output[x, y] = (Math.Abs( data[x, y - 1] - data[x, y] )*3 +
                                        Math.Abs( data[x, y + 1] - data[x, y] )*3 +
                                        Math.Abs( data[x - 1, y] - data[x, y] )*3 +
                                        Math.Abs( data[x + 1, y] - data[x, y] )*3 +
                                        Math.Abs( data[x - 1, y - 1] - data[x, y] )*2 +
                                        Math.Abs( data[x + 1, y - 1] - data[x, y] )*2 +
                                        Math.Abs( data[x - 1, y + 1] - data[x, y] )*2 +
                                        Math.Abs( data[x + 1, y + 1] - data[x, y] )*2)*SlopeDivisor;
                    }
                }
            }

            return output;
        }


        /// <summary> Finds a threshold value such that ratio of values below the threshold ("coverage")
        /// approximately equals <paramref name="desiredCoverage"/>. This is an imprecise function,
        /// but it typically achieves coverage within 0.001 of desired value. </summary>
        /// <param name="data"> 2D array of data to process. Should be normalized to range 0...1 </param>
        /// <param name="desiredCoverage"> Target "coverage" (ratio of blocks below the threshold). </param>
        /// <returns> Computed threshold, somewhere between 0 and 1. </returns>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe float FindThreshold( [NotNull] float[,] data, float desiredCoverage ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                return FindThresholdImpl( ptr, data.Length, desiredCoverage );
            }
        }

        /// <summary> Finds a threshold value such that ratio of values below the threshold ("coverage")
        /// approximately equals <paramref name="desiredCoverage"/>. This is an imprecise function,
        /// but it typically achieves coverage within 0.001 of desired value. </summary>
        /// <param name="data"> 3D array of data to process. Should be normalized to range 0...1 </param>
        /// <param name="desiredCoverage"> Target "coverage" (ratio of blocks below the threshold). </param>
        /// <returns> Computed threshold, somewhere between 0 and 1. </returns>
        /// <exception cref="ArgumentNullException"> data is null </exception>
        public static unsafe float FindThreshold( [NotNull] float[,,] data, float desiredCoverage ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                return FindThresholdImpl( ptr, data.Length, desiredCoverage );
            }
        }

        static unsafe float FindThresholdImpl( float* data, int dataLength, float desiredCoverage ) {
            if( desiredCoverage < float.Epsilon ) return 0;
            if( desiredCoverage > 1 - float.Epsilon ) return 1;
            float threshold = 0.5f;
            for( int i = 1; i <= ThresholdSearchPasses; i++ ) {
                float coverage = CalculateCoverage( data, dataLength, threshold );
                if( coverage > desiredCoverage ) {
                    threshold = threshold - 1/(float)(2 << i);
                } else {
                    threshold = threshold + 1/(float)(2 << i);
                }
            }
            return threshold;
        }

        static unsafe float CalculateCoverage( [NotNull] float* data, int length, float threshold ) {
            int coveredVoxels = 0;
            float* end = data + length;
            while( data < end ) {
                if( *data < threshold ) coveredVoxels++;
                data++;
            }
            return coveredVoxels/(float)length;
        }

        #endregion
    }
}
