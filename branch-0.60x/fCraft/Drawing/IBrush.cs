// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {

    public interface IBrush {
        string Name { get; }
        string Description { get; }
        IBrush MakeBrush( Player player, Command args, DrawOperationState op );
        Block NextBlock( DrawOperationState op );
    }



/*


    public class StripedDrawBrush : IDrawBrush {
        public string Name {
            get { return "Striped"; }
        }

        public readonly RotationAxis Axis;

        public StripedDrawBrush( RotationAxis axis ) {
            Axis = axis;
        }

        public Block NextBlock( DrawOperation op ) {
            int number;

            switch( Axis ) {
                case RotationAxis.X:
                    number = op.CurrentCoords.X;
                    break;
                case RotationAxis.Y:
                    number = op.CurrentCoords.Y;
                    break;
                case RotationAxis.Z:
                    number = op.CurrentCoords.Z;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return op.UserBlocks[number & 1];
        }

        public object Clone() {
            return new StripedDrawBrush( Axis );
        }
    }


    public class RandomDrawBrush : IDrawBrush {
        public string Name {
            get { return "Random"; }
        }

        Random rand = new Random();

        public Block NextBlock( DrawOperation op ) {
            return op.UserBlocks[rand.Next( op.UserBlocks.Length )];
        }

        public object Clone() { return this; }
    }*/
}
