// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    /// <summary> Brush that creates a diagonal rainbow pattern, using
    /// Red, Orange, Yellow, Green, Aqua, Blue, and Violet blocks. </summary>
    public sealed class RainbowBrush : IBrushFactory, IBrush, IBrushInstance {
        public static readonly RainbowBrush Instance = new RainbowBrush();

        RainbowBrush() { }

        public string Name {
            get { return "Rainbow"; }
        }

        public int AlternateBlocks {
            get { return 1; }
        }

        public string[] Aliases {
            get { return null; }
        }

        const string HelpString = "Rainbow brush: Creates a diagonal 7-color rainbow pattern.";
        public string Help {
            get { return HelpString; }
        }


        public string Description {
            get { return Name; }
        }

        public IBrushFactory Factory {
            get { return this; }
        }


        public IBrush MakeBrush( Player player, CommandReader cmd ) {
            return this;
        }


        public IBrushInstance MakeInstance( Player player, CommandReader cmd, DrawOperation state ) {
            return this;
        }

        static readonly Block[] Rainbow = {
            Block.Red,
            Block.Orange,
            Block.Yellow,
            Block.Green,
            Block.Aqua,
            Block.Blue,
            Block.Violet
        };

        public string InstanceDescription {
            get { return "Rainbow"; }
        }

        public IBrush Brush {
            get { return Instance; }
        }

        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            return Rainbow[(state.Coords.X + state.Coords.Y + state.Coords.Z) % 7];
        }


        public void End() { }
    }
}