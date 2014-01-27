// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    /// <summary> Draw operation that performs a 2D flood fill. 
    /// Uses player's position to determine plane of filling. </summary>
    public sealed class Fill2DDrawOperation : DrawOperation {
        int maxFillExtent;

        public override string Name {
            get { return "Fill2D"; }
        }

        public override int ExpectedMarks {
            get { return 1; }
        }

        public override string Description {
            get {
                if( SourceBlock == Block.None ) {
                    return String.Format( "{0}({1})",
                                          Name,
                                          Brush.InstanceDescription );
                } else {
                    return String.Format( "{0}({1} @{2} -> {3})",
                                          Name, SourceBlock, Axis, Brush.InstanceDescription );
                }
            }
        }

        public Block SourceBlock { get; private set; }
        public Axis Axis { get; private set; }
        public Vector3I Origin { get; private set; }

        public Fill2DDrawOperation( Player player )
            : base( player ) {
            SourceBlock = Block.None;
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            if( marks.Length < 1 ) throw new ArgumentException( "At least one mark needed.", "marks" );

            Marks = marks;
            Origin = marks[0];
            SourceBlock = Map.GetBlock( Origin );

            Vector3I playerCoords = Player.Position.ToBlockCoords();
            Vector3I lookVector = (Origin - playerCoords);
            Axis = lookVector.LongestAxis;

            Vector3I maxDelta;

            maxFillExtent = Player.Info.Rank.FillLimit;
            if( maxFillExtent < 1 || maxFillExtent > 2048 ) maxFillExtent = 2048;

            switch( Axis ) {
                case Axis.X:
                    maxDelta = new Vector3I( 0, maxFillExtent, maxFillExtent );
                    coordEnumerator = BlockEnumeratorX().GetEnumerator();
                    break;
                case Axis.Y:
                    maxDelta = new Vector3I( maxFillExtent, 0, maxFillExtent );
                    coordEnumerator = BlockEnumeratorY().GetEnumerator();
                    break;
                default: // Z
                    maxDelta = new Vector3I( maxFillExtent, maxFillExtent, 0 );
                    coordEnumerator = BlockEnumeratorZ().GetEnumerator();
                    break;
            }

            Bounds = new BoundingBox( Origin - maxDelta, Origin + maxDelta );

            // Clip bounds to the map, used to limit fill extent
            Bounds = Bounds.GetIntersection( Map.Bounds );

            // Set everything up for filling
            Coords = Origin;

            StartTime = DateTime.UtcNow;
            Context = BlockChangeContext.Drawn | BlockChangeContext.Filled;
            BlocksTotalEstimate = Bounds.Volume;

            if( Brush == null ) throw new NullReferenceException( Name + ": Brush not set" );
            return Brush.Begin( Player, this );
        }


        // fields to accommodate non-standard brushes (which require caching)
        bool nonStandardBrush;
        HashSet<Vector3I> allCoords;

        public override bool Begin() {
            if( !RaiseBeginningEvent( this ) ) return false;
            UndoState = Player.DrawBegin( this );
            StartTime = DateTime.UtcNow;

            if( !(Brush is NormalBrush) ) {
                // for nonstandard brushes, cache all coordinates up front
                nonStandardBrush = true;

                // Generate a list if all coordinates
                allCoords = new HashSet<Vector3I>();
                while( coordEnumerator.MoveNext() ) {
                    allCoords.Add( coordEnumerator.Current );
                }
                coordEnumerator.Dispose();

                // Replace our F2D enumerator with a HashSet enumerator
                coordEnumerator = allCoords.GetEnumerator();
            }

            HasBegun = true;
            Map.QueueDrawOp( this );
            RaiseBeganEvent( this );
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator.MoveNext() ) {
                Coords = coordEnumerator.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }


        bool CanPlace( Vector3I coords ) {
            if( nonStandardBrush && allCoords.Contains( coords ) ) {
                return false;
            }
            return (Map.GetBlock( coords ) == SourceBlock) &&
                   (Player.CanPlace( Map, coords, Brush.NextBlock( this ), Context ) == CanPlaceResult.Allowed);
        }


        IEnumerable<Vector3I> BlockEnumeratorX() {
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push( Origin );

            while( stack.Count > 0 ) {
                Vector3I coords = stack.Pop();
                while( coords.Y >= Bounds.YMin && CanPlace( coords ) ) coords.Y--;
                coords.Y++;
                bool spanLeft = false;
                bool spanRight = false;
                while( coords.Y <= Bounds.YMax && CanPlace( coords ) ) {
                    yield return coords;

                    if( coords.Z > Bounds.ZMin ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X, coords.Y, coords.Z - 1 ) );
                        if( !spanLeft && canPlace ) {
                            stack.Push( new Vector3I( coords.X, coords.Y, coords.Z - 1 ) );
                            spanLeft = true;
                        } else if( spanLeft && !canPlace ) {
                            spanLeft = false;
                        }
                    }

                    if( coords.Z < Bounds.ZMax ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X, coords.Y, coords.Z + 1 ) );
                        if( !spanRight && canPlace ) {
                            stack.Push( new Vector3I( coords.X, coords.Y, coords.Z + 1 ) );
                            spanRight = true;
                        } else if( spanRight && !canPlace ) {
                            spanRight = false;
                        }
                    }
                    coords.Y++;
                }
            }
        }


        IEnumerable<Vector3I> BlockEnumeratorY() {
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push( Origin );

            while( stack.Count > 0 ) {
                Vector3I coords = stack.Pop();
                while( coords.Z >= Bounds.ZMin && CanPlace( coords ) ) coords.Z--;
                coords.Z++;
                bool spanLeft = false;
                bool spanRight = false;
                while( coords.Z <= Bounds.ZMax && CanPlace( coords ) ) {
                    yield return coords;

                    if( coords.X > Bounds.XMin ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X -1, coords.Y, coords.Z ) );
                        if( !spanLeft && canPlace ) {
                            stack.Push( new Vector3I( coords.X - 1, coords.Y, coords.Z ) );
                            spanLeft = true;
                        } else if( spanLeft && !canPlace ) {
                            spanLeft = false;
                        }
                    }

                    if( coords.X < Bounds.XMax ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X + 1, coords.Y, coords.Z ) );
                        if( !spanRight && canPlace ) {
                            stack.Push( new Vector3I( coords.X + 1, coords.Y, coords.Z ) );
                            spanRight = true;
                        } else if( spanRight && !canPlace ) {
                            spanRight = false;
                        }
                    }
                    coords.Z++;
                }
            }
        }


        IEnumerable<Vector3I> BlockEnumeratorZ() {
            Stack<Vector3I> stack = new Stack<Vector3I>();
            stack.Push( Origin );

            while( stack.Count > 0 ) {
                Vector3I coords = stack.Pop();
                while( coords.Y >= Bounds.YMin && CanPlace( coords ) ) coords.Y--;
                coords.Y++;
                bool spanLeft = false;
                bool spanRight = false;
                while( coords.Y <= Bounds.YMax && CanPlace( coords ) ) {
                    yield return coords;

                    if( coords.X > Bounds.XMin ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X - 1, coords.Y, coords.Z ) );
                        if( !spanLeft && canPlace ) {
                            stack.Push( new Vector3I( coords.X - 1, coords.Y, coords.Z ) );
                            spanLeft = true;
                        } else if( spanLeft && !canPlace ) {
                            spanLeft = false;
                        }
                    }

                    if( coords.X < Bounds.XMax ) {
                        bool canPlace = CanPlace( new Vector3I( coords.X + 1, coords.Y, coords.Z ) );
                        if( !spanRight && canPlace ) {
                            stack.Push( new Vector3I( coords.X + 1, coords.Y, coords.Z ) );
                            spanRight = true;
                        } else if( spanRight && !canPlace ) {
                            spanRight = false;
                        }
                    }
                    coords.Y++;
                }
            }
        }
    }
}