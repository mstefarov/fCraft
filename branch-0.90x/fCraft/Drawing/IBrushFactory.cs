using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Class that describes a type of brush in general, and allows creating new brushes with /Brush.
    /// One instance of IBrushFactory for each type of brush is kept by the BrushManager. </summary>
    public interface IBrushFactory {
        /// <summary> Name of the brush. Should be unique. </summary>
        [NotNull]
        string Name { get; }

        /// <summary> Information printed to the player when they call "/help brush ThisBrushesName".
        /// Should include description and usage information. </summary>
        [NotNull]
        string Help { get; }

        /// <summary> List of aliases/alternate names for this brush. May be null. </summary>
        [CanBeNull]
        string[] Aliases { get; }

        /// <summary> Creates a new brush for a player, based on given parameters. </summary>
        /// <param name="player"> Player who will be using this brush.
        /// Errors and warnings about the brush creation should be communicated by messaging the player. </param>
        /// <param name="cmd"> Parameters passed to the /Brush command (after the brush name). </param>
        /// <returns> A newly-made brush, or null if there was some problem with parameters/permissions. </returns>
        [CanBeNull]
        IBrush MakeBrush( [NotNull] Player player, [NotNull] CommandReader cmd );


        /// <summary> Creates a new brush with default parameters, if possible.
        /// If it is impossible to create a default brush without any configuration, null should be returned. </summary>
        [CanBeNull]
        IBrush MakeDefault();
    }
}