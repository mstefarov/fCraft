// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    static class ChatCommands {

        public static void Init() {
            CommandManager.RegisterCommand( CdSay );
            CommandManager.RegisterCommand( CdStaffChat );

            CommandManager.RegisterCommand( CdIgnore );
            CommandManager.RegisterCommand( CdUnignore );

            CommandManager.RegisterCommand( CdMe );

            CommandManager.RegisterCommand( CdRoll );

            CommandManager.RegisterCommand( CdDeafen );

            CommandManager.RegisterCommand( CdClear );

            CommandManager.RegisterCommand( CdTimer );
        }


        #region Timer

        static readonly TimeSpan DefaultTimerDuration = TimeSpan.FromSeconds( 4 );

        static readonly CommandDescriptor CdTimer = new CommandDescriptor {
            Name = "timer",
            Permissions = new[] { Permission.Say },
            IsConsoleSafe = true,
            Category = CommandCategory.Chat,
            Usage = "/timer [Duration Message]",
            Help = "Starts a timer with a given duration and message. " +
                   "As the timer counts down, announcements are shown globally. " +
                   "Type &H/timers&S for a list of all in-progress timers.",
            Handler = DoTimer
        };



        static void DoTimer( Player player, Command cmd ) {
            TimeSpan duration = DefaultTimerDuration;
            string name = player.Name + "'s timer";
            if( cmd.HasNext ) {
                string time = cmd.Next();
                if( time == null || !time.TryParseMiniTimespan( out duration ) ) {
                    CdTimer.PrintUsage( player );
                    return;
                }
                if( cmd.HasNext ) {
                    name = cmd.NextAll();
                }
                player.Message( "Started a {0} timer.", duration.ToMiniString() );
            }
            ChatTimer.Start( player.Info, duration, name );
        }

        #endregion


        #region Say, StaffChat


        static readonly CommandDescriptor CdSay = new CommandDescriptor {
            Name = "say",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = Say
        };

        static void Say( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll().Trim();
                if( player.Can( Permission.UseColorCodes ) && msg.Contains( "%" ) ) {
                    msg = Color.ReplacePercentCodes( msg );
                }
                if( msg.Length > 0 ) {
                    Chat.SendSay( player, msg );
                } else {
                    CdSay.PrintUsage( player );
                }
            } else {
                player.MessageNoAccess( Permission.Say );
            }
        }



        static readonly CommandDescriptor CdStaffChat = new CommandDescriptor {
            Name = "staff",
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Usage = "/staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffChat
        };

        internal static void StaffChat( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( DateTime.UtcNow < player.Info.MutedUntil ) {
                player.Message( "You are muted for another {0:0} seconds.",
                                player.Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                return;
            }

            string message = cmd.NextAll().Trim();
            if( message.Length > 0 ) {
                if( player.Can( Permission.UseColorCodes ) && message.Contains( "%" ) ) {
                    message = Color.ReplacePercentCodes( message );
                }
                Chat.SendStaff( player, message );
            }
        }

        #endregion


        #region Ignore / Unignore

        static readonly CommandDescriptor CdIgnore = new CommandDescriptor {
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
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
                if( targetInfo == null ) return;

                if( player.Ignore( targetInfo ) ) {
                    player.MessageNow( "You are now ignoring {0}", targetInfo.ClassyName );
                } else {
                    player.MessageNow( "You are already ignoring {0}", targetInfo.ClassyName );
                }

            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if( ignoreList.Length > 0 ) {
                    player.MessageNow( "Ignored players: {0}", ignoreList.JoinToClassyString() );
                } else {
                    player.MessageNow( "You are not currently ignoring anyone." );
                }
                return;
            }
        }


        static readonly CommandDescriptor CdUnignore = new CommandDescriptor {
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
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
                if( targetInfo == null ) return;

                if( player.Unignore( targetInfo ) ) {
                    player.MessageNow( "You are no longer ignoring {0}", targetInfo.ClassyName );
                } else {
                    player.MessageNow( "You are not currently ignoring {0}", targetInfo.ClassyName );
                }
            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
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

        static readonly CommandDescriptor CdMe = new CommandDescriptor {
            Name = "me",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Usage = "/me Message",
            Help = "Sends IRC-style action message prefixed with your name.",
            Handler = Me
        };

        internal static void Me( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            string msg = cmd.NextAll().Trim();
            if( msg.Length > 0 ) {
                player.Info.ProcessMessageWritten();
                if( player.Can( Permission.UseColorCodes ) && msg.Contains( "%" ) ) {
                    msg = Color.ReplacePercentCodes( msg );
                }
                Chat.SendMe( player, msg );
            }
        }

        #endregion


        #region Roll

        static readonly CommandDescriptor CdRoll = new CommandDescriptor {
            Name = "roll",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Gives random number between 1 and 100.\n" +
                   "&H/roll MaxNumber\n" +
                   "&S  Gives number between 1 and max.\n" +
                   "&H/roll MinNumber MaxNumber\n" +
                   "&S  Gives number between min and max.",
            Handler = Roll
        };

        internal static void Roll( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            Random rand = new Random();
            int n1;
            int min, max;
            if( cmd.NextInt( out n1 ) ) {
                int n2;
                if( !cmd.NextInt( out n2 ) ) {
                    n2 = 1;
                }
                min = Math.Min( n1, n2 );
                max = Math.Max( n1, n2 );
            } else {
                min = 1;
                max = 100;
            }

            int num = rand.Next( min, max + 1 );
            Server.Message( player,
                            "{0}{1} rolled {2} ({3}...{4})",
                            player.ClassyName, Color.Silver, num, min, max );
            player.Message( "{0}You rolled {1} ({2}...{3})",
                            Color.Silver, num, min, max );
        }

        #endregion


        #region Deafen

        static readonly CommandDescriptor CdDeafen = new CommandDescriptor {
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


        #region Clear

        static readonly CommandDescriptor CdClear = new CommandDescriptor {
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

        #endregion
    }
}