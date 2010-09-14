// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Collections.Generic;


namespace fCraft {
    static class StandardCommands {

        // Register standard commands.
        internal static void Init() {

            string banCommonHelp = "Ban information can be viewed with &H/baninfo";

            CommandList.RegisterCommand( cdMe );

            CommandList.RegisterCommand( cdRoll );
            CommandList.RegisterCommand( cdSay );

            cdBan.help += banCommonHelp;
            cdBanIP.help += banCommonHelp;
            cdBanAll.help += banCommonHelp;
            cdUnban.help += banCommonHelp;
            cdUnbanIP.help += banCommonHelp;
            cdUnbanAll.help += banCommonHelp;

            CommandList.RegisterCommand( cdBan );
            CommandList.RegisterCommand( cdBanIP );
            CommandList.RegisterCommand( cdBanAll );
            CommandList.RegisterCommand( cdUnban );
            CommandList.RegisterCommand( cdUnbanIP );
            CommandList.RegisterCommand( cdUnbanAll );

            CommandList.RegisterCommand( cdKick );
            CommandList.RegisterCommand( cdChangeClass );
            CommandList.RegisterCommand( cdTP );
            CommandList.RegisterCommand( cdBring );

            CommandList.RegisterCommand( cdFreeze );
            CommandList.RegisterCommand( cdUnfreeze );

            CommandList.RegisterCommand( cdHide );
            CommandList.RegisterCommand( cdUnhide );
            CommandList.RegisterCommand( cdSetSpawn );

            CommandList.RegisterCommand( cdReloadConfig );
        }


        static CommandDescriptor cdReloadConfig = new CommandDescriptor {
            name = "reloadconfig",
            permissions = new Permission[] { Permission.ReloadConfig },
            consoleSafe = true,
            help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes class changes and IRC bot settings. " +
                   "Server has to be restarted to change those.",
            handler = ReloadConfig
        };

        static void ReloadConfig( Player player, Command cmd ) {
            player.Message( "Attempting to reload config..." );
            if( Config.Load( true ) ) {
                player.Message( "Config reloaded." );
            } else {
                player.Message( "An error occured while trying to reload the config. See server log for details." );
            }
        }



        static CommandDescriptor cdMe = new CommandDescriptor {
            name = "me",
            consoleSafe = true,
            usage = "/me Message",
            help = "Sends IRC-style action message prefixed with your name.",
            handler = Me
        };

        internal static void Me( Player player, Command cmd ) {
            string msg = cmd.NextAll().Trim();
            if( msg != null ) {
                Server.SendToAll( "*" + Color.Purple + player.name + " " + msg );
            }
        }



        static CommandDescriptor cdRoll = new CommandDescriptor {
            name = "roll",
            consoleSafe = true,
            help = "Gives random number between 1 and 100.&N" +
                   "&H/roll MaxNumber&N" +
                   "Gives number between 1 and max.&N" +
                   "&H/roll MinNumber MaxNumber&N" +
                   "Gives number between min and max.",
            handler = Roll
        };

        internal static void Roll( Player player, Command cmd ) {
            Random rand = new Random();
            int min = 1, max = 100, num, t1, t2;
            if( cmd.NextInt( out t1 ) ) {
                if( cmd.NextInt( out t2 ) ) {
                    if( t2 >= t1 ) {
                        min = t1;
                        max = t2;
                    }
                } else if( t1 >= 1 ) {
                    max = t1;
                }
            }
            num = rand.Next( min, max + 1 );
            string msg = player.GetClassyName() + Color.Silver + " rolled " + num + " (" + min + "..." + max + ")";
            Logger.LogConsole( msg );
            Server.SendToAll( msg );
        }



        static CommandDescriptor cdSay = new CommandDescriptor {
            name = "say",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Say },
            usage = "/say Message",
            help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            handler = Say
        };

