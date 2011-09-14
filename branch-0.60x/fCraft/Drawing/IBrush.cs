// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using JetBrains.Annotations;

// ReSharper disable UnusedMemberInSuper.Global
namespace fCraft.Drawing {

    public interface IBrushFactory {
        [NotNull]
        string Name { get; }
        
        [NotNull]
        string Help { get; }

        string[] Aliases { get; }

        IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd );
    }


    public interface IBrush {
        [NotNull]
        IBrushFactory Factory { get; }
        [NotNull]
        string Description { get; }

        IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation state );
    }


    public interface IBrushInstance {
        [NotNull]
        IBrush Brush { get; }
        bool HasAlternateBlock { get; }
        [NotNull]
        string InstanceDescription { get; }

        bool Begin( [NotNull] Player player, [NotNull] DrawOperation state );
        Block NextBlock( [NotNull] DrawOperation state );
        void End();
    }
}