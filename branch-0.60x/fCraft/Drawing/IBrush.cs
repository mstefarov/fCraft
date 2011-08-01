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