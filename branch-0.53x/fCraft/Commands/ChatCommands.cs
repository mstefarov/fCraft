// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    static class ChatCommands {

        public static void Init() {
            CommandManager.RegisterCommand( cdSay );
            CommandManager.RegisterCommand( cdStaffChat );

            CommandManager.RegisterCommand( cdIgnore );
            CommandManager.RegisterCommand( cdUnignore );

            CommandManager.RegisterCommand( cdMe );

            CommandManager.RegisterCommand( cdRoll );

            CommandManager.RegisterCommand( cdDeafen );

            CommandManager.RegisterCommand( cdClear );
        }


        #region Say, StaffChat

        static readonly CommandDescriptor cdClear = new CommandDescriptor {
            Name = "clear",
            Category = CommandCategory.Chat,
            Help = "Clears the chat screen.",
            Handler = Clear
        };

        static void Clear( Player player, Command cmd ) {
            for( int i = 0; i < 20; i++ ) {
                player.Message( "" );
            }
        }


        static readonly CommandDescriptor cdSay = new CommandDescriptor {
            Name = "say",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MutedMessage();
                return;
            }

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( player.Can( Permission.UseColorCodes ) && msg.Contains( "%" ) ) {
                    msg = Color.ReplacePercentCodes( msg );
                }
                if( msg != null && msg.Trim().Length > 0 ) {
                    player.Info.LinesWritten++;
                    Server.SendToAllExceptIgnored( player, "&Y{0}", null, msg.Trim() );
                    IRC.SendAction( String.Format( "&Y{0}", msg.Trim() ) );
                } else {
                    cdSay.PrintUsage( player );
                }
            } else {
                player.NoAccessMessage( Permission.Say );
            }
        }



        static readonly CommandDescriptor cdStaffChat = new CommandDescriptor {
            Name = "staff",
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Usage = "/staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffChat
        };

        internal static void StaffChat( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MutedMessage();
                return;
            }

            if( DateTime.UtcNow < player.Info.MutedUntil ) {
                player.Message( "You are muted for another {0:0} seconds.",
                                player.Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                return;
            }


            Player[] plist = Server.PlayerList;

            if( plist.Length > 0 ) player.Info.LinesWritten++;

            string message = cmd.NextAll();
            if( message != null && message.Trim().Length > 0 ) {
                message = message.Trim();
                if( player.Can( Permission.UseColorCodes ) && message.Contains( "%" ) ) {
                    message = Color.ReplacePercentCodes( message );
                }
                for( int i = 0; i < plist.Length; i++ ) {
                    if( (plist[i].Can( Permission.ReadStaffChat ) || plist[i] == player) && !plist[i].IsIgnoring( player.Info ) ) {
                        plist[i].Message( "{0}(staff){1}{0}: {2}", Color.PM, player.GetClassyName(), message );
                    }
                }
            }
        }

        #endregion



        #region Ignore / Unignore

        static readonly CommandDescriptor cdIgnore = new CommandDescriptor {
            Name = "ignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/ignore [PlayerName]",
            Help = "Temporarily blocks the other player from messaging you. " +
                   "If no player name is given, lists all ignored players.",
            Handler = Ignore
        };

        internal static void Ignore( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                PlayerInfo targetInfo;
                if( !PlayerDB.FindPlayerInfo( name, out targetInfo ) ) {
                    PlayerInfo[] infos = PlayerDB.FindPlayers( name );
                    if( infos.Length == 1 ) {
                        targetInfo = infos[0];
                    } else if( infos.Length > 1 ) {
                        player.ManyMatchesMessage( "player", infos );
                        return;
                    } else {
                        player.NoPlayerMessage( name );
                        return;
                    }
                } else if( targetInfo == null ) {
                    player.NoPlayerMessage( name );
                    return;
                }
                if( player.Ignore( targetInfo ) ) {
                    player.MessageNow( "You are now ignoring {0}", targetInfo.GetClassyName() );
                } else {
                    player.MessageNow( "You are already ignoring {0}", targetInfo.GetClassyName() );
                }

            } else {
                PlayerInfo[] ignoreList = player.GetIgnoreList();
                if( ignoreList.Length > 0 ) {
                    player.MessageNow( "Ignored players: {0}", ignoreList.JoinToClassyString() );
                } else {
                    player.MessageNow( "You are not currently ignoring anyone." );
                }
                return;
            }
        }


        static readonly CommandDescriptor cdUnignore = new CommandDescriptor {
            Name = "unignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/unignore PlayerName",
            Help = "Unblocks the other player from messaging you.",
            Handler = Unignore
        };

        internal static void Unignore( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                PlayerInfo targetInfo;
                if( !PlayerDB.FindPlayerInfo( name, out targetInfo ) ) {
                    PlayerInfo[] infos = PlayerDB.FindPlayers( name );
                    if( infos.Length == 1 ) {
                        targetInfo = infos[0];
                    } else if( infos.Length > 1 ) {
                        player.ManyMatchesMessage( "player", infos );
                        return;
                    } else {
                        player.NoPlayerMessage( name );
                        return;
                    }
                } else if( targetInfo == null ) {
                    player.NoPlayerMessage( name );
                    return;
                }
                if( player.Unignore( targetInfo ) ) {
                    player.MessageNow( "You are no longer ignoring {0}", targetInfo.GetClassyName() );
                } else {
                    player.MessageNow( "You are not currently ignoring {0}", targetInfo.GetClassyName() );
                }
            } else {
                PlayerInfo[] ignoreList = player.GetIgnoreList();
                if( ignoreList.Length > 0 ) {
                    player.MessageNow( "Ignored players: {0}", ignoreList.JoinToClassyString() );
                } else {
                    player.MessageNow( "You are not currently ignoring anyone." );
                }
                return;
            }
        }

        #endregion


        #region Me

        static readonly CommandDescriptor cdMe = new CommandDescriptor {
            Name = "me",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Usage = "/me Message",
            Help = "Sends IRC-style action message prefixed with your name.",
            Handler = Me
        };

        internal static void Me( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MutedMessage();
                return;
            }

            string msg = cmd.NextAll().Trim();
            if( msg.Length > 0 ) {
                player.Info.LinesWritten++;
                if( player.Can( Permission.UseColorCodes ) && msg.Contains( "%" ) ) {
                    msg = Color.ReplacePercentCodes( msg );
                }
                string message = String.Format( "{0}*{1} {2}", Color.Me, player.Name, msg );
                Server.SendToAll( message );
                IRC.SendChannelMessage( message );
            }
        }

        #endregion


        #region Roll

        static readonly CommandDescriptor cdRoll = new CommandDescriptor {
            Name = "roll",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat },
            Help = "Gives random number between 1 and 100.&N" +
                   "&H/roll MaxNumber&N" +
                   "Gives number between 1 and max.&N" +
                   "&H/roll MinNumber MaxNumber&N" +
                   "Gives number between min and max.",
            Handler = Roll
        };

        internal static void Roll( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MutedMessage();
                return;
            }

            Random rand = new Random();
            int min = 1, max = 100, t1;
            if( cmd.NextInt( out t1 ) ) {
                int t2;
                if( cmd.NextInt( out t2 ) ) {
                    if( t2 < t1 ) {
                        min = t2;
                        max = t1;
                    } else {
                        min = t1;
                        max = t2;
                    }
                } else if( t1 >= 1 ) {
                    max = t1;
                }
            }
            int num = rand.Next( min, max + 1 );
            Server.SendToAll( "{0}{1} rolled {2} ({3}...{4})",
                              player.GetClassyName(), Color.Silver, num, min, max );
        }

        #endregion


        #region Deafen

        static readonly CommandDescriptor cdDeafen = new CommandDescriptor {
            Name = "deafen",
            Aliases = new[] { "deaf" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Help = "Blocks all chat messages from being sent to you.",
            Handler = Deafen
        };

        internal static void Deafen( Player player, Command cmd ) {
            if( !player.IsDeaf ) {
                player.MessageNow( "Deafened mode: ON" );
                player.MessageNow( "You will not see any messages until you type &H/deafen&S again." );
                player.IsDeaf = true;
            } else {
                player.IsDeaf = false;
                player.MessageNow( "Deafened mode: OFF" );
            }
        }

        #endregion
    }
}