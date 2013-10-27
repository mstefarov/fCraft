using System;
using fCraft.Drawing;
using JetBrains.Annotations;

namespace fCraft.GUI {
    internal static class DrawImageCommand {
        // New /drawimage implementation contributed by Matvei Stefarov <me@matvei.org>
        public static readonly CommandDescriptor CdDrawImage = new CommandDescriptor {
            Name = "DrawImage",
            Aliases = new[] {"DrawImg", "ImgDraw", "ImgPrint"},
            Category = CommandCategory.Building,
            Permissions = new[] {Permission.DrawAdvanced},
            Usage = "/DrawImage SomeWebsite.com/picture.png [Palette]",
            Help = "Downloads and draws an image, using minecraft blocks. " +
                   "First mark specifies the origin (corner) block of the image. " +
                   "Second mark specifies direction (from origin block) in which image should be drawn. " +
                   "Optionally, a block palette name can be specified: " +
                   "Layered (default), Light, Dark, Gray, DarkGray, LayeredGray, or BW (black and white). " +
                   "If your image is from imgur.com, simply type '++' followed by the image code. " +
                   "Example: &H/DrawImage ++kbFRo.png&S",
            Handler = DrawImageHandler
        };

        static void DrawImageHandler( [NotNull] Player player, [NotNull] CommandReader cmd ) {
            ImageDrawOperation op = new ImageDrawOperation( player );
            if( !op.ReadParams( cmd ) ) {
                CdDrawImage.PrintUsage( player );
                return;
            }
            player.Message( "DrawImage: Click 2 blocks or use &H/Mark&S to set direction." );
            player.SelectionStart( 2, DrawImageCallback, op, Permission.DrawAdvanced );
        }

        static void DrawImageCallback( [NotNull] Player player, [NotNull] Vector3I[] marks, [NotNull] object tag ) {
            ImageDrawOperation op = (ImageDrawOperation)tag;
            player.Message( "&HDrawImage: Downloading {0}", op.ImageUrl );
            try {
                op.Prepare( marks );
                if( !player.CanDraw( op.BlocksTotalEstimate ) ) {
                    player.Message(
                        "DrawImage: You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                        player.Info.Rank.DrawLimit,
                        op.BlocksTotalEstimate );
                    return;
                }
                op.Begin();
            } catch( ArgumentException ex ) {
                player.Message( "&WDrawImage: Error setting up: " + ex.Message );
            } catch( Exception ex ) {
                Logger.Log( LogType.Warning,
                            "{0}: Error downloading image from {1}: {2}",
                            op.Description,
                            op.ImageUrl,
                            ex );
                player.Message( "&WDrawImage: Error downloading: " + ex.Message );
            }
        }
    }
}
