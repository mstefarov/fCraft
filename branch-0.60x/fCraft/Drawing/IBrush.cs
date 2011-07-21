// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

// ReSharper disable UnusedMemberInSuper.Global
namespace fCraft.Drawing {

    public interface IBrushFactory {
        string Name { get; }
        IBrush MakeBrush( Player player, Command cmd );
    }


    public interface IBrush {
        IBrushFactory Factory { get; }
        string Description { get; }
        IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state );
    }


    public interface IBrushInstance {
        IBrush Brush { get; }
        bool HasAlternateBlock { get; }
        string InstanceDescription { get; }
        bool Begin( Player player, DrawOperation state );
        Block NextBlock( DrawOperation state );
        void End();
    }
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
*/