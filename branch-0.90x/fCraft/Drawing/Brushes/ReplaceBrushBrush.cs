// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Constructs ReplaceBrushBrush. </summary>
    public sealed class ReplaceBrushBrushFactory : IBrushFactory {
        /// <summary> Singleton instance of the ReplaceBrushBrushFactory. </summary>
        public static readonly ReplaceBrushBrushFactory Instance = new ReplaceBrushBrushFactory();


        ReplaceBrushBrushFactory() {
            Aliases = new[] { "rb" };
        }


        public string Name {
            get { return "ReplaceBrush"; }
        }

        public string[] Aliases { get; private set; }

        public string Help {
            get {
                return "ReplaceBrush brush: Replaces blocks of a given type with output of another brush. " +
                       "Usage: &H/Brush rb <Block> <BrushName>";
            }
        }


        public IBrush MakeBrush(Player player, CommandReader cmd) {
            if (player == null) throw new ArgumentNullException("player");
            if (cmd == null) throw new ArgumentNullException("cmd");

            if (!cmd.HasNext) {
                player.Message("ReplaceBrush usage: &H/Brush rb <Block> <BrushName>");
                return null;
            }

            Block block;
            if (!cmd.NextBlock(player, false, out block)) return null;

            string brushName = cmd.Next();
            if (brushName == null || !CommandManager.IsValidCommandName(brushName)) {
                player.Message("ReplaceBrush usage: &H/Brush rb <Block> <BrushName>");
                return null;
            }
            IBrushFactory brushFactory = BrushManager.GetBrushFactory(brushName);

            if (brushFactory == null) {
                player.Message("Unrecognized brush \"{0}\"", brushName);
                return null;
            }

            IBrush newBrush = brushFactory.MakeBrush(player, cmd);
            if (newBrush == null) {
                return null;
            }

            return new ReplaceBrushBrush(block, newBrush);
        }


        public IBrush MakeDefault() {
            // There is no default for this brush: parameters always required.
            return null;
        }
    }


    /// <summary> Brush that replaces all blocks of the given type with output of a brush. </summary>
    public sealed class ReplaceBrushBrush : IBrush {
        public int AlternateBlocks {
            get { return 1; }
        }

        public Block Block { get; private set; }

        public IBrushFactory Factory {
            get { return ReplaceBrushBrushFactory.Instance; }
        }

        public IBrush Replacement { get; private set; }

        public string Description {
            get {
                return String.Format("{0}({1} -> {2})",
                                     Factory.Name,
                                     Block,
                                     Replacement.Description);
            }
        }


        public ReplaceBrushBrush(Block block, [NotNull] IBrush replacement) {
            Block = block;
            Replacement = replacement;
        }


        public bool Begin(Player player, DrawOperation op) {
            if (player == null) throw new ArgumentNullException("player");
            if (op == null) throw new ArgumentNullException("op");
            op.Context |= BlockChangeContext.Replaced;
            return Replacement.Begin(player, op);
        }


        public Block NextBlock(DrawOperation op) {
            if (op == null) throw new ArgumentNullException("op");
            Block block = op.Map.GetBlock(op.Coords);
            if (block == Block) {
                return Replacement.NextBlock(op);
            }
            return Block.None;
        }


        public void End() {
            Replacement.End();
        }


        public IBrush Clone() {
            return new ReplaceBrushBrush(Block, Replacement.Clone());
        }
    }
}
