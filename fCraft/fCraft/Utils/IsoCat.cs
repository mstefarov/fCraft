using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/*
The MIT License

Copyright (c) 2010 Matvei Stefarov <me@matvei.org>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace fListBot {

    class Map {
        public byte[] blocks; // standard minecraft tiles
        public short[,] shadows; // heightmap
        public short widthX; // equivalent to Notch's X
        public short height; // equivalent to Notch's Y
        public short widthY; // equivalent to Notch's Z
        public short dim;
    }

    unsafe class IsoCat {
        static byte[] tiles, stiles;
        static int tileX, tileY;
        static int maxTileDim;
        static int tileStride;
        const string Tileset = "tileset3.tif",
                     TilesetShadowed = "tileset3s.tif";

        public static void Init() {
            using( Bitmap tilesBmp = (Bitmap)Bitmap.FromFile( Tileset ) ) {

                tileX = tilesBmp.Width / 50;
                tileY = tilesBmp.Height;
                tileStride = tileX * tileY * 4;
                tiles = new byte[50 * tileStride];

                maxTileDim = Math.Max( tileX, tileY );

                for( int i = 0; i < 50; i++ ) {
                    for( int y = 0; y < tileY; y++ ) {
                        for( int x = 0; x < tileX; x++ ) {
                            int p = i * tileStride + (y * tileX + x) * 4;
                            Color c = tilesBmp.GetPixel( x + i * tileX, y );
                            tiles[p] = c.B;
                            tiles[p + 1] = c.G;
                            tiles[p + 2] = c.R;
                            tiles[p + 3] = c.A;
                        }
                    }
                }
            }

            using( Bitmap stilesBmp = (Bitmap)Bitmap.FromFile( TilesetShadowed ) ) {

                stiles = new byte[50 * tileStride];

                for( int i = 0; i < 50; i++ ) {
                    for( int y = 0; y < tileY; y++ ) {
                        for( int x = 0; x < tileX; x++ ) {
                            int p = i * tileStride + (y * tileX + x) * 4;
                            Color c = stilesBmp.GetPixel( x + i * tileX, y );
                            stiles[p] = c.B;
                            stiles[p + 1] = c.G;
                            stiles[p + 2] = c.R;
                            stiles[p + 3] = c.A;
                        }
                    }
                }
            }
        }


        byte* image;
        int imageWidth, imageHeight;
        int x = 0, y = 0, h = 0;
        byte block = 0;
        int rot, mode;
        Map map;
        string filename;
        Bitmap imageBmp;
        BitmapData imageData;

        int dim, dim2, dim1;


        int isoOffset, isoX, isoY, isoH;

        int imageStride;


        public IsoCat( Map _map, string _filename, int _mode, int _rot ) {
            rot = _rot;
            mode = _mode;
            map = _map;
            filename = _filename;

            dim = map.dim;
            dim2 = dim / 2 - 1;
            dim1 = dim - 1;

            imageWidth = tileX * dim + tileY / 2 * map.height + tileX * 2;
            imageHeight = tileY / 2 * map.height + maxTileDim / 2 * Math.Max( dim, map.height ) + tileY * 2;

            imageBmp = new Bitmap( imageWidth, imageHeight, PixelFormat.Format32bppArgb );
            imageData = imageBmp.LockBits( new Rectangle( 0, 0, imageBmp.Width, imageBmp.Height ),
                                           ImageLockMode.ReadWrite,
                                           PixelFormat.Format32bppArgb );

            image = (byte*)imageData.Scan0;
            imageStride = imageData.Stride;

            isoOffset = (map.height * tileY / 2 * imageStride + imageStride / 2 + tileX * 2);
            isoX = (tileX / 4 * imageStride + tileX * 2);
            isoY = (tileY / 4 * imageStride - tileY * 2);
            isoH = (-tileY / 2 * imageStride);
        }



        byte* bp, ctp;
        public unsafe Bitmap Draw() {
            int blockRight, blockLeft, blockUp;

            fixed( byte* bpx = map.blocks ) {
                fixed( byte* tp = tiles ) {
                    fixed( byte* stp = stiles ) {
                        bp = bpx;
                        while( h < map.height ) {
                            block = GetBlock( x, y, h );
                            if( block != 0 ) {

                                switch( rot ) {
                                    case 0: ctp = (h >= map.shadows[y,x] ? tp : stp); break;
                                    case 1: ctp = (h >= map.shadows[x,dim1 - y] ? tp : stp); break;
                                    case 2: ctp = (h >= map.shadows[dim1 - y,dim1 - x] ? tp : stp); break;
                                    default: ctp = (h >= map.shadows[dim1 - x,y] ? tp : stp); break;
                                }

                                if( x != dim1 ) blockRight = GetBlock( x + 1, y, h );
                                else blockRight = 0;
                                if( y != dim1 ) blockLeft = GetBlock( x, y + 1, h );
                                else blockLeft = 0;
                                if( h != map.height - 1 ) blockUp = GetBlock( x, y, h + 1 );
                                else blockUp = 0;

                                if( blockUp == 0 || blockLeft == 0 || blockRight == 0 || // air
                                    blockUp == 8 || blockLeft == 8 || blockRight == 8 || // water
                                    blockUp == 9 || blockLeft == 9 || blockRight == 9 || // water
                                    (block != 20 && (blockUp == 20 || blockLeft == 20 || blockRight == 20 )) || // glass
                                    blockUp == 18 || blockLeft == 18 || blockRight == 18 || // foliage
                                    blockLeft == 44 || blockRight == 44 || // step

                                    blockUp == 10 || blockLeft == 10 || blockRight == 10 || // lava
                                    blockUp == 11 || blockLeft == 11 || blockRight == 11 || // lava

                                    blockUp == 37 || blockLeft == 37 || blockRight == 37 || // flower
                                    blockUp == 38 || blockLeft == 38 || blockRight == 38 || // flower
                                    blockUp == 6 || blockLeft == 6 || blockRight == 6 || // tree
                                    blockUp == 39 || blockLeft == 39 || blockRight == 39 || // mushroom
                                    blockUp == 40 || blockLeft == 40 || blockRight == 40 ) // mushroom
                                    BlendTile();
                            }

                            x++;
                            if( x == dim ) {
                                y++;
                                x = 0;
                            }
                            if( y == dim ) {
                                h++;
                                y = 0;
                                //TODO: update status if( dim % 16 == 0 ) task.UpdateDrawStatus( h / (float)map.height );
                            }
                        }
                    }
                }
            }
            imageBmp.UnlockBits( imageData );
            return imageBmp;
        }


        unsafe void BlendTile() {
            int xpos = x * isoX + y * isoY + h * isoH + isoOffset;
            if( block > 49 ) return;
            int tileOffset = block * tileStride;
            BlendPixel( xpos, tileOffset );
            BlendPixel( xpos + 4, tileOffset + 4 );
            BlendPixel( xpos + 8, tileOffset + 8 );
            BlendPixel( xpos + 12, tileOffset + 12 );
            xpos += imageStride;
            BlendPixel( xpos, tileOffset + 16 );
            BlendPixel( xpos + 4, tileOffset + 20 );
            BlendPixel( xpos + 8, tileOffset + 24 );
            BlendPixel( xpos + 12, tileOffset + 28 );
            xpos += imageStride;
            BlendPixel( xpos, tileOffset + 32 );
            BlendPixel( xpos + 4, tileOffset + 36 );
            BlendPixel( xpos + 8, tileOffset + 40 );
            BlendPixel( xpos + 12, tileOffset + 44 );
            xpos += imageStride;
            BlendPixel( xpos, tileOffset + 48 );
            BlendPixel( xpos + 4, tileOffset + 52 );
            BlendPixel( xpos + 8, tileOffset + 56 );
            BlendPixel( xpos + 12, tileOffset + 60 );
        }


        byte tA;
        int FA, SA, DA;
        unsafe void BlendPixel( int imageOffset, int tileOffset ) {
            if( ctp[tileOffset + 3] == 0 ) return;

            tA = ctp[tileOffset + 3];

            // Get final alpha channel.
            FA = tA + ((255 - tA) * image[imageOffset + 3]) / 255;

            // Get percentage (out of 256) of source alpha compared to final alpha
            SA = 0;
            if( FA != 0 ) SA = tA * 255 / FA;

            // Destination percentage is just the additive inverse.
            DA = 255 - SA;

            image[imageOffset] = (byte)((ctp[tileOffset] * SA + image[imageOffset] * DA) / 255);
            image[imageOffset + 1] = (byte)((ctp[tileOffset + 1] * SA + image[imageOffset + 1] * DA) / 255);
            image[imageOffset + 2] = (byte)((ctp[tileOffset + 2] * SA + image[imageOffset + 2] * DA) / 255);
            image[imageOffset + 3] = (byte)FA;
        }


        byte GetBlock( int xx, int yy, int hh ) {
            int xpos;
            if( rot == 0 ) {
                xpos = (hh * dim + yy) * dim + xx;
            } else if( rot == 1 ) {
                xpos = (hh * dim + xx) * dim + (dim1 - yy);
            } else if( rot == 2 ) {
                xpos = (hh * dim + (dim1 - yy)) * dim + (dim1 - xx);
            } else {
                xpos = (hh * dim + (dim1 - xx)) * dim + yy;
            }

            byte bl = (byte)(bp[xpos]);
            int test = bl;
            if( mode == 0 ) {

                return bp[xpos];
            } else if( bp[xpos] != 0 ) {
                if( mode == 1 && (xx == dim1 || yy == dim1 || hh == map.height - 1) ) {
                    return 0;
                } else if( mode == 2 && xx > dim2 && yy > dim2 ) {
                    return 0;
                } else {
                    return bp[xpos];
                }
            } else {
                return 0;
            }
        }
    }
}