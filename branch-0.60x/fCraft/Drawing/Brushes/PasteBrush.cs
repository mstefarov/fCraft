// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class PasteBrushFactory : IBrushFactory {
        public static readonly PasteBrushFactory Instance = new PasteBrushFactory();

        PasteBrushFactory() {
            Aliases= new[] { "p" };
            Help = "Paste brush: Pastes previously copied blocks. "+
                   "If one or more blocktypes are given, pastes ONLY those blocks. "+
                   "If bounds of the draw command exceed pasted area, parts furthest away from the origin remain untouched.";
        }

        public string Name {
            get { return "Paste"; }
        }

        public string[] Aliases { get; private set; }

        public string Help { get; private set; }


        public IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Add( block );
            }

            if( blocks.Count == 0 ) {
                return new PasteBrush();
            } else {
                return new PasteBrush( blocks.ToArray() );
            }
        }
    }


    public sealed class PasteBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public CopyInformation CopyInfo { get; private set; }
        public BoundingBox CopyBounds { get; private set; }

        public PasteBrush() { }

        public PasteBrush( Block[] blocks ) {
            Blocks = blocks;
        }


        public PasteBrush( [NotNull] PasteBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return ReplaceBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else {
                    return String.Format( "{0}({1})",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                }
            }
        }


        public IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            Vector3I StartCoord = state.Marks[0];
            Vector3I EndCoord = new Vector3I {
                X = (short)(state.Marks[0].X + CopyInfo.Width),
                Y = (short)(state.Marks[0].Y + CopyInfo.Length),
                Z = (short)(state.Marks[0].Z + CopyInfo.Height)
            };

            CopyBounds = new BoundingBox( StartCoord, EndCoord );

            CopyInfo = player.GetCopyInformation();
            if( CopyInfo == null ) {
                player.Message( "Nothing to paste." );
                return null;
            }

            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Add( block );
            }

            if( blocks.Count > 0 ) {
                Blocks = blocks.ToArray();
            }

            return new PasteBrush( this );
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public bool HasAlternateBlock {
            get { return false; }
        }


        public string InstanceDescription {
            get { return Description; }
        }


        public bool Begin( [NotNull] Player player, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }

            CopyInfo = player.GetCopyInformation();
            if( CopyInfo == null ) {
                player.Message( "Nothing to paste." );
                return false;
            }
            /*
            if( bounds.XMin < 0 || bounds.XMax > map.Width - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( bounds.YMin < 0 || bounds.YMax > map.Length - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( bounds.ZMin < 0 || bounds.ZMax > map.Height - 1 ) {
                player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }
            */

            return true;
        }


        public Block NextBlock( [NotNull] DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            Block block = state.Map.GetBlock( state.Coords.X, state.Coords.Y, state.Coords.Z );
            for( int i = 0; i < Blocks.Length; i++ ) {

            }
            return Block.Undefined;
        }


        public void End() { }

        #endregion
    }
}