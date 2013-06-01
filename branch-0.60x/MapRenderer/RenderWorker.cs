using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using fCraft.GUI;
using ImageManipulation;

namespace fCraft.MapRenderer {
    class RenderWorker {
        static int threadCount;
        readonly BlockingQueue<RenderTask> inQueue, outQueue;
        readonly MapRendererParams p;
        readonly int threadNumber;
        IsoCat renderer;
        readonly Thread thread;

        public RenderWorker( BlockingQueue<RenderTask> inputQueue, BlockingQueue<RenderTask> outputQueue, MapRendererParams taskParams ) {
            inQueue = inputQueue;
            outQueue = outputQueue;
            threadCount++;
            threadNumber = threadCount;
            thread = new Thread( RenderLoop ) {
                IsBackground = true,
                Name = "RenderWorker" + threadNumber
            };
            p = taskParams;
        }


        public void Start() {
            thread.Start();
        }


        void RenderLoop() {
            renderer = MakeRenderer();
            using( MemoryStream ms = new MemoryStream() ) {
                while( true ) { // terminates with the rest of the program
                    RenderTask task = inQueue.WaitDequeue();
                    try {
                        // render the map
                        IsoCatResult result = renderer.Draw( task.Map );

                        // crop image (if needed)
                        Image image;
                        if( p.Uncropped ) {
                            image = result.Bitmap;
                        } else {
                            image = result.Bitmap.Clone( result.CropRectangle, result.Bitmap.PixelFormat );
                        }

                        // encode image
                        if( p.ExportFormat.Equals( ImageFormat.Jpeg ) ) {
                            EncoderParameters encoderParams = new EncoderParameters();
                            encoderParams.Param[0] = new EncoderParameter( Encoder.Quality, p.JpegQuality );
                            image.Save( ms, p.ImageEncoder, encoderParams );
                        } else if( p.ExportFormat.Equals( ImageFormat.Gif ) ) {
                            OctreeQuantizer q = new OctreeQuantizer( 255, 8 );
                            image = q.Quantize( image );
                            image.Save( ms, p.ExportFormat );
                        } else {
                            image.Save( ms, p.ExportFormat );
                        }
                        task.Result = ms.ToArray();

                    } catch( Exception ex ) {
                        task.Exception = ex;
                    }
                    outQueue.Enqueue( task );
                    ms.SetLength( 0 );
                }
            }
        }


        IsoCat MakeRenderer() {
            // create and configure the renderer
            IsoCat newRenderer = new IsoCat {
                SeeThroughLava = p.SeeThroughLava,
                SeeThroughWater = p.SeeThroughWater,
                Mode = p.Mode,
                Gradient = !p.NoGradient,
                DrawShadows = !p.NoShadows
            };
            if( p.Mode == IsoCatMode.Chunk ) {
                newRenderer.ChunkCoords[0] = p.Region.XMin;
                newRenderer.ChunkCoords[1] = p.Region.YMin;
                newRenderer.ChunkCoords[2] = p.Region.ZMin;
                newRenderer.ChunkCoords[3] = p.Region.XMax;
                newRenderer.ChunkCoords[4] = p.Region.YMax;
                newRenderer.ChunkCoords[5] = p.Region.ZMax;
            }
            switch( p.Angle ) {
                case 90:
                    newRenderer.Rotation = 1;
                    break;
                case 180:
                    newRenderer.Rotation = 2;
                    break;
                case 270:
                case -90:
                    newRenderer.Rotation = 3;
                    break;
            }
            return newRenderer;
        }
    }
}