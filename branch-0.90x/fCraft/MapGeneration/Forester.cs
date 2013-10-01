// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// Based on Forester script by dudecon, ported from Java with permission.
// Original: http://www.minecraftforum.net/viewtopic.php?f=25&t=9426
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft.Events {
    public sealed class ForesterBlockPlacingEventArgs : EventArgs {
        internal ForesterBlockPlacingEventArgs( Vector3I coordinate, Block block ) {
            Coordinate = coordinate;
            Block = block;
        }
        public Vector3I Coordinate { get; private set; }
        public Block Block { get; private set; }
    }
}

namespace fCraft.MapGeneration {
    /// <summary> Vegetation generator for RealisticMapGenState. </summary>
    public static class Forester {
        const int MaxTries = 1000;

        public static void Generate( [NotNull] ForesterArgs args ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            args.Validate();
            List<Tree> treeList = new List<Tree>();

            if( args.Operation == ForesterOperation.Conserve ) {
                FindTrees( args, treeList );
            }

            if( args.TreeCount > 0 && treeList.Count > args.TreeCount ) {
                treeList = treeList.Take( args.TreeCount ).ToList();
            }

            if( args.Operation == ForesterOperation.Replant || args.Operation == ForesterOperation.Add ) {
                switch( args.Shape ) {
                    case TreeShape.Rainforest:
                        PlantRainForestTrees( args, treeList );
                        break;
                    case TreeShape.Mangrove:
                        PlantMangroves( args, treeList );
                        break;
                    default:
                        PlantTrees( args, treeList );
                        break;
                }
            }

            if( args.Operation == ForesterOperation.ClearCut ) return;

            ProcessTrees( args, treeList );
            if( args.Foliage ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeFoliage();
                }
            }
            if( args.Wood ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeTrunk();
                }
            }
        }


        public static void Plant( [NotNull] ForesterArgs args, Vector3I treeCoordinate ) {
            List<Tree> treeList = new List<Tree> {
                new Tree {
                    Args = args,
                    Height = args.Height,
                    Pos = treeCoordinate
                }
            };
            switch( args.Shape ) {
                case TreeShape.Rainforest:
                    PlantRainForestTrees( args, treeList );
                    break;
                case TreeShape.Mangrove:
                    PlantMangroves( args, treeList );
                    break;
                default:
                    PlantTrees( args, treeList );
                    break;
            }
            ProcessTrees( args, treeList );
            if( args.Foliage ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeFoliage();
                }
            }
            if( args.Wood ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeTrunk();
                }
            }
        }


        static void FindTrees( [NotNull] ForesterArgs args, [NotNull] ICollection<Tree> treeList ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            if( treeList == null ) throw new ArgumentNullException( "treeList" );
            int treeHeight = args.Height;

            for( int x = 0; x < args.Map.Width; x++ ) {
                for( int z = 0; z < args.Map.Length; z++ ) {
                    int y = args.Map.Height - 1;
                    while( true ) {
                        int foliageTop = args.Map.SearchColumn( x, z, args.FoliageBlock, y );
                        if( foliageTop < 0 ) break;
                        y = foliageTop;
                        Vector3I trunkTop = new Vector3I( x, y - 1, z );
                        int height = DistanceToBlock( args.Map, new Vector3F( trunkTop ), Vector3F.Down, args.TrunkBlock, true );
                        if( height == 0 ) {
                            y--;
                            continue;
                        }
                        y -= height;
                        if( args.Height > 0 ) {
                            height = args.Rand.Next( treeHeight - args.HeightVariation,
                                                     treeHeight + args.HeightVariation + 1 );
                        }
                        treeList.Add( new Tree {
                            Args = args,
                            Pos = new Vector3I( x, y, z ),
                            Height = height
                        } );
                        y--;
                    }
                }
            }
        }


        static void PlantTrees( [NotNull] ForesterArgs args, [NotNull] ICollection<Tree> treeList ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            if( treeList == null ) throw new ArgumentNullException( "treeList" );
            int treeHeight = args.Height;

            int attempts = 0;
            while( treeList.Count < args.TreeCount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeHeight - args.HeightVariation,
                                             treeHeight + args.HeightVariation + 1 );

                Vector3I treeLoc = FindRandomTreeLocation( args, height );
                if( treeLoc.Y < 0 ) continue;
                else treeLoc.Y++;
                treeList.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = treeLoc
                } );
            }
        }


        static Vector3I FindRandomTreeLocation( [NotNull] ForesterArgs args, int height ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            int padding = (int)(height / 3f + 1);
            int minDim = Math.Min( args.Map.Width, args.Map.Length );
            if( padding > minDim / 2.2 ) {
                padding = (int)(minDim / 2.2);
            }
            int x = args.Rand.Next( padding, args.Map.Width - padding - 1 );
            int z = args.Rand.Next( padding, args.Map.Length - padding - 1 );
            int y = args.Map.SearchColumn( x, z, args.PlantOn );
            return new Vector3I( x, y, z);
        }


        static void PlantRainForestTrees( [NotNull] ForesterArgs args, [NotNull] ICollection<Tree> treeList ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            if( treeList == null ) throw new ArgumentNullException( "treeList" );
            int treeHeight = args.Height;

            int existingTreeNum = treeList.Count;
            int remainingTrees = args.TreeCount - existingTreeNum;

            const int shortTreeFraction = 6;
            int attempts = 0;
            for( int i = 0; i < remainingTrees && attempts < MaxTries; attempts++ ) {
                float randomFac =
                    (float)( ( Math.Sqrt( args.Rand.NextDouble() ) * 1.618 - .618 ) * args.HeightVariation + .5 );

                int height;
                if( i % shortTreeFraction == 0 ) {
                    height = (int)( treeHeight + randomFac );
                } else {
                    height = (int)( treeHeight - randomFac );
                }
                Vector3I xyz = FindRandomTreeLocation( args, height );
                if( xyz.Y < 0 ) continue;

                xyz.Y++;

                bool displaced = false;
                foreach( Tree otherTree in treeList ) {
                    Vector3I otherLoc = otherTree.Pos;
                    int tallX = otherLoc[0];
                    int tallZ = otherLoc[2];
                    float dist = (float)Math.Sqrt( Sqr( tallX - xyz.X + .5 ) + Sqr( tallZ - xyz.Z + .5 ) );
                    float threshold = ( otherTree.Height + height ) * .193f;
                    if( dist < threshold ) {
                        displaced = true;
                        break;
                    }
                }
                if( displaced ) continue;
                treeList.Add( new RainforestTree {
                    Args = args,
                    Pos = xyz,
                    Height = height
                } );
                i++;
            }
        }


        static void PlantMangroves( [NotNull] ForesterArgs args, [NotNull] ICollection<Tree> treeList ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            if( treeList == null ) throw new ArgumentNullException( "treeList" );
            int treeHeight = args.Height;

            int attempts = 0;
            while( treeList.Count < args.TreeCount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeHeight - args.HeightVariation,
                                             treeHeight + args.HeightVariation + 1 );
                int padding = (int)(height / 3f + 1);
                int minDim = Math.Min( args.Map.Width, args.Map.Length );
                if( padding > minDim / 2.2 ) {
                    padding = (int)(minDim / 2.2);
                }
                int x = args.Rand.Next( padding, args.Map.Width - padding - 1 );
                int z = args.Rand.Next( padding, args.Map.Length - padding - 1 );
                int top = args.Map.Height - 1;

                int y = top - DistanceToBlock( args.Map, new Vector3F( x, z, top ), Vector3F.Down, Block.Air, true );
                int dist = DistanceToBlock( args.Map, new Vector3F( x, z, y ), Vector3F.Down, Block.Water, true );

                if( dist > height * .618 || dist == 0 ) {
                    continue;
                }

                y += (int)Math.Sqrt( height - dist ) + 2;
                treeList.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = new Vector3I( x, y, z )
                } );
            }
        }


        static void ProcessTrees( [NotNull] ForesterArgs args, [NotNull] IList<Tree> treeList ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            if( treeList == null ) throw new ArgumentNullException( "treeList" );
            TreeShape[] shapeChoices;
            switch( args.Shape ) {
                case TreeShape.Stickly:
                    shapeChoices = new[]{ TreeShape.Normal,
                                          TreeShape.Bamboo,
                                          TreeShape.Palm };
                    break;
                case TreeShape.Procedural:
                    shapeChoices = new[]{ TreeShape.Round,
                                          TreeShape.Cone };
                    break;
                default:
                    shapeChoices = new[] { args.Shape };
                    break;
            }

            for( int i = 0; i < treeList.Count; i++ ) {
                TreeShape newShape = shapeChoices[args.Rand.Next( 0, shapeChoices.Length )];
                Tree newTree;
                switch( newShape ) {
                    case TreeShape.Normal:
                        newTree = new NormalTree();
                        break;
                    case TreeShape.Bamboo:
                        newTree = new BambooTree();
                        break;
                    case TreeShape.Palm:
                        newTree = new PalmTree();
                        break;
                    case TreeShape.Round:
                        newTree = new RoundTree();
                        break;
                    case TreeShape.Cone:
                        newTree = new ConeTree();
                        break;
                    case TreeShape.Rainforest:
                        newTree = new RainforestTree();
                        break;
                    case TreeShape.Mangrove:
                        newTree = new MangroveTree();
                        break;
                    default:
                        throw new ArgumentException( "Unknown tree shape type" );
                }
                newTree.Copy( treeList[i] );

                if( args.MapHeightLimit ) {
                    int height = newTree.Height;
                    int yBase = newTree.Pos[1];
                    int mapHeight = args.Map.Height;
                    int foliageHeight;
                    if( args.Shape == TreeShape.Rainforest ) {
                        foliageHeight = 2;
                    } else {
                        foliageHeight = 4;
                    }
                    if( yBase + height + foliageHeight > mapHeight ) {
                        newTree.Height = mapHeight - yBase - foliageHeight;
                    }
                }

                if( newTree.Height < 1 ) newTree.Height = 1;
                newTree.Prepare();
                treeList[i] = newTree;
            }
        }


        #region Trees

        class Tree {
            public Vector3I Pos;
            public int Height = 1;
            public ForesterArgs Args;

            public virtual void Prepare() { }

            public virtual void MakeTrunk() { }

            public virtual void MakeFoliage() { }

            public void Copy( [NotNull] Tree other ) {
                if( other == null ) throw new ArgumentNullException( "other" );
                Args = other.Args;
                Pos = other.Pos;
                Height = other.Height;
            }
        }


        class StickTree : Tree {
            public override void MakeTrunk() {
                for( int i = 0; i < Height; i++ ) {
                    Args.PlaceBlock( Pos.X, Pos.Z, Pos.Y + i, Args.TrunkBlock );
                }
            }
        }


        sealed class NormalTree : StickTree {
            public override void MakeFoliage() {
                int topY = Pos[1] + Height - 1;
                int start = topY - 2;
                int end = topY + 2;

                for( int y = start; y < end; y++ ) {
                    int rad;
                    if( y > start + 1 ) {
                        rad = 1;
                    } else {
                        rad = 2;
                    }
                    for( int xOff = -rad; xOff < rad + 1; xOff++ ) {
                        for( int zOff = -rad; zOff < rad + 1; zOff++ ) {
                            if( Args.Rand.NextDouble() > .618 &&
                                Math.Abs( xOff ) == Math.Abs( zOff ) &&
                                Math.Abs( xOff ) == rad ) {
                                continue;
                            }
                            Args.PlaceBlock( Pos[0] + xOff, Pos[2] + zOff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        sealed class BambooTree : StickTree {
            public override void MakeFoliage() {
                int start = Pos[1];
                int end = start + Height + 1;
                for( int y = start; y < end; y++ ) {
                    for( int i = 0; i < 2; i++ ) {
                        int xOff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        int zOff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        Args.PlaceBlock( Pos[0] + xOff, Pos[2] + zOff, y, Args.FoliageBlock );
                    }
                }
            }
        }


        sealed class PalmTree : StickTree {
            public override void MakeFoliage() {
                int y = Pos[1] + Height;
                for( int xOff = -2; xOff < 3; xOff++ ) {
                    for( int zOff = -2; zOff < 3; zOff++ ) {
                        if( Math.Abs( xOff ) == Math.Abs( zOff ) ) {
                            Args.PlaceBlock( Pos[0] + xOff, Pos[2] + zOff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        class ProceduralTree : Tree {
            protected float TrunkRadius { get; set; }
            protected float BranchSlope { get; set; }
            protected float TrunkHeight { get; set; }
            float BranchDensity { get; set; }
            protected float[] FoliageShape { get; set; }
            Vector3I[] FoliageCoords { get; set; }

            void CrossSection( Vector3I center, float radius, int dirAxis, Block matIndex ) {
                int rad = (int)(radius + .618);
                int secIndex1 = (dirAxis - 1) % 3;
                int secIndex2 = (dirAxis + 1) % 3;

                Vector3I coord = new Vector3I();

                for( int off1 = -rad; off1 <= rad; off1++ ) {
                    for( int off2 = -rad; off2 <= rad; off2++ ) {
                        float thisDist = (float)Math.Sqrt( Sqr( Math.Abs( off1 ) + .5 ) +
                                                           Sqr( Math.Abs( off2 ) + .5 ) );
                        if( thisDist > radius ) continue;
                        int pri = center[dirAxis];
                        int sec1 = center[secIndex1] + off1;
                        int sec2 = center[secIndex2] + off2;
                        coord[dirAxis] = pri;
                        coord[secIndex1] = sec1;
                        coord[secIndex2] = sec2;
                        Args.PlaceBlock( coord, matIndex );
                    }
                }
            }


            protected virtual float ShapeFunc( int z ) {
                if( Args.Rand.NextDouble() < 100f / Sqr( Height ) && z < TrunkHeight ) {
                    return Height * .12f;
                } else {
                    return -1;
                }
            }

            void FoliageCluster( Vector3I center ) {
                int z = center[1];
                foreach( float i in FoliageShape ) {
                    CrossSection( new Vector3I( center[0], z, center[2] ), i, 1, Args.FoliageBlock );
                    z++;
                }
            }

            void TaperedLimb( Vector3I start, Vector3I end, float startSize, float endSize ) {
                Vector3I delta = end - start;
                int primIndex = (int)delta.LongestAxis;
                int maxDist = delta[primIndex];
                if( maxDist == 0 ) return;
                int primSign = (maxDist > 0 ? 1 : -1);

                int secIndex1 = (primIndex - 1) % 3;
                int secIndex2 = (primIndex + 1) % 3;

                int secDelta1 = delta[secIndex1];
                float secFac1 = secDelta1 / (float)delta[primIndex];
                int secDelta2 = delta[secIndex2];
                float secFac2 = secDelta2 / (float)delta[primIndex];

                Vector3I coord = new Vector3I();
                int endOffset = delta[primIndex] + primSign;

                for( int primOffset = 0; primOffset < endOffset; primOffset += primSign ) {
                    int primLoc = start[primIndex] + primOffset;
                    int secLoc1 = (int)(start[secIndex1] + primOffset * secFac1);
                    int secLoc2 = (int)(start[secIndex2] + primOffset * secFac2);
                    coord[primIndex] = primLoc;
                    coord[secIndex1] = secLoc1;
                    coord[secIndex2] = secLoc2;
                    float primDist = Math.Abs( delta[primIndex] );
                    float radius = endSize + (startSize - endSize) * Math.Abs( delta[primIndex] - primOffset ) / primDist;

                    CrossSection( coord, radius, primIndex, Args.TrunkBlock );
                }
            }

            public override void MakeFoliage() {
                foreach( Vector3I coord in FoliageCoords ) {
                    FoliageCluster( coord );
                }
                foreach( Vector3I coord in FoliageCoords ) {
                    Args.PlaceBlock( coord, Args.FoliageBlock );
                }
            }

            void MakeBranches() {
                int topY = Pos[1] + (int)(TrunkHeight + .5);
                float endRad = TrunkRadius * (1 - TrunkHeight / Height);
                if( endRad < 1 ) endRad = 1;

                foreach( Vector3I coord in FoliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord.X - Pos.X ) + Sqr( coord.Z - Pos.Z ) );
                    float distY = coord[1] - Pos[1];
                    float value = (BranchDensity * 220 * Height) / Cub( distY + dist );

                    if( value < Args.Rand.NextDouble() ) continue;

                    int posy = coord[1];
                    float slope = (float)(BranchSlope + (.5 - Args.Rand.NextDouble()) * .16);

                    float branchY, baseSize;
                    if( coord[1] - dist * slope > topY ) {
                        float threshold = 1 / (float)Height;
                        if( Args.Rand.NextDouble() < threshold ) continue;
                        branchY = topY;
                        baseSize = endRad;
                    } else {
                        branchY = posy - dist * slope;
                        baseSize = endRad + (TrunkRadius - endRad) *
                                   (topY - branchY) / TrunkHeight;
                    }

                    float startSize = (float)(baseSize * (1 + Args.Rand.NextDouble()) *
                                              .618 * Math.Pow( dist / Height, .618 ));
                    float randR = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * baseSize * .618);
                    float randAng = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int randX = (int)(randR * Math.Sin( randAng ) + .5);
                    int randZ = (int)(randR * Math.Cos( randAng ) + .5);
                    Vector3I startCoord = new Vector3I {
                        X = Pos[0] + randX,
                        Z = Pos[2] + randZ,
                        Y = (int)branchY
                    };
                    if( startSize < 1 ) startSize = 1;
                    const float endSize = 1;
                    TaperedLimb( startCoord, coord, startSize, endSize );
                }
            }

            struct RootBase {
                public int X, Z;
                public float Radius;
            }

            void MakeRoots( [NotNull] IList<RootBase> rootBases ) {
                if( rootBases == null ) throw new ArgumentNullException( "rootBases" );
                if( rootBases.Count == 0 ) return;
                foreach( Vector3I coord in FoliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord[0] - Pos[0] ) + Sqr( coord[2] - Pos[2] ) );
                    float distY = coord[1] - Pos[1];
                    float value = (BranchDensity * 220 * Height) / Cub( distY + dist );
                    if( value < Args.Rand.NextDouble() ) continue;

                    RootBase rootBase = rootBases[Args.Rand.Next( 0, rootBases.Count )];
                    float rootBaseRadius = rootBase.Radius;

                    float randR = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * rootBaseRadius * .618);
                    float randAng = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int randX = (int)(randR * Math.Sin( randAng ) + .5);
                    int randZ = (int)(randR * Math.Cos( randAng ) + .5);
                    int randY = (int)(Args.Rand.NextDouble() * rootBaseRadius * .5);
                    Vector3I startCoord = new Vector3I {
                        X = rootBase.X + randX,
                        Z = rootBase.Z + randZ,
                        Y = Pos[1] + randY
                    };
                    Vector3F offset = new Vector3F( startCoord - coord );

                    if( Args.Shape == TreeShape.Mangrove ) {
                        // offset = [int(val * 1.618 - 1.5) for val in offset]
                        offset = offset * 1.618f - HalfBlock * 3;
                    }

                    Vector3I endCoord = startCoord + offset.RoundDown();
                    float rootStartSize = (float)(rootBaseRadius * .618 * Math.Abs( offset[1] ) / (Height * .618));

                    if( rootStartSize < 1 ) rootStartSize = 1;
                    const float endSize = 1;

                    if( Args.Roots == RootMode.ToStone ||
                        Args.Roots == RootMode.Hanging ) {
                        float offLength = offset.Length;
                        if( offLength < 1 ) continue;
                        float rootMid = endSize;
                        Vector3F vec = offset / offLength;

                        Block searchIndex = Block.Air;
                        if( Args.Roots == RootMode.ToStone ) {
                            searchIndex = Block.Stone;
                        } else if( Args.Roots == RootMode.Hanging ) {
                            searchIndex = Block.Air;
                        }

                        int startDist = (int)(Args.Rand.NextDouble() * 6 * Math.Sqrt( rootStartSize ) + 2.8);
                        Vector3I searchStart = new Vector3I( startCoord + vec * startDist );

                        dist = startDist + DistanceToBlock( Args.Map, new Vector3F( searchStart ), vec, searchIndex );

                        if( dist < offLength ) {
                            rootMid += (rootStartSize - endSize) * (1 - dist / offLength);
                            endCoord = new Vector3I( startCoord + vec * dist );
                            if( Args.Roots == RootMode.Hanging ) {
                                float remainingDist = offLength - dist;
                                Vector3I bottomCord = endCoord;
                                bottomCord[1] -= (int)remainingDist;
                                TaperedLimb( endCoord, bottomCord, rootMid, endSize );
                            }
                        }
                        TaperedLimb( startCoord, endCoord, rootStartSize, rootMid );
                    } else {
                        TaperedLimb( startCoord, endCoord, rootStartSize, endSize );
                    }
                }
            }

            public override void MakeTrunk() {
                int startY = Pos[1];
                int midY = (int)(Pos[1] + TrunkHeight * .382);
                int topY = (int)(Pos[1] + TrunkHeight + .5);

                float midRad = TrunkRadius * .8f;
                float endRad = TrunkRadius * (1 - TrunkHeight / Height);

                if( endRad < 1 ) endRad = 1;
                if( midRad < endRad ) midRad = endRad;

                float startRad;
                List<RootBase> rootBases = new List<RootBase>();
                if( Args.RootButtresses || Args.Shape == TreeShape.Mangrove ) {
                    startRad = TrunkRadius * .8f;
                    rootBases.Add( new RootBase {
                        X = Pos[0],
                        Z = Pos[2],
                        Radius = startRad
                    } );
                    float buttressRadius = TrunkRadius * .382f;
                    float posRadius = TrunkRadius;
                    if( Args.Shape == TreeShape.Mangrove ) {
                        posRadius *= 2.618f;
                    }
                    int munOfButtresses = (int)(Math.Sqrt( TrunkRadius ) + 3.5);
                    for( int i = 0; i < munOfButtresses; i++ ) {
                        float randAng = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        float thisPosRadius = (float)(posRadius * (.9 + Args.Rand.NextDouble() * .2));
                        int thisX = Pos[0] + (int)(thisPosRadius * Math.Sin( randAng ));
                        int thisZ = Pos[2] + (int)(thisPosRadius * Math.Cos( randAng ));

                        float thisButtressRadius = (float)(buttressRadius * (.618 + Args.Rand.NextDouble()));
                        if( thisButtressRadius < 1 ) thisButtressRadius = 1;

                        TaperedLimb( new Vector3I( thisX, startY, thisZ ), new Vector3I( Pos[0], midY, Pos[2] ),
                                     thisButtressRadius, thisButtressRadius );
                        rootBases.Add( new RootBase {
                            X = thisX,
                            Z = thisZ,
                            Radius = thisButtressRadius
                        } );
                    }
                } else {
                    startRad = TrunkRadius;
                    rootBases.Add( new RootBase {
                        X = Pos[0],
                        Z = Pos[2],
                        Radius = startRad
                    } );
                }
                TaperedLimb( new Vector3I( Pos[0], startY, Pos[2] ), new Vector3I( Pos[0], midY, Pos[2] ), startRad, midRad );
                TaperedLimb( new Vector3I( Pos[0], midY, Pos[2] ), new Vector3I( Pos[0], topY, Pos[2] ), midRad, endRad );
                MakeBranches();
                if( Args.Roots != RootMode.None ) {
                    MakeRoots( rootBases.ToArray() );
                }
            }

            public override void Prepare() {
                base.Prepare();
                TrunkRadius = (float)Math.Sqrt( Height * Args.TrunkThickness );
                if( TrunkRadius < 1 ) TrunkRadius = 1;

                TrunkHeight = Height * .618f;
                BranchDensity = (Args.BranchDensity / Args.FoliageDensity);

                int startY = Pos[1];
                int endY = (Pos[1] + Height);
                int numOfClustersPerY = (int)(1.5 + Sqr( Args.FoliageDensity * Height / 19f ));
                if( numOfClustersPerY < 1 ) numOfClustersPerY = 1;

                List<Vector3I> foliageCoords = new List<Vector3I>();
                for( int y = endY - 1; y >= startY; y-- ) {
                    for( int i = 0; i < numOfClustersPerY; i++ ) {
                        float shapeFac = ShapeFunc( y - startY );
                        if( shapeFac < 0 ) continue;
                        float r = (float)((Math.Sqrt( Args.Rand.NextDouble() ) + .328) * shapeFac);
                        float theta = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        int x = (int)(r * Math.Sin( theta )) + Pos[0];
                        int z = (int)(r * Math.Cos( theta )) + Pos[2];
                        foliageCoords.Add( new Vector3I( x, y, z ) );
                    }
                }
                FoliageCoords = foliageCoords.ToArray();
            }
        }


        class RoundTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = .382f;
                FoliageShape = new[] { 2, 3, 3, 2.5f, 1.6f };
                TrunkRadius *= .8f;
                TrunkHeight = Args.TrunkHeight * Height;
            }


            protected override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;

                if( y < Height * (.282 + .1 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }

                float radius = Height / 2f;
                float adj = Height / 2f - y;
                float dist;
                if( adj == 0 ) {
                    dist = radius;
                } else if( Math.Abs( adj ) >= radius ) {
                    dist = 0;
                } else {
                    dist = (float)Math.Sqrt( radius * radius - adj * adj );
                }
                dist *= .618f;
                return dist;
            }
        }


        sealed class ConeTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = .15f;
                FoliageShape = new[] { 3, 2.6f, 2, 1 };
                TrunkRadius *= .618f;
                TrunkHeight = Height;
            }


            protected override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;
                if( y < Height * (.25 + .05 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }
                float radius = (Height - y) * .382f;
                if( radius < 0 ) radius = 0;
                return radius;
            }
        }


        sealed class RainforestTree : ProceduralTree {
            public override void Prepare() {
                FoliageShape = new[] { 3.4f, 2.6f };
                base.Prepare();
                BranchSlope = 1;
                TrunkRadius *= .382f;
                TrunkHeight = Height * .9f;
            }


            protected override float ShapeFunc( int y ) {
                if( y < Height * .8 ) {
                    if( Args.Height < Height ) {
                        float twigs = base.ShapeFunc( y );
                        if( twigs >= 0 && Args.Rand.NextDouble() < .05 ) {
                            return twigs;
                        }
                    }
                    return -1;
                } else {
                    float width = Height * .382f;
                    float topDist = (Height - y) / (Height * .2f);
                    float dist = (float)(width * (.618 + topDist) * (.618 + Args.Rand.NextDouble()) * .382);
                    return dist;
                }
            }
        }


        sealed class MangroveTree : RoundTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = 1;
                TrunkRadius *= .618f;
            }


            protected override float ShapeFunc( int y ) {
                float val = base.ShapeFunc( y );
                if( val < 0 ) return -1;
                val *= 1.618f;
                return val;
            }
        }

        #endregion


        #region Math Helpers

        static int DistanceToBlock( [NotNull] Map map, Vector3F coord, Vector3F vec, Block blockType ) {
            return DistanceToBlock( map, coord, vec, blockType, false );
        }

        static readonly Vector3F HalfBlock = new Vector3F( .5f, .5f, .5f );
        static int DistanceToBlock( [NotNull] Map map, Vector3F coord, Vector3F vec, Block blockType, bool invert ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            coord += HalfBlock;
            int iterations = 0;
            while( map.InBounds( new Vector3I( coord ) ) ) {
                Block blockAtPos = map.GetBlock( new Vector3I( coord ) );
                if( (blockAtPos == blockType && !invert) ||
                    (blockAtPos != blockType && invert) ) {
                    break;
                } else {
                    coord += vec;
                    iterations++;
                }
            }
            return iterations;
        }

        static float Sqr( float val ) {
            return val * val;
        }

        static float Cub( float val ) {
            return val * val * val;
        }

        static int Sqr( int val ) {
            return val * val;
        }

        static double Sqr( double val ) {
            return val * val;
        }

        #endregion


        #region Enumerations

        public enum ForesterOperation {
            ClearCut,
            Conserve,
            Replant,
            Add
        }

        public enum TreeShape {
            Normal,
            Bamboo,
            Palm,
            Stickly,
            Round,
            Cone,
            Procedural,
            Rainforest,
            Mangrove
        }

        public enum RootMode {
            Normal,
            ToStone,
            Hanging,
            None
        }

        #endregion
    }

    // TODO: Add a UI to RealisticMapGenGui to set these
    public sealed class ForesterArgs {
        // ReSharper disable ConvertToConstant.Global
        public Forester.ForesterOperation Operation = Forester.ForesterOperation.Replant;
        public int TreeCount = 15; // 0 = no limit if op=conserve/replant
        public Forester.TreeShape Shape = Forester.TreeShape.Procedural;
        public int Height = 25;
        public int HeightVariation = 15;
        public bool Wood = true;
        public float TrunkThickness = 1;
        public float TrunkHeight = .7f;
        public float BranchDensity = 1;
        public Forester.RootMode Roots = Forester.RootMode.Normal;
        public bool RootButtresses = true;
        public bool Foliage = true;
        public float FoliageDensity = 1;
        public bool MapHeightLimit = true;
        public Block PlantOn = Block.Grass;
        public Random Rand;
        public Map Map;

        public Block TrunkBlock = Block.Log;
        public Block FoliageBlock = Block.Leaves;
        // ReSharper restore ConvertToConstant.Global

        public event EventHandler<ForesterBlockPlacingEventArgs> BlockPlacing;

        internal void PlaceBlock( int x, int y, int z, Block block ) {
            var h = BlockPlacing;
            if( h != null )
                h( this, new ForesterBlockPlacingEventArgs( new Vector3I( x, y, z ), block ) );
        }

        internal void PlaceBlock( Vector3I coord, Block block ) {
            // todo: rewrite the whole thing to use XYZ coords
            var h = BlockPlacing;
            if( h != null )
                h( this, new ForesterBlockPlacingEventArgs( new Vector3I( coord.X, coord.Z, coord.Y ), block ) );
        }

        internal void Validate() {
            if( TreeCount < 0 ) TreeCount = 0;
            if( Height < 1 ) Height = 1;
            if( HeightVariation > Height ) HeightVariation = Height;
            if( TrunkThickness < 0 ) TrunkThickness = 0;
            if( TrunkHeight < 0 ) TrunkHeight = 0;
            if( FoliageDensity < 0 ) FoliageDensity = 0;
            if( BranchDensity < 0 ) BranchDensity = 0;
        }
    }
}