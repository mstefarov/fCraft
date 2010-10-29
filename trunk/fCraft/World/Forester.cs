using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {


    class ForesterArgs {
        public Forester.Operation operation = Forester.Operation.Replant;
        public int treeCount = 15; // 0 = no limit if op=conserve/replant
        public Forester.TreeShape shape = Forester.TreeShape.Procedural;
        public int height = 25; // 0 = auto
        public int heightVariation = 15;
        public bool wood = true;
        public Forester.RootMode rootMode = Forester.RootMode.Hanging;
        public float trunkThickness = 1;
        public float trunkHeight = .7f;
        public float branchDensity = 1;
        public bool rootButtresses = true;
        public bool foliage = true;
        public float foliageDensity = 1;
        public bool mapHeightLimit = true;
        public Block plantOn = Block.Grass;
        public Random rand;
        public Map inMap;
        public Map outMap;

        public void Validate() {
            if( treeCount < 0 ) treeCount = 0;
            if( height < 1 ) height = 0;
            if( heightVariation > height ) heightVariation = height;
            if( trunkThickness < 0 ) trunkThickness = 0;
            if( trunkHeight < 0 ) trunkHeight = 0;
            if( foliageDensity < 0 ) foliageDensity = 0;
            if( branchDensity < 0 ) branchDensity = 0;
        }
    }


    class Tree {
        public Vector3i pos;
        public int height;
        public ForesterArgs args;

        public Tree() { }
        public Tree( int _x, int _y, int _h, int _height ) {
            pos = new Vector3i( _x, _y, _h );
            height = _height;
        }
        public Tree( Vector3i _pos, int _height ) {
            pos = _pos;
            height = _height;
        }
        public virtual void Prepare() { }
        public virtual void MakeTrunk() { }
        public virtual void MakeFoliage() { }
        public virtual void Copy( Tree other ) {
            pos = other.pos;
            height = other.height;
        }
    }

    class StickTree : Tree {
        public override void MakeTrunk() {
            base.MakeTrunk();
            for( int i = 0; i < height; i++ ) {
                args.outMap.SetBlock( pos.x, pos.y, pos.h + i, Block.Log );
            }
        }
    }

    class NormalTree : StickTree {
        public override void MakeFoliage() {
            base.MakeFoliage();
            int topH = pos.h + height - 1;
            int start = topH - 2;
            int end = topH + 2;

            int rad;
            for( int i = start; i < end; i++ ) {
                if( i > start + 1 ) {
                    rad = 1;
                } else {
                    rad = 2;
                }
                for( int xoff = -rad; xoff < rad + 1; xoff++ ) {
                    for( int yoff = -rad; yoff < rad + 1; yoff++ ) {
                        if( args.rand.NextDouble() > .618 && Math.Abs( xoff ) == Math.Abs( yoff ) && Math.Abs( xoff ) == rad ) {
                            continue;
                        }
                        args.outMap.SetBlock( pos.x + xoff, pos.y + yoff, i, Block.Leaves );
                    }
                }
            }
        }
    }

    class BambooTree : StickTree {
        public override void MakeFoliage() {
            base.MakeFoliage();
            int start = pos.h;
            int end = pos.h + height + 1;
            for( int hh = start; hh < end; hh++ ) {
                for( int i = 0; i < 2; i++ ) {
                    int xoff = args.rand.Next( 0, 2 ) * 2 - 1;
                    int yoff = args.rand.Next( 0, 2 ) * 2 - 1;
                    args.outMap.SetBlock( pos.x + xoff, pos.y + yoff, hh, Block.Leaves );
                }
            }
        }
    }

    class PalmTree : StickTree {
        public override void MakeFoliage() {
            base.MakeFoliage();
            for( int xoff = -2; xoff < 3; xoff++ ) {
                for( int yoff = -2; yoff < 3; yoff++ ) {
                    if( Math.Abs( xoff ) == Math.Abs( yoff ) ) {
                        args.outMap.SetBlock( pos.x + xoff, pos.y + yoff, pos.h + height, Block.Leaves );
                    }
                }
            }
        }
    }

    class ProceduralTree : Tree {

        public float trunkRadius { get; set; }
        public float branchSlope { get; set; }
        public float trunkHeight { get; set; }
        public float branchDensity { get; set; }
        public float[] foliageShape { get; set; }
        public Vector3i[] foliageCoords { get; set; }



        void CrossSection( Vector3i center, float radius, int diraxis, Block matidx ) {
            int rad = (int)(radius + .618);
            int secidx1 = (diraxis - 1) % 3;
            int secidx2 = (diraxis + 1) % 3;

            Vector3i coord = new Vector3i( 0, 0, 0 );
            for( int off1 = -rad; off1 < rad + 1; off1++ ) {
                for( int off2 = -rad; off2 < rad + 1; off2++ ) {
                    float thisdist = (float)Math.Sqrt( (Math.Abs( off1 ) + .5) * (Math.Abs( off1 ) + .5) +
                                                       (Math.Abs( off2 ) + .5) * (Math.Abs( off2 ) + .5) );
                    if( thisdist > radius ) continue;
                    int pri = center[diraxis];
                    int sec1 = center[secidx1] + off1;
                    int sec2 = center[secidx2] + off2;
                    coord[diraxis] = pri;
                    coord[secidx1] = sec1;
                    coord[secidx2] = sec2;
                    args.outMap.SetBlock( coord[0], coord[2], coord[1], matidx );
                }
            }
        }

        public virtual float ShapeFunc( int _h ) {
            if( args.rand.NextDouble() < 100f / (height * height) && _h < trunkHeight ) {
                return height * .12f;
            } else {
                return -1;
            }
        }

        void FoliageCluster( Vector3i center ) {
            int hoff = center.h;
            foreach( float i in foliageShape ) {
                CrossSection( new Vector3i( center.x, center.y, hoff ), i, 1, Block.Leaves );
                hoff++;
            }
        }

        bool TaperedLimb( Vector3i start, Vector3i end, float startSize, float endSize ) {
            Vector3i delta = end - start;

            int primidx = delta.GetLargestComponent();
            int maxdist = Math.Abs( delta[primidx] );
            if( maxdist == 0 ) return false;

            int secidx1 = (primidx - 1) % 3;
            int secidx2 = (primidx + 1) % 3;

            int primsign = (maxdist > 0 ? 1 : -1);

            int secdelta1 = delta[secidx1];
            float secfac1 = secdelta1 / delta[primidx];
            int secdelta2 = delta[secidx2];
            float secfac2 = secdelta2 / delta[primidx];

            Vector3i coord = new Vector3i();
            int endoffset = delta[primidx] + primsign;

            for( int primoffset = 0; primsign < endoffset; primsign += primsign ) {
                int primloc = start[primidx] + primoffset;
                int secloc1 = (int)(start[secidx1] + primoffset * secfac1);
                int secloc2 = (int)(start[secidx2] + primoffset * secfac2);
                coord[primidx] = primloc;
                coord[secidx1] = secloc1;
                coord[secidx2] = secloc2;
                float radius = endSize + (startSize - endSize) * Math.Abs( delta[primidx] - primoffset ) / (float)maxdist;

                CrossSection( coord, radius, primidx, Block.Log );
            }
            return true;
        }

        public override void MakeFoliage() {
            base.MakeFoliage();
            foreach( Vector3i coord in foliageCoords ) {
                FoliageCluster( coord );
            }
            foreach( Vector3i coord in foliageCoords ) {
                args.outMap.SetBlock( coord, Block.Log );
            }
        }

        void MakeBranches() {
            int topy = pos[1] + (int)(trunkHeight + .5);
            float endrad = trunkRadius * (1 - trunkHeight / (float)height);
            if( endrad < 1 ) endrad = 1;

            foreach( Vector3i coord in foliageCoords ) {
                float dist = (float)Math.Sqrt( (coord.x - pos.x) * (coord.x - pos.x) +
                                               (coord.y - pos.y) * (coord.y - pos.y) );
                float ydist = coord[1] - pos[1];
                float value = (branchDensity * 220 * height) /
                              ((ydist + dist) * (ydist + dist) * (ydist + dist));

                if( value < args.rand.NextDouble() ) continue;

                int posy = coord[1];
                float slope = (float)(branchSlope + (.5 - args.rand.NextDouble()) * .16);

                float branchy, basesize;
                if( coord[1] - dist * slope > topy ) {
                    float threshold = 1 / (float)height;
                    if( args.rand.NextDouble() < threshold ) continue;
                    branchy = topy;
                    basesize = endrad;
                } else {
                    branchy = posy - dist * slope;
                    basesize = endrad + (trunkRadius - endrad) *
                               (topy - branchy) / trunkHeight;
                }

                float startsize = (float)(basesize * (1 + args.rand.NextDouble()) *
                                          .618 * Math.Pow( dist / (float)height, .618 ));
                float rndr = (float)(Math.Sqrt( args.rand.NextDouble() ) * basesize * .618);
                float rndang = (float)(args.rand.NextDouble() * 2 * Math.PI);
                int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                int rndy = (int)(rndr * Math.Cos( rndang ) + .5);
                Vector3i startcoord = new Vector3i {
                    x = pos.x + rndx,
                    y = pos.y + rndy,
                    h = (int)branchy
                };
                if( startsize < 1 ) startsize = 1;
                float endsize = 1;
                TaperedLimb( startcoord, coord, startsize, endsize );
            }
        }

        struct RootBase {
            public int x, y;
            public float radius;
        }

        void MakeRoots( RootBase[] rootbases ) {
            foreach( Vector3i coord in foliageCoords ) {
                float dist = (float)Math.Sqrt( (coord[0] - pos[0]) * (coord[0] - pos[0]) +
                                               (coord[2] - pos[2]) * (coord[2] - pos[2]) );
                float ydist = coord[1] - pos[1];
                float value = (branchDensity * 220 * height) /
                              ((ydist + dist) * (ydist + dist) * (ydist + dist));
                if( value < args.rand.NextDouble() ) continue;

                RootBase rootbase = rootbases[args.rand.Next( 0, rootbases.Length )];
                int rootx = rootbase.x;
                int rootz = rootbase.y;
                float rootbaseradius = rootbase.radius;

                float rndr = (float)(Math.Sqrt( args.rand.NextDouble() ) * rootbaseradius * .618);
                float rndang = (float)(args.rand.NextDouble() * 2 * Math.PI);
                int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                int rndz = (int)(rndr * Math.Cos( rndang ) + .5);
                int rndy = (int)(args.rand.NextDouble() * rootbaseradius * .5);
                Vector3i startcoord = new Vector3i {
                    x = rootx + rndx,
                    h = pos[1] + rndy,
                    y = rootz + rndz
                };
                Vector3f offset = new Vector3f( startcoord - coord );

                if( args.shape == Forester.TreeShape.Mangrove ) {
                    offset = offset * 1.618f - 1.5f;
                }

                Vector3i endcoord = startcoord + new Vector3i( offset );
                float rootstartsize = (float)(rootbaseradius * .618 * Math.Abs( offset[1] ) / (height * .618));

                if( rootstartsize < 1 ) rootstartsize = 1;
                float endsize = 1;

                if( args.rootMode == Forester.RootMode.ToStone ||
                    args.rootMode == Forester.RootMode.Hanging ) {
                    float offlength = offset.GetLength();
                    if( offlength < 1 ) continue;
                    float rootmid = endsize;
                    Vector3f vec = offset / offlength;

                    int searchIndex = 1;
                    if( args.rootMode == Forester.RootMode.ToStone ) {
                        searchIndex = 1;
                    } else if( args.rootMode == Forester.RootMode.Hanging ) {
                        searchIndex = 0;
                    }

                    int startdist = (int)(args.rand.NextDouble() * 6 * Math.Sqrt( rootstartsize ) + 2.8);
                    Vector3i searchstart = new Vector3i( startcoord + vec * startdist );

                    dist = startdist + Forester.DistanceToBlock( args.inMap, new Vector3f( searchstart ), vec, (Block)searchIndex, false );

                    if( dist < offlength ) {
                        rootmid += (rootstartsize - endsize) * (1 - dist / offlength);
                        endcoord = new Vector3i( startcoord + vec * dist );
                        if( args.rootMode == Forester.RootMode.Hanging ) {
                            float remaining_dist = offlength - dist;
                            Vector3i bottomcord = endcoord;
                            bottomcord[1] -= (int)remaining_dist;
                            TaperedLimb( endcoord, bottomcord, rootmid, endsize );
                        }
                    }
                    TaperedLimb( startcoord, endcoord, rootstartsize, rootmid );
                } else {
                    TaperedLimb( startcoord, endcoord, rootstartsize, endsize );
                }
            }
        }

        public override void MakeTrunk() {
            int starty = pos[1];
            int midy = (int)(pos[1] + trunkHeight * .382);
            int topy = (int)(pos[1] + trunkHeight * .5);

            int x = pos[0];
            int z = pos[2];
            float midrad = trunkRadius * .8f;
            float endrad = trunkRadius * (1 - trunkHeight / (float)height);

            if( endrad < 1 ) endrad = 1;
            if( midrad < endrad ) midrad = endrad;

            float startrad;
            List<RootBase> rootbases = new List<RootBase>();
            if( args.rootButtresses || args.shape == Forester.TreeShape.Mangrove ) {
                startrad = trunkRadius * .8f;
                float buttress_radius = trunkRadius * .382f;
                float posradius = trunkRadius;
                if( args.shape == Forester.TreeShape.Mangrove ) {
                    posradius *= 2.618f;
                }
                int num_of_buttresss = (int)(Math.Sqrt( trunkRadius ) + 3.5);
                for( int i = 0; i < num_of_buttresss; i++ ) {
                    float rndang = (float)(args.rand.NextDouble() * 2 * Math.PI);
                    float thisposradius = (float)(posradius * (.9 + args.rand.NextDouble() * .2));
                    int thisx = x + (int)(thisposradius * Math.Sin( rndang ));
                    int thisz = z + (int)(thisposradius * Math.Cos( rndang ));

                    float thisbuttressradius = (float)(buttress_radius * (.618 + args.rand.NextDouble()));
                    if( thisbuttressradius < 1 ) thisbuttressradius = 1;

                    TaperedLimb( new Vector3i( thisx, thisz, starty ), new Vector3i( x, z, midy ),
                                 thisbuttressradius, thisbuttressradius );
                    rootbases.Add( new RootBase {
                        x = thisx,
                        y = thisz,
                        radius = thisbuttressradius
                    } );
                }
            } else {
                startrad = trunkRadius;
                rootbases.Add( new RootBase {
                    x = x,
                    y = z,
                    radius = startrad
                } );
            }
            TaperedLimb( new Vector3i( x, z, starty ), new Vector3i( x, z, midy ), startrad, midrad );
            TaperedLimb( new Vector3i( x, z, midy ), new Vector3i( x, z, topy ), midrad, endrad );
            MakeBranches();
            if( args.rootMode != Forester.RootMode.None ) {
                MakeRoots( rootbases.ToArray() );
            }
        }

        public override void Prepare() {
            base.Prepare();
            trunkRadius = (float)Math.Sqrt( height * args.trunkThickness );
            if( trunkRadius < 1 ) trunkRadius = 1;

            trunkHeight = height * .618f;
            branchDensity = (args.branchDensity / args.foliageDensity);

            int ystart = pos[1];
            int yend = (int)(pos[1] + height);
            int num_of_clusters_per_y = (int)(1.5 + Math.Pow( args.foliageDensity * height / 19f, 2 ));
            if( num_of_clusters_per_y < 1 ) num_of_clusters_per_y = 1;

            List<Vector3i> _foliageCoords = new List<Vector3i>();
            for( int y = yend; y < ystart; y-- ) {
                for( int i = 0; i < num_of_clusters_per_y; i++ ) {
                    float shapefac = ShapeFunc( y - ystart );
                    if( shapefac < 0 ) continue;
                    float r = (float)((Math.Sqrt( args.rand.NextDouble() ) + .328) * shapefac);
                    float theta = (float)(args.rand.NextDouble() * 2 * Math.PI);
                    int x = (int)(r * Math.Sin( theta )) + pos[0];
                    int z = (int)(r * Math.Cos( theta )) + pos[2];
                    _foliageCoords.Add( new Vector3i( x, z, y ) );
                }
            }
            foliageCoords = _foliageCoords.ToArray();
        }
    }

    class RoundTree : ProceduralTree {
        public override void Prepare() {
            base.Prepare();
            branchSlope = .382f;
            foliageShape = new float[] { 2, 3, 3, 2.5f, 1.6f };
            trunkRadius *= .8f;
            trunkHeight = args.trunkHeight * height;
        }

        public override float ShapeFunc( int y ) {
            float twigs = base.ShapeFunc( y );
            if( twigs >= 0 ) return twigs;

            if( y < height * (.282 + .1 * Math.Sqrt( args.rand.NextDouble() )) ) {
                return -1;
            }

            float radius = height / 2f;
            float adj = height / 2f - y;
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

    class ConeTree : ProceduralTree {
        public override void Prepare() {
            base.Prepare();
            branchSlope = .15f;
            foliageShape = new float[] { 3, 2.6f, 2, 1 };
            trunkRadius *= .618f;
            trunkHeight = height;
        }

        public override float ShapeFunc( int y ) {
            float twigs = base.ShapeFunc( y );
            if( twigs >= 0 ) return twigs;
            if( y < height * (.25 + .05 * Math.Sqrt( args.rand.NextDouble() )) ) {
                return -1;
            }
            float radius = (height - y) * .382f;
            if( radius < 0 ) radius = 0;
            return radius;
        }
    }

    class RainforestTree : ProceduralTree {
        public override void Prepare() {
            foliageShape = new float[] { 3.4f, 2.6f };
            base.Prepare();
            branchSlope = 1;
            trunkRadius *= .382f;
            trunkHeight = height * .9f;
        }

        public override float ShapeFunc( int y ) {
            if( y < height * .8 ) {
                if( args.height < height ) {
                    float twigs = base.ShapeFunc( y );
                    if( twigs >= 0 && args.rand.NextDouble() < .05 ) {
                        return twigs;
                    }
                }
                return -1;
            } else {
                float width = height * .382f;
                float topdist = (height - y) / (height * .2f);
                float dist = (float)(width * (.618 + topdist) * (.618 + args.rand.NextDouble()) * .382);
                return dist;
            }
        }
    }

    class MangroveTree : RoundTree {
        public override void Prepare() {
            base.Prepare();
            branchSlope = 1;
            trunkRadius *= .618f;
        }
        public override float ShapeFunc( int y ) {
            float val = base.ShapeFunc( y );
            if( val < 0 ) return -1;
            val *= 1.618f;
            return val;
        }
    }


    class Forester {
        public enum Operation {
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

        ForesterArgs args;

        public Forester( ForesterArgs _args ) {
            args = _args;
        }

        public static void PickRandomSurfacePoint( Map map, Random rand, out int x, out int y, out int h, Block blockType ) {
            x = 8 + rand.Next( map.widthX - 16 );
            y = 8 + rand.Next( map.widthY - 16 );
            h = map.SearchColumn( x, y, blockType, map.height - 1 );
            if( h < 0 ) {
                h = 8 + rand.Next( map.height - 16 );
            }
        }

        public static int DistanceToBlock( Map map, Vector3f coord, Vector3f vec, Block blockType, bool invert ) {
            coord = (coord + .5f) * .5f;
            int iterations = 0;
            while( map.InBounds( new Vector3i( coord ) ) ) {
                byte blockAtPos = map.GetBlock( new Vector3i( coord ) );
                if( (blockAtPos == (byte)blockType && !invert) ||
                    (blockAtPos != (byte)blockType && invert) ) {
                    break;
                } else {
                    coord += vec;
                    iterations++;
                }
            }
            return iterations;
        }

        void FindTrees( List<Tree> treelist ) {
            int treeheight = args.height;
            if( treeheight == 0 ) treeheight = 5;//auto

            for( int x = 0; x < args.inMap.widthX; x++ ) {
                for( int z = 0; z < args.inMap.widthY; z++ ) {
                    int y = args.height - 1;
                    while( true ) {
                        int foliagetop = args.inMap.SearchColumn( x, z, Block.Leaves, y );
                        if( foliagetop < 0 ) break;
                        y = foliagetop;
                        Vector3i trunktop = new Vector3i( x, z, y - 1 );
                        int height = DistanceToBlock( args.inMap, new Vector3f( trunktop ), new Vector3f( 0, 0, -1 ), Block.Log, true );
                        if( height == 0 ) {
                            y--;
                            continue;
                        }
                        y -= height;
                        if( args.height > 0 ) {
                            height = args.rand.Next( treeheight - args.heightVariation,
                                                     treeheight + args.heightVariation + 1 );
                        }
                        treelist.Add( new Tree {
                            args = args,
                            pos = new Vector3i( x, z, y ),
                            height = height
                        } );
                        y--;
                    }
                }
            }
        }

        void PlantTrees( List<Tree> treelist ) {
            int treeheight = args.height;
            if( treeheight == 0 ) treeheight = 5;//auto

            while( treelist.Count < args.treeCount ) {
                int height = args.rand.Next( treeheight - args.heightVariation,
                                             treeheight + args.heightVariation + 1 );

                
                Vector3i treeLoc = RandomTreeLoc(height);
                if( treeLoc.h < 0 ) continue;
                else treeLoc.h++;
                treelist.Add( new Tree {
                    args = args,
                    height = height,
                    pos = treeLoc
                } );
            }
        }

        Vector3i RandomTreeLoc( int height ) {
            int padding = (int)(height / 3f + 1);
            int mindim = Math.Min( args.inMap.widthX, args.inMap.widthY );
            if( padding > mindim / 2.2 ) {
                padding = (int)(mindim / 2.2);
            }
            int x = args.rand.Next( padding, args.inMap.widthX - padding );
            int z = args.rand.Next( padding, args.inMap.widthY - padding );
            int y = args.inMap.SearchColumn( x, z, args.plantOn, args.inMap.height - 1 );
            return new Vector3i( x, z, y );
        }


        void PlantRainForestTrees( List<Tree> treelist ) {
            int treeheight = args.height;
            if( treeheight == 0 ) treeheight = 5;//auto

            int existingtreenum = treelist.Count;
            int remainingtrees = args.treeCount - existingtreenum;

            int short_tree_fraction = 6;
            for( int i = 0; i < remainingtrees; i++ ) {
                float randomfac = (float)((Math.Sqrt(args.rand.NextDouble())*1.618-.618)*args.heightVariation+.5);
                
                int height;
                if( i % short_tree_fraction == 0 ) {
                    height = (int)(treeheight + randomfac);
                } else {
                    height = (int)(treeheight - randomfac);
                }
                Vector3i xyz = RandomTreeLoc( height );
                if( xyz.h < 0 ) continue;

                xyz.h++;

                bool displaced = false;
                foreach( Tree othertree in treelist ) {
                    Vector3i other_loc = othertree.pos;
                    float otherheight = othertree.height;
                    int tallx = other_loc[0];
                    int tallz = other_loc[2];
                    float dist = (float)Math.Sqrt( (tallx - xyz.x + .5) * (tallx - xyz.x + .5) +
                                                   (tallz - xyz.y + .5) * (tallz - xyz.y + .5) );
                    float threshold = (otherheight + height) * .193f;
                    if( dist < threshold ) {
                        displaced = true;
                        break;
                    }
                }
                if( displaced ) continue;
                treelist.Add( new RainforestTree() );
            }
        }

        void PlantMangroves( List<Tree> treelist ) {
            int treeheight = args.height;
            if( treeheight == 0 ) treeheight = 5;//auto

            while( treelist.Count < args.treeCount ) {
                int height = args.rand.Next( treeheight - args.heightVariation,
                                             treeheight + args.heightVariation + 1 );
                int padding = (int)(height / 3f + 1);
                int mindim = Math.Min( args.inMap.widthX, args.inMap.widthY );
                if( padding > mindim / 2.2 ) {
                    padding = (int)(mindim / 2.2);
                }
                int x = args.rand.Next( padding, args.inMap.widthX - padding );
                int z = args.rand.Next( padding, args.inMap.widthY - padding );
                int top = args.inMap.height - 1;

                int y = top - DistanceToBlock( args.inMap, new Vector3f( x, z, top ), new Vector3f( 0, -1, 0 ), Block.Air, true );
                int dist = DistanceToBlock( args.inMap, new Vector3f( x, z, y ), new Vector3f( 0, -1, 0 ), Block.Water, true );

                if( dist > height * .618 || dist == 0 ) {
                    continue;
                }

                y += (int)Math.Sqrt( height - dist ) + 2;
                treelist.Add( new Tree {
                    args = args,
                    height = height,
                    pos = new Vector3i( x, z, y )
                } );
            }
        }

        void ProcessTrees( List<Tree> treelist ) {
            TreeShape[] shape_choices;
            switch( args.shape ) {
                case TreeShape.Stickly:
                    shape_choices = new TreeShape[]{ TreeShape.Normal,
                                                     TreeShape.Bamboo,
                                                     TreeShape.Palm};
                    break;
                case TreeShape.Procedural:
                    shape_choices = new TreeShape[]{ TreeShape.Round,
                                                     TreeShape.Cone };
                    break;
                default:
                    shape_choices = new TreeShape[] { args.shape };
                    break;
            }

            for( int i = 0; i < treelist.Count; i++ ) {
                TreeShape newshape = shape_choices[args.rand.Next( 0, shape_choices.Length )];
                Tree newtree;
                switch( newshape ) {
                    case TreeShape.Normal:
                        newtree = new NormalTree();
                        break;
                    case TreeShape.Bamboo:
                        newtree = new BambooTree();
                        break;
                    case TreeShape.Palm:
                        newtree = new PalmTree();
                        break;
                    case TreeShape.Round:
                        newtree = new RoundTree();
                        break;
                    case TreeShape.Cone:
                        newtree = new ConeTree();
                        break;
                    case TreeShape.Rainforest:
                        newtree = new RainforestTree();
                        break;
                    case TreeShape.Mangrove:
                        newtree = new MangroveTree();
                        break;
                    default:
                        throw new ArgumentException();
                }
                newtree.Copy( treelist[i] );

                if( args.mapHeightLimit ) {
                    int height = newtree.height;
                    int ybase = newtree.pos[1];
                    int mapheight = args.inMap.height;
                    int foliageheight;
                    if( args.shape == TreeShape.Rainforest ) {
                        foliageheight = 2;
                    } else {
                        foliageheight = 4;
                    }
                    if( ybase + height + foliageheight > mapheight ) {
                        newtree.height = mapheight - ybase - foliageheight;
                    }
                }

                if( newtree.height < 1 ) newtree.height = 1;
                newtree.Prepare();
                treelist[i] = newtree;
            }
        }

        public void Generate() {
            List<Tree> treelist = new List<Tree>();
            args.outMap = new Map();
            args.outMap.blocks = (byte[])args.inMap.blocks.Clone();
            args.outMap.widthX = args.inMap.widthX;
            args.outMap.widthY = args.inMap.widthY;
            args.outMap.height = args.inMap.height;

            if( args.operation == Operation.Conserve ) {
                FindTrees( treelist );
            }

            if( args.treeCount > 0 && treelist.Count > args.treeCount ) {
                treelist = treelist.Take( args.treeCount ).ToList();
            }

            if( args.operation == Operation.Replant || args.operation == Operation.Add ) {
                switch( args.shape ) {
                    case TreeShape.Rainforest:
                        PlantRainForestTrees( treelist );
                        break;
                    case TreeShape.Mangrove:
                        PlantMangroves( treelist );
                        break;
                    default:
                        PlantTrees( treelist );
                        break;
                }
            }

            if( args.operation != Operation.ClearCut ) {
                ProcessTrees( treelist );
                if( args.foliage ) {
                    foreach( Tree tree in treelist ) {
                        tree.MakeFoliage();
                    }
                }
                if( args.wood ) {
                    foreach( Tree tree in treelist ) {
                        tree.MakeTrunk();
                    }
                }
            }
        }
    }
}
