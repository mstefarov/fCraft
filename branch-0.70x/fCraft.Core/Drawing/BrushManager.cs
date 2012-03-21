// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Class that manages available brushes, and provides a way to look them up by name or alias. </summary>
    public static class BrushManager {
        static readonly Dictionary<string, IBrushFactory> BrushFactories = new Dictionary<string, IBrushFactory>();
        static readonly Dictionary<string, IBrushFactory> BrushAliases = new Dictionary<string, IBrushFactory>();

        static readonly CommandDescriptor CdBrush = new CommandDescriptor {
            Name = "Brush",
            Category = CommandCategory.Building,
            Permissions = new[] { Permission.Draw, Permission.DrawAdvanced },
            Help = "Gets or sets the current brush. Available brushes are: ",
            HelpSections = new Dictionary<string, string>(), // filled by RegisterBrush
            Handler = BrushHandler
        };


        static void BrushHandler( Player player, CommandReader cmd ) {
            string brushName = cmd.Next();
            if( brushName == null ) {
                player.Message( player.Brush.Description );
            } else {
                IBrushFactory brushFactory = GetBrushFactory( brushName );
                if( brushFactory == null ) {
                    player.Message( "Unrecognized brush \"{0}\"", brushName );
                } else {
                    IBrush newBrush = brushFactory.MakeBrush( player, cmd );
                    if( newBrush != null ) {
                        player.Brush = newBrush;
                        player.Message( "Brush set to {0}", player.Brush.Description );
                    }
                }
            }
        }


        internal static void Init() {
            CommandManager.RegisterCommand( CdBrush );
            RegisterBrush( NormalBrushFactory.Instance );
            RegisterBrush( CheckeredBrushFactory.Instance );
            RegisterBrush( RandomBrushFactory.Instance );
            RegisterBrush( RainbowBrush.Instance );
            RegisterBrush( CloudyBrushFactory.Instance );
            RegisterBrush( MarbledBrushFactory.Instance );
            RegisterBrush( ReplaceBrushFactory.Instance );
            RegisterBrush( ReplaceNotBrushFactory.Instance );
            RegisterBrush( ReplaceBrushBrushFactory.Instance );
        }


        public static void RegisterBrush( [NotNull] IBrushFactory factory ) {
            if( factory == null ) throw new ArgumentNullException( "factory" );
            string helpString = String.Format( "&S{0} brush: {1}",
                                               factory.Name, factory.Help );
            string lowerName = factory.Name.ToLower();
            BrushFactories.Add( lowerName, factory );
            if( factory.Aliases != null ) {
                helpString += " Aliases: &H" + factory.Aliases.JoinToString();
                foreach( string alias in factory.Aliases ) {
                    BrushAliases.Add( alias.ToLower(), factory );
                }
            }
            CdBrush.HelpSections.Add( lowerName, helpString );
            CdBrush.Help += factory.Name + " ";
        }


        [CanBeNull]
        public static IBrushFactory GetBrushFactory( [NotNull] string brushName ) {
            if( brushName == null ) throw new ArgumentNullException( "brushName" );
            IBrushFactory factory;
            string lowerName = brushName.ToLower();
            if( BrushFactories.TryGetValue( lowerName, out factory ) ||
                BrushAliases.TryGetValue( lowerName, out factory ) ) {
                return factory;
            } else {
                return null;
            }
        }
    }
}