        internal static void Say( Player player, Command cmd ) {
            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    Server.SendToAll( Color.Say + msg.Trim() );
                } else {
                    cdSay.PrintUsage( player );
                }
            } else {
                player.NoAccessMessage( Permission.Say );
            }
        }



        static CommandDescriptor cdBan = new CommandDescriptor {
            name = "ban",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban },
            usage = "/ban PlayerName [Reason]",
            help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Ban
        };

        internal static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, false );
        }



        static CommandDescriptor cdBanIP = new CommandDescriptor {
            name = "banip",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
            usage = "/banip PlayerName|IPAddress [Reason]",
            help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanIP
        };

        internal static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, false );
        }



        static CommandDescriptor cdBanAll = new CommandDescriptor {
            name = "banall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/banall PlayerName|IPAddress [Reason]",
            help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            handler = BanAll
        };

        internal static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, false );
        }



        static CommandDescriptor cdUnban = new CommandDescriptor {
            name = "unban",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban },
            usage = "/unban PlayerName [Reason]",
            help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = Unban
        };

        internal static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, true );
        }



        static CommandDescriptor cdUnbanIP = new CommandDescriptor {
            name = "unbanip",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP },
            usage = "/unbanip PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanIP
        };

        internal static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, true );
        }



        static CommandDescriptor cdUnbanAll = new CommandDescriptor {
            name = "unbanall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            usage = "/unbanall PlayerName|IPaddress [Reason]",
            help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            handler = UnbanAll
        };

        internal static void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, true );
        }


        internal static void DoBan( Player player, string nameOrIP, string reason, bool banIP, bool banAll, bool unban ) {
            if( nameOrIP == null ) {
                player.Message( "Please specify player name or IP to ban." );
                return;
            }

            IPAddress address;
            Player offender = Server.FindPlayerExact( nameOrIP );
            PlayerInfo info = PlayerDB.FindPlayerInfoExact( nameOrIP );

            if( Config.GetBool( ConfigKey.RequireBanReason ) && (reason == null || reason.Length == 0) ) {
                player.Message( Color.Red + "Please specify a ban/unban reason." );
                // freeze the target player to prevent further damage
                if( !unban && offender != null && player.Can( Permission.Freeze ) && player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    player.Message( offender.GetClassyName() + Color.Red + " has been frozen while you retry." );
                    Freeze( player, new Command( "/freeze " + offender.name ) );
                }

                return;
            }

            // ban by IP address
            if( banIP && IPAddress.TryParse( nameOrIP, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

            // ban online players
            } else if( !unban && offender != null ) {

                // check permissions
                if( player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    address = offender.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, offender.name, banAll, unban );
                    if( !banAll ) {
                        if( offender.info.ProcessBan( player, reason ) ) {
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        offender.info.name, player.name );
                            Server.SendToAll( offender.GetClassyName() + Color.Red + " was banned by " + player.GetClassyName(), offender );
                            if( reason != null && reason.Length > 0 ) {
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                                    Server.SendToAll( Color.Red + "Ban reason: " + reason );
                                }
                                offender.session.Kick( "Banned by " + player.GetClassyName() + Color.White + ": " + reason );
                            } else {
                                offender.session.Kick( "Banned by " + player.GetClassyName() );
                            }
                        } else {
                            player.Message( offender.GetClassyName() + "&S is already banned." );
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.info.playerClass.maxBan.GetClassyName() );
                    player.Message( "{0}&S is ranked {1}",
                                    offender.GetClassyName(),
                                    offender.info.playerClass.GetClassyName() );
                }

                // ban or unban offline players
            } else if( info != null ) {
                if( player.info.playerClass.CanBan( info.playerClass ) || unban ) {
                    address = info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.name, banAll, unban );
                    if( !banAll ) {
                        if( unban ) {
                            if( info.ProcessUnban( player.name, reason ) ) {
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( info.GetClassyName() + Color.Red + " (offline) was unbanned by " + player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                    Server.SendToAll( Color.Red + "Unban reason: " + reason );
                                }
                            } else {
                                player.Message( info.name + " (offline) is not currenty banned." );
                            }
                        } else {
                            if( info.ProcessBan( player, reason ) ) {
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            info.name, player.name );
                                Server.SendToAll( info.GetClassyName() + Color.Red + " (offline) was banned by " + player.GetClassyName() );
                                if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                    Server.SendToAll( Color.Red + "Ban reason: " + reason );
                                }
                            } else {
                                player.Message( info.GetClassyName() + "&S (offline) is already banned." );
                            }
                        }
                    }
                } else {
                    PlayerClass maxRank = player.info.playerClass.maxBan;
                    if( maxRank == null ) {
                        player.Message( "You can only ban players ranked {0}&S or lower.",
                                        player.info.playerClass.GetClassyName() );
                    } else {
                        player.Message( "You can only ban players ranked {0}&S or lower.",
                                        maxRank.GetClassyName() );
                    }
                    player.Message( "{0} is ranked {1}",
                                    info.name,
                                    info.playerClass.name );
                }

                // ban players who are not in the database yet
            } else if( Player.IsValidName( nameOrIP ) ) {
                if( unban ) {
                    player.Message( nameOrIP + " (unrecognized) is not banned." );
                } else {
                    info = PlayerDB.AddFakeEntry( nameOrIP );
                    info.ProcessBan( player, reason );
                    player.Message( "Player \"" + nameOrIP + "\" (unrecognized) was banned." );
                    Logger.Log( "{0} (unrecognized) was banned by {1}", LogType.UserActivity,
                                info.name,
                                player.GetClassyName() );
                    Server.SendToAll( Color.Red + info.name + " (unrecognized) was banned by " + player.GetClassyName() );

                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }
                }
            } else {
                player.Message( "Please specify valid player name or IP." );
            }
        }

        internal static void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {

            if( address == IPAddress.None || address == IPAddress.Any ) {
                player.Message( "Invalid IP: " + address );
                return;
            }

            if( unban ) {
                if( IPBanList.Remove( address ) ) {
                    player.Message( address.ToString() + " has been removed from the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was unbanned by " + player.GetClassyName() );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Unban reason: " + reason );
                    }
                } else {
                    player.Message( address.ToString() + " is not currently banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( otherInfo.ProcessUnban( player.name, reason + "~UnBanAll" ) ) {
                            Server.SendToAll( Color.Red + otherInfo.name + " was unbanned (UnbanAll) by " + player.GetClassyName() );
                            player.Message( otherInfo.name + " matched the IP and was also unbanned." );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    player.Message( address.ToString() + " has been added to the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was banned by " + player.GetClassyName() );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }

                } else {
                    player.Message( address.ToString() + " is already banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( banAll && otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                            player.Message( otherInfo.name + " matched the IP and was also banned." );
                        }
                        Server.SendToAll( String.Format( "{0}{1} was banned (BanAll) by {2}",
                                                         otherInfo.GetClassyName(),
                                                         Color.Red,
                                                         player.GetClassyName() ) );
                        foreach( Player other in Server.FindPlayers( address ) ) {
                            if( reason != null && reason.Length > 0 ) {
                                other.session.Kick( "IP-banned by " + player.GetClassyName() + Color.White + ": " + reason );
                            } else {
                                other.session.Kick( "IP-banned by " + player.GetClassyName() );
                            }
                        }
                    }
                }
            }
        }



        static CommandDescriptor cdKick = new CommandDescriptor {
            name = "kick",
            aliases = new string[] { "k" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.Kick },
            usage = "/kick PlayerName [Reason]",
            help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string msg = cmd.NextAll();
                List<Player> targets = Server.FindPlayers( player, name );
                if( targets.Count == 1 ) {
                    DoKick( player, targets[0], msg, false );
                } else if( targets.Count > 1 ) {
                    player.ManyPlayersMessage( targets );
                } else {
                    player.NoPlayerMessage( name );
                }
            } else {
                player.Message( "Usage: " + Color.Help + "/kick PlayerName [Message]" +
                                   Color.Sys + " or " + Color.Help + "/k PlayerName [Message]" );
            }
        }

        internal static bool DoKick( Player player, Player target, string reason, bool silent ) {
            if( !player.info.playerClass.CanKick( target.info.playerClass ) ) {
                player.Message( "You can only kick players ranked {0}&S or lower.",
                                player.info.playerClass.maxKick.GetClassyName() );
                player.Message( target.GetClassyName() + "&S is ranked " + target.info.playerClass.GetClassyName() );
                return false;
            } else {
                if( !silent ) {
                    Server.SendToAll( target.GetClassyName() + Color.Red + " was kicked by " + player.GetClassyName() );
                    target.info.ProcessKick( player );
                }
                if( reason != null && reason.Length > 0 ) {
                    if( !silent && Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) ) {
                        Server.SendToAll( Color.Red + "Kick reason: " + reason );
                    }
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.name, player.name, reason );
                    target.session.Kick( "Kicked by " + player.GetClassyName() + Color.White + ": " + reason );
                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.name, player.name );
                    target.session.Kick( "You have been kicked by " + player.GetClassyName() );
                }
                return true;
            }
        }



        static CommandDescriptor cdChangeClass = new CommandDescriptor {
            name = "user",
            aliases = new string[] { "rank", "promote", "demote" },
            consoleSafe = true,
            usage = "/user PlayerName ClassName [Reason]",
            help = "Changes the class/rank of a player to a specified class. " +
                   "Any text specified after the ClassName will be saved as a memo.",
            handler = ChangeClass
        };

        internal static void ChangeClass( Player player, Command cmd ) {
            string name = cmd.Next();
            string newClassName = cmd.Next();

            // Check arguments
            if( newClassName == null ) {
                cdChangeClass.PrintUsage( player );
                player.Message( "See &H/classes&S for list of player classes." );
                return;
            }

            // Parse class name
            PlayerClass newClass = ClassList.FindClass( newClassName );
            if( newClass == null ) {
                player.Message( "Unrecognized player class: {0}",
                                newClassName );
                return;
            }

            // Parse player name
            PlayerInfo info;
            Player target = Server.FindPlayerExact( name );
            if( target == null ) {
                info = PlayerDB.FindPlayerInfoExact( name );
            } else {
                info = target.info;
            }

            if( info == null ) {
                info = PlayerDB.AddFakeEntry( name );
                player.Message( "Warning: player \"{0}\" is in the database (possible typo)",
                                name );
            }



            DoChangeClass( player, info, target, newClass, cmd.NextAll() );
        }

        internal static void DoChangeClass( Player player, PlayerInfo targetInfo, Player target, PlayerClass newClass, string reason ) {

            bool promote = (targetInfo.playerClass.rank < newClass.rank);

            // Make sure it's not same rank
            if( targetInfo.playerClass == newClass ) {
                player.Message( "{0} is already ranked {1}",
                                targetInfo.name,
                                newClass.GetClassyName() );
                return;
            }

            // Make sure player has the general permissions
            if( (promote && !player.Can( Permission.Promote )) ) {
                player.NoAccessMessage( Permission.Promote );
                return;
            } else if( !promote && !player.Can( Permission.Demote ) ) {
                player.NoAccessMessage( Permission.Demote );
                return;
            }

            // Make sure player has the specific permissions (including limits)
            if( promote && !player.info.playerClass.CanPromote( newClass ) ) {
                player.Message( "You can only promote players up to {0}",
                                player.info.playerClass.maxPromote.GetClassyName() );
                player.Message( "{0} is ranked {1}",
                                targetInfo.name,
                                targetInfo.playerClass.GetClassyName() );
                return;
            } else if( !promote && !player.info.playerClass.CanDemote( targetInfo.playerClass ) ) {
                player.Message( "You can only demote players that are {0}&S or lower",
                                player.info.playerClass.maxDemote.GetClassyName() );
                player.Message( "{0} is ranked {1}",
                                targetInfo.name,
                                targetInfo.playerClass.GetClassyName() );
                return;
            }

            if( Config.GetBool( ConfigKey.RequireClassChangeReason ) && (reason == null || reason.Length == 0) ) {
                if( promote ) {
                    player.Message( Color.Red + "Please specify a promotion reason." );
                } else {
                    player.Message( Color.Red + "Please specify a demotion reason." );
                }
                cdChangeClass.PrintUsage( player );
                return;
            }

            // Do the class change
            if( (promote && targetInfo.playerClass.rank < newClass.rank) ||
                (!promote && targetInfo.playerClass.rank > newClass.rank) ) {
                PlayerClass oldClass = targetInfo.playerClass;

                if( !Server.FirePlayerClassChange( targetInfo, player, oldClass, newClass ) ) return;

                Logger.Log( "{0} changed the class of {1} from {2} to {3}.", LogType.UserActivity,
                            player.name, targetInfo.name, targetInfo.playerClass.name, newClass.name );

                // if player is online, toggle visible/invisible players
                if( target != null && target.world != null ) {

                    HashSet<Player> invisiblePlayers = new HashSet<Player>();
                    HashSet<Player> blindPlayers = new HashSet<Player>();

                    Player[] plist = target.world.playerList;
                    for( int i = 0; i < plist.Length; i++ ) {
                        if( !target.CanSee( plist[i] ) ) {
                            invisiblePlayers.Add( plist[i] );
                        }
                        if( !plist[i].CanSee( target ) ) {
                            blindPlayers.Add( plist[i] );
                        }
                    }

                    targetInfo.ProcessClassChange( newClass, player, reason );

                    for( int i = 0; i < plist.Length; i++ ) {
                        if( target.CanSee( plist[i] ) && invisiblePlayers.Contains( plist[i] ) ) {
                            target.Send( PacketWriter.MakeAddEntity( plist[i], plist[i].pos ) );
                        } else if( !target.CanSee( plist[i] ) && !invisiblePlayers.Contains( plist[i] ) ) {
                            target.Send( PacketWriter.MakeRemoveEntity( plist[i].id ) );
                        }
                        if( plist[i].CanSee( target ) && blindPlayers.Contains( plist[i] )){
                            plist[i].Send( PacketWriter.MakeAddEntity( target, target.pos ) );
                        } else if( !plist[i].CanSee( target ) && !blindPlayers.Contains( plist[i] ) ) {
                            plist[i].Send( PacketWriter.MakeRemoveEntity( target.id ) );
                        }
                    }

                } else {
                    targetInfo.ProcessClassChange( newClass, player, reason );
                }

                Server.FirePlayerListChangedEvent();

                string verb = (promote ? "promoted" : "demoted");

                if( Config.GetBool( ConfigKey.AnnounceClassChanges ) ) {
                    Server.SendToAll( String.Format( "&S{0} was {1} from {2}&S to {3}",
                                                    targetInfo.name,
                                                    verb,
                                                    oldClass.GetClassyName(),
                                                    newClass.GetClassyName() ) );
                } else {
                    player.Message( "You {0} {1} from {2}&S to {3}",
                                    verb,
                                    targetInfo.name,
                                    oldClass.GetClassyName(),
                                    newClass.GetClassyName() );
                }

                if( target != null ) {
                    target.Send( PacketWriter.MakeSetPermission( target ) );
                    target.Message( "You have been {0} to {1}&S by {2}",
                                    verb,
                                    newClass.GetClassyName(),
                                    player.GetClassyName() );
                    target.world.UpdatePlayer( target );
                }
            } else {
                if( promote ) {
                    player.Message( "{0}&S is already same or lower rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newClass.GetClassyName() );
                } else {
                    player.Message( "{0}&S is already same or higher rank than {1}",
                                    targetInfo.GetClassyName(),
                                    newClass.GetClassyName() );
                }
            }
        }



        static CommandDescriptor cdTP = new CommandDescriptor {
            name = "tp",
            aliases = new string[] { "spawn" },
            permissions = new Permission[] { Permission.Teleport },
            usage = "/tp [PlayerName]&S or &H/tp X Y Z",
            help = "Teleports you to a specified player's location. " +
                   "If no name is given, teleports you to map spawn. " +
                   "If coordinates are given, teleports to that location.",
            handler = TP
        };

        internal static void TP( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Send( PacketWriter.MakeTeleport( 255, player.world.map.spawn ) );
            } else {
                Player target = player.world.FindPlayer( name );
                if( target != null ) {
                    Position pos = target.pos; // fix for offset errors in the way teleports are handled by the client
                    pos.x += 1;
                    pos.y += 1;
                    pos.h -= 16;
                    player.Send( PacketWriter.MakeTeleport( 255, pos ) );
                } else if( cmd.Next() == null ) {

                    List<Player> targets = Server.FindPlayers( player, name );
                    if( targets.Count == 1 ) {
                        target = targets[0];
                        if( player.CanJoin( target.world ) ) {
                            player.session.JoinWorld( target.world, target.pos );
                        } else {
                            player.Message( "Cannot teleport to {0}&S because world {1}&S requires you to be {2}+&S to join.",
                                            target.GetClassyName(),
                                            target.world.GetClassyName(),
                                            target.world.classAccess.GetClassyName() );
                        }
                    } else if( targets.Count > 1 ) {
                        player.ManyPlayersMessage( targets );
                    } else {
                        World w = Server.FindWorld( name );
                        if( w != null ) {
                            player.ParseMessage( "/join " + name, false );
                        } else {
                            player.NoPlayerMessage( name );
                        }
                    }

                } else {
                    cmd.Rewind();
                    int x, y, h;
                    if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out h ) ) {
                        if( x < 0 || x > player.world.map.widthX ||
                            y < 0 || y > player.world.map.widthY ||
                            y < 0 || y > player.world.map.height ) {
                            player.Message( "Specified coordinates are outside the map!" );
                        } else {
                            player.pos.Set( x * 32 + 16, y * 32 + 16, h * 32 + 16, player.pos.r, player.pos.l );
                            player.Send( PacketWriter.MakeTeleport( 255, player.pos ) );
                        }
                    } else {
                        cdTP.PrintUsage( player );
                    }
                }
            }
        }



        static CommandDescriptor cdBring = new CommandDescriptor {
            name = "bring",
            permissions = new Permission[] { Permission.Bring },
            usage = "/bring PlayerName",
            help = "Teleports you to a specified player's location. If no name is given, teleports you to map spawn.",
            handler = Bring
        };

        internal static void Bring( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdBring.PrintUsage( player );
                return;
            }
            Player target = player.world.FindPlayer( name );
            if( target != null && player.CanSee( target ) ) {
                Position pos = player.pos;
                pos.x += 1;
                pos.y += 1;
                pos.h += 1;
                target.Send( PacketWriter.MakeTeleport( 255, pos ) );

            } else {
                List<Player> targets = Server.FindPlayers( player, name );
                if( targets.Count == 1 ) {
                    target = targets[0];
                    if( target.CanJoin( player.world ) ) {
                        target.session.JoinWorld( player.world, player.pos );
                    } else {
                        player.Message( "Cannot bring {0}&S because this world requires {0}+&S to join.",
                                        target.GetClassyName(),
                                        player.world.classAccess.GetClassyName() );
                    }
                } else if( targets.Count > 1 ) {
                    player.ManyPlayersMessage( targets );
                } else {
                    player.NoPlayerMessage( name );
                }
            }
        }



        static CommandDescriptor cdFreeze = new CommandDescriptor {
            name = "freeze",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Freeze },
            usage = "/freeze PlayerName",
            help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            handler = Freeze
        };

        internal static void Freeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }
            List<Player> targets = Server.FindPlayers( player, name );
            if( targets.Count == 1 ) {
                if( !targets[0].isFrozen ) {
                    Server.SendToAll( targets[0].GetClassyName() + "&S has been frozen by " + player.GetClassyName() );
                    targets[0].isFrozen = true;
                } else {
                    player.Message( targets[0].GetClassyName() + "&S is already frozen." );
                }
            } else if( targets.Count > 1 ) {
                player.ManyPlayersMessage( targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdUnfreeze = new CommandDescriptor {
            name = "unfreeze",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Freeze },
            usage = "/unfreeze PlayerName",
            help = "Releases the player from a frozen state. See &H/freeze&S for more information.",
            handler = Unfreeze
        };

        internal static void Unfreeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                cdFreeze.PrintUsage( player );
                return;
            }
            List<Player> targets = Server.FindPlayers( player, name );
            if( targets.Count == 1 ) {
                if( targets[0].isFrozen ) {
                    Server.SendToAll( targets[0].GetClassyName() + "&S is no longer frozen." );
                    targets[0].isFrozen = false;
                } else {
                    player.Message( targets[0].GetClassyName() + "&S is currently not frozen." );
                }
            } else if( targets.Count > 1 ) {
                player.ManyPlayersMessage( targets );
            } else {
                player.NoPlayerMessage( name );
            }
        }



        static CommandDescriptor cdHide = new CommandDescriptor {
            name = "hide",
            permissions = new Permission[] { Permission.Hide },
            help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( !player.isHidden ) {
                player.isHidden = true;

                Server.SendToBlind( PacketWriter.MakeRemoveEntity( player.id ), player );

                string message = String.Format( "{0}&S left the server.", player.GetClassyName() );
                foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                    Server.SendToBlind( packet, player );
                }

                message = String.Format( "{0}&S is now hidden.", player.GetClassyName() );
                foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                    Server.SendToSeeing( packet, player );
                }

                player.Message( Color.Gray + "You are now hidden." );
            } else {
                player.Message( "You are already hidden." );
            }
        }



        static CommandDescriptor cdUnhide = new CommandDescriptor {
            name = "unhide",
            permissions = new Permission[] { Permission.Hide },
            usage = "/unhide PlayerName",
            help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( player.Can( Permission.Hide ) ) {
                if( player.isHidden ) {
                    player.isHidden = false;

                    player.Message( Color.Gray + "You are no longer hidden." );
                    player.world.SendToBlind( PacketWriter.MakeAddEntity( player, player.pos ), player );

                    string message = String.Format( "{0}&S is no longer hidden.", player.GetClassyName() );
                    foreach( Packet packet in PacketWriter.MakeWrappedMessage( ">", message, false ) ) {
                        Server.SendToSeeing( packet, player );
                    }

                    Server.ShowPlayerConnectedMessage( player, false, player.world );
                } else {
                    player.Message( "You are not currently hidden." );
                }
            } else {
                player.NoAccessMessage( Permission.Hide );
            }
        }



        static CommandDescriptor cdSetSpawn = new CommandDescriptor {
            name = "setspawn",
            permissions = new Permission[] { Permission.SetSpawn },
            help = "Assigns your current location to be the spawn point of the map/world.",
            handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            if( player.Can( Permission.SetSpawn ) ) {
                player.world.map.spawn = player.pos;
                player.world.map.changesSinceSave++;
                player.Send( PacketWriter.MakeTeleport( 255, player.world.map.spawn ), true );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.name );
            } else {
                player.NoAccessMessage( Permission.SetSpawn );
            }
        }


        /*
        static CommandDescriptor cdSpectate = new CommandDescriptor {
            name = "spectate",
            permissions = new Permission[]{Permission.Spectate},
            help = "spectate",
            handler = Spectate
        };

        internal static void Spectate( Player player, Command cmd ) {
        }
        */
    }
}