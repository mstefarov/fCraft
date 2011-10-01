// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fCraft.Events;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    public static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/baninfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );

            CommandManager.RegisterCommand( CdBanEx );

            CommandManager.RegisterCommand( CdKick );

            CommandManager.RegisterCommand( CdRank );

            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );

            CommandManager.RegisterCommand( CdSetSpawn );

            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );

            CommandManager.RegisterCommand( CdTP );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );

            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );

            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );

            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );
        }


        #region Ban / Unban

        static readonly CommandDescriptor CdBan = new CommandDescriptor {
            Name = "ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = BanHandler
        };

        static void BanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdBan.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                Player targetPlayer = target.PlayerObject;
                target.Ban( player, reason, true, true );
                WarnIfOtherPlayersOnIP( player, target, targetPlayer );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target );
                }
            }
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor {
            Name = "banip",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/banip PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIPHandler
        };

        static void BanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanIP( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    target.BanIP( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdBanAll = new CommandDescriptor {
            Name = "banall",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/banall PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAllHandler
        };

        static void BanAllHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanAll.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanAll( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    target.BanAll( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdUnban = new CommandDescriptor {
            Name = "unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanHandler
        };

        static void UnbanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdUnban.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                target.Unban( player, reason, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor {
            Name = "unbanip",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/unbanip PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIPHandler
        };

        static void UnbanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanIP( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    target.UnbanIP( player, reason, true, true );
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor {
            Name = "unbanall",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/unbanall PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAllHandler
        };

        static void UnbanAllHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanAll.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanAll( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    target.UnbanAll( player, reason, true, true );
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdBanEx = new CommandDescriptor {
            Name = "banex",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/banex +PlayerName&S or &H/banex -PlayerName",
            Help = "Adds or removes an IP-ban exemption for an account. " +
                   "Exempt accounts can log in from any IP, including banned ones.",
            Handler = BanExHandler
        };

        static void BanExHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null || playerName.Length < 2 || (playerName[0] != '-' && playerName[0] != '+') ) {
                CdBanEx.PrintUsage( player );
                return;
            }
            bool addExemption = (playerName[0] == '+');
            string targetName = playerName.Substring( 1 );
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;

            switch( target.BanStatus ) {
                case BanStatus.Banned:
                    if( addExemption ) {
                        player.Message( "Player {0}&S is currently banned. Unban before adding an exemption.",
                                        target.ClassyName );
                    } else {
                        player.Message( "Player {0}&S is already banned. There is no exemption to remove.",
                                        target.ClassyName );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption already exists for player {0}", target.ClassyName );
                    } else {
                        player.Message( "IP-Ban exemption removed for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.NotBanned;
                    }
                    break;
                case BanStatus.NotBanned:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption added for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.IPBanExempt;
                    } else {
                        player.Message( "No IP-Ban exemption exists for player {0}",
                                        target.ClassyName );
                    }
                    break;
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = KickHandler
        };

        static void KickHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: &H/kick PlayerName [Message]" );
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            string reason = cmd.NextAll();
            DateTime previousKickDate = target.Info.LastKickDate;
            string previousKickedBy = target.Info.LastKickBy;
            string previousKickReason = target.Info.LastKickReason;

            // do the kick
            try {
                Player targetPlayer = target;
                target.Kick( player, reason, LeaveReason.Kick, true, true, true );
                WarnIfOtherPlayersOnIP( player, target.Info, targetPlayer );

            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target.Info );
                }
                return;
            }

            // warn player if target has been kicked before
            if( target.Info.TimesKicked > 1 ) {
                player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                target.ClassyName, target.Info.TimesKicked - 1 );
                if( previousKickDate != DateTime.MinValue ) {
                    player.Message( "Most recent kick was {0} ago, by {1}.",
                                    DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                    previousKickedBy );
                }
                if( !String.IsNullOrEmpty( previousKickReason ) ) {
                    player.Message( "Most recent kick reason was: {0}",
                                    previousKickReason );
                }
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor {
            Name = "rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote, Permission.Demote },
            AnyPermission = true,
            IsConsoleSafe = true,
            Usage = "/rank PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = RankHandler
        };

        static void RankHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( newRankName == null ) {
                CdRank.PrintUsage( player );
                player.Message( "See &H/ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank( newRankName );
            if( newRank == null ) {
                player.MessageNoRank( newRankName );
                return;
            }

            // Parse player name
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact( name );

            if( targetInfo == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.MessageNoPlayer( name );
                    return;
                }
                if( Player.IsValidName( name ) ) {
                    if( cmd.IsConfirmed ) {
                        if( newRank > RankManager.DefaultRank ) {
                            targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                        } else {
                            targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Demoted );
                        }
                    } else {
                        player.Confirm( cmd,
                                        "Warning: Player \"{0}\" is not in the database (possible typo). Type the full name or",
                                        name );
                        return;
                    }
                } else {
                    player.Message( "Player not found. Please specify a valid name." );
                    return;
                }
            }

            try {
                targetInfo.ChangeRank( player, newRank, cmd.NextAll(), true, true, false );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        #endregion


        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor {
            Name = "hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            Handler = HideHandler
        };

        static void HideHandler( Player player, Command cmd ) {
            if( player.Info.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.Info.IsHidden = true;
            player.Message( "&8You are now hidden." );

            // to make it look like player just logged out in /info
            player.Info.LastSeen = DateTime.UtcNow;

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Server.Players.CantSee( player ).Message( "&SPlayer {0}&S left the server.", player.ClassyName );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerDisconnectedHandler( null, new PlayerDisconnectedEventArgs( player, LeaveReason.ClientQuit, true ) );
                }
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is now hidden.", player.ClassyName );

            Player.RaisePlayerHideChangedEvent( player );
        }



        static readonly CommandDescriptor CdUnhide = new CommandDescriptor {
            Name = "unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/unhide [silent]",
            Help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = UnhideHandler
        };

        static void UnhideHandler( Player player, Command cmd ) {
            if( !player.Info.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            bool silent = cmd.HasNext;

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is no longer hidden.",
                                                     player.ClassyName );
            player.Message( "&8You are no longer hidden." );
            player.Info.IsHidden = false;
            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Server.Players.CantSee( player ).MessageAlt( Server.MakePlayerConnectedMessage( player, false, player.World ) );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerReadyHandler( null, new PlayerConnectedEventArgs( player, player.World ) );
                }
            }

            Player.RaisePlayerHideChangedEvent( player );
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "setspawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/setspawn [PlayerName]",
            Handler = SetSpawnHandler
        };

        static void SetSpawnHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null ) {
                player.World.Map.Spawn = player.Position;
                player.TeleportTo( player.World.Map.Spawn );
                player.Send( PacketWriter.MakeAddEntity( 255, player.ListName, player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.Name );

            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = player.World.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( PacketWriter.MakeAddEntity( 255, target.ListName, player.Position ) );
                    } else {
                        player.Message( "You can only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName, true );
                    if( infos.Length > 0 ) {
                        player.Message( "You can only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( CdSetSpawn );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor {
            Name = "freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = FreezeHandler
        };

        static void FreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot freeze yourself." );
                return;
            }

            if( player.Can( Permission.Freeze, target.Info.Rank ) ) {
                target.Info.IsHidden = false;
                if( target.Info.Freeze( player.Name ) ) {
                    Server.Message( "{0}&S has been frozen by {1}",
                                      target.ClassyName, player.ClassyName );
                } else {
                    player.Message( "{0}&S is already frozen.", target.ClassyName );
                }
            } else {
                player.Message( "You can only freeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Freeze ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor {
            Name = "unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/help freeze&S for more information.",
            Handler = UnfreezeHandler
        };

        static void UnfreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( player.Can( Permission.Freeze, target.Info.Rank ) ) {
                if( target.Info.Unfreeze() ) {
                    Server.Message( "{0}&S is no longer frozen.", target.ClassyName );
                } else {
                    player.Message( "{0}&S is currently not frozen.", target.ClassyName );
                }
            } else {
                player.Message( "You can only unfreeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Freeze ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
            }
        }

        #endregion


        #region TP

        static readonly CommandDescriptor CdTP = new CommandDescriptor {
            Name = "tp",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/tp PlayerName&S or &H/tp X Y Z",
            Help = "Teleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TPHandler
        };

        static void TPHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdTP.PrintUsage( player );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, z;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.TeleportTo( new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            Z = (short)(z * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } );
                    }
                } else {
                    CdTP.PrintUsage( player );
                }

            } else {
                Player[] matches = Server.FindPlayers( player, name, true );
                if( matches.Length == 1 ) {
                    Player target = matches[0];

                    if( target.World == player.World ) {
                        player.TeleportTo( target.Position );

                    } else {
                        switch( target.World.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if( target.World.IsFull ) {
                                    player.Message( "Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    target.World.ClassyName );
                                    return;
                                }
                                player.StopSpectating();
                                player.JoinWorld( target.World, WorldChangeReason.Tp, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.ClassyName,
                                                target.World.ClassyName );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                target.World.ClassyName,
                                                target.World.AccessSecurity.MinRank.ClassyName );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.MessageManyMatches( "player", matches );

                } else {
                    // Try to guess if player typed "/tp" instead of "/join"
                    World[] worlds = WorldManager.FindWorlds( player, name );

                    if( worlds.Length == 1 ) {
                        player.StopSpectating();
                        player.ParseMessage( "/join " + name, false );
                    } else {
                        player.MessageNoPlayer( name );
                    }
                }
            }
        }

        #endregion


        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = BringHandler
        };

        static void BringHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false, true );
                if( toPlayer == null ) return;
            } else if( toPlayer.World == null ) {
                player.Message( "When used from console, /bring requires both names to be given." );
                return;
            }

            World world = toPlayer.World;

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You can only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == world ) {
                // teleport within the same world
                target.TeleportTo( toPlayer.Position );
                target.Position = toPlayer.Position;
                if( target.Info.IsFrozen ) {
                    target.Position = toPlayer.Position;
                }

            } else {
                if( world.AccessSecurity.CheckDetailed( target.Info ) == SecurityCheckResult.RankTooLow && !cmd.IsConfirmed ) {
                    player.Confirm( cmd,
                                    "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                    target.Name,
                                    world );
                    return;
                }
                // teleport to a different world
                BringPlayerToWorld( player, target, world, true );
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "wbring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/wbring PlayerName WorldName",
            Help = "Teleports a player to the given world's spawn.",
            Handler = WorldBringHandler
        };

        static void WorldBringHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, playerName, false, true );
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You can only wbring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "Player {0}&S is already in world {1}",
                                target.ClassyName, world.ClassyName );
                return;
            }

            if( world.AccessSecurity.CheckDetailed( target.Info ) == SecurityCheckResult.RankTooLow && !cmd.IsConfirmed ) {
                player.Confirm( cmd,
                                "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                target.Name,
                                world );
                return;
            }
            BringPlayerToWorld( player, target, world, true );
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "bringall",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/bringall [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "Teleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAllHandler
        };

        static void BringAllHandler( Player player, Command cmd ) {
            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.FindRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.Message( "Unknown rank: {0}", arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to &H/bring&W players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    foreach( Player worldPlayer in world.Players ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            // Remove the player him/herself
            targetPlayers.Remove( player );

            int count = 0;

            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers ) {
                if( !player.CanSee( targetPlayer ) ) continue;
                if( targetPlayer.World == player.World ) {
                    // teleport within the same world
                    targetPlayer.TeleportTo( player.Position );
                    targetPlayer.Position = player.Position;
                    if( targetPlayer.Info.IsFrozen ) {
                        targetPlayer.Position = player.Position;
                    }

                } else {
                    // teleport to a different world
                    BringPlayerToWorld( player, targetPlayer, player.World, false );
                }
                count++;
            }

            // Check if there's anyone to bring
            if( count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", count );
            }
        }



        static void BringPlayerToWorld( Player player, Player target, World world, bool overridePermissions ) {
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    target.JoinWorld( world, WorldChangeReason.Bring );
                    break;

                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;

                case SecurityCheckResult.RankTooLow:
                    if( overridePermissions ) {
                        target.StopSpectating();
                        target.JoinWorld( world, WorldChangeReason.Bring );
                    } else {
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.ClassyName,
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                    }
                    break;
                // TODO: case PermissionType.RankTooHigh:
            }
        }

        #endregion


        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = PatrolHandler
        };

        static void PatrolHandler( Player player, Command cmd ) {
            Player target = player.World.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            player.TeleportTo( target.Position );
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "specpatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrolHandler
        };

        static void SpecPatrolHandler( Player player, Command cmd ) {
            Player target = player.World.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            target.LastPatrolTime = DateTime.UtcNow;
            player.Spectate( target );
        }

        #endregion


        #region Mute / Unmute

        static readonly CommandDescriptor CdMute = new CommandDescriptor {
            Name = "mute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified length of time.",
            Usage = "/mute PlayerName Duration",
            Handler = MuteHandler
        };

        static void MuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;

            // validate command parameters
            if( targetName == null || !Player.IsValidName( targetName ) ||
                timeString == null || !timeString.TryParseMiniTimespan( out duration ) ||
                duration <= TimeSpan.Zero ) {
                CdMute.PrintUsage( player );
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            // check permissions
            if( !player.Can( Permission.Mute, target.Info.Rank ) ) {
                player.Message( "You can only mute players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Mute ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            // do the muting
            if( target.Info.Mute( player.Name, duration ) ) {
                target.Message( "You were muted by {0}&S for {1}", player.ClassyName, duration.ToMiniString() );
                Server.Message( target,
                                "&SPlayer {0}&S was muted by {1}&S for {2}",
                                target.ClassyName, player.ClassyName, duration.ToMiniString() );
                Logger.Log( "Player {0} was muted by {1} for {2}", LogType.UserActivity,
                            target.Name, player.Name, duration.ToMiniString() );
            } else {
                player.Message( "Player {0}&S is already muted by {1}&S for another {2}.",
                                target.ClassyName,
                                target.Info.MutedBy,
                                target.Info.MutedUntil.Subtract( DateTime.UtcNow ).ToMiniString() );
            }
        }


        static readonly CommandDescriptor CdUnmute = new CommandDescriptor {
            Name = "unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/unmute PlayerName",
            Handler = UnmuteHandler
        };

        static void UnmuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName != null && Player.IsValidName( targetName ) ) {

                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
                if( target == null ) return;

                if( !player.Can( Permission.Mute, target.Info.Rank ) ) {
                    player.Message( "You can only unmute players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Mute ).ClassyName );
                    player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    return;
                }

                if( target.Info.MutedUntil >= DateTime.UtcNow ) {
                    target.Info.Unmute();
                    target.Message( "You were unmuted by {0}", player.ClassyName );
                    Server.Message( target,
                                    "&SPlayer {0}&S was unmuted by {1}",
                                    target.ClassyName, player.ClassyName );
                    Logger.Log( "Player {0} was unmuted by {1}.", LogType.UserActivity,
                                target.Name, player.Name );
                } else {
                    player.Message( "Player {0}&S is not muted.", target.ClassyName );
                }

            } else {
                CdUnmute.PrintUsage( player );
            }
        }

        #endregion


        #region Spectate / Unspectate

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Handler = SpectateHandler
        };

        static void SpectateHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                PlayerInfo lastSpec = player.LastSpectatedPlayer;
                if( lastSpec != null ) {
                    Player spec = player.SpectatedPlayer;
                    if( spec != null ) {
                        player.Message( "Now spectating {0}", spec.ClassyName );
                    } else {
                        player.Message( "Last spectated {0}", lastSpec.ClassyName );
                    }
                } else {
                    CdSpectate.PrintUsage( player );
                }
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You can only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( !player.Spectate( target ) ) {
                player.Message( "Already spectating {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Handler = UnspectateHandler
        };

        static void UnspectateHandler( Player player, Command cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }

        #endregion


        // freeze target if player is allowed to do so
        static void FreezeIfAllowed( Player player, PlayerInfo targetInfo ) {
            if( !targetInfo.IsFrozen && player.Can( Permission.Freeze, targetInfo.Rank ) ) {
                FreezeHandler( player, new Command( "/freeze " + targetInfo.Name ) );
            }
        }


        // warn player if others are still online from target's IP
        static void WarnIfOtherPlayersOnIP( Player player, PlayerInfo targetInfo, Player except ) {
            Player[] otherPlayers = Server.Players.FromIP( targetInfo.LastIP )
                                                  .Except( except )
                                                  .ToArray();
            if( otherPlayers.Length > 0 ) {
                player.Message( "&WWarning: Other player(s) share IP with {0}&W: {1}",
                                targetInfo.ClassyName,
                                otherPlayers.JoinToClassyString() );
            }
        }
    }
}