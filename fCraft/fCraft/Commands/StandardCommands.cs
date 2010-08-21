// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;


namespace fCraft {
    static class StandardCommands {

        // Register standard commands.
        internal static void Init() {

            string banCommonHelp = "Ban information can be viewed with &H/baninfo";

            CommandList.RegisterCommand( cdMe );

            CommandList.RegisterCommand( cdNick );
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



        static CommandDescriptor cdNick = new CommandDescriptor {
            name = "nick",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ChangeName },
            usage = "/nick [Nickname]",
            help = "Allows temporarily changing your displayed name. " +
                   "The new name is shown in chat and player list. " +
                   "The skin also changes to match the new name. " +
                   "To reset the name back to normal, write &H/nick&S without any parameters.",
            handler = Nick
        };

        internal static void Nick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                if( player.nick != player.name ) {
                    Server.SendToAll( Color.Sys + player.nick + " reset their name back to \"" + player.name + "\"", player );
                    player.Message( "You name is reset back to \"" + player.name + "\"" );
                    player.nick = player.name;
                    if( player.world != null ) player.world.UpdatePlayer( player );
                } else {
                    player.Message( "You do not have a nickname set." );
                }
            } else if( Player.IsValidName( name ) ) {
                Server.SendToAll( Color.Sys + player.nick + " is now known as " + name, player );
                player.Message( "You are now known as {0}. Use &H/nick&S again to reset.", name );
                player.nick = name;
                if( player.world != null ) player.world.UpdatePlayer( player );
            } else {
                player.Message( "Invalid player name." );
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
            string msg = Color.Silver + player.nick + " rolled " + num + " (" + min + "..." + max + ")";
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
            help = "Bans the player's IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. Note: does NOT ban the player name, just the IP." +
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
            help = "Removes ban for a specified player's last known IP. Does NOT remove the name bans. " +
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
                    player.Message( Color.Red + offender.GetLogName() + " has been frozen while you retry." );
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
                if( !player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    player.Message( "You can only ban players ranked {0}{1}&S or lower.",
                                    player.info.playerClass.maxBan.color,
                                    player.info.playerClass.maxBan.name );
                    player.Message( offender.GetLogName() + " is ranked " + offender.info.playerClass.name + "." );
                } else {
                    address = offender.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, offender.name, banAll, unban );
                    if( !banAll ) {
                        if( offender.info.ProcessBan( player, reason ) ) {
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity, offender.info.name, player.GetLogName() );
                            Server.SendToAll( Color.Red + offender.name + " was banned by " + player.nick, offender );
                            offender.session.Kick( "You were just banned by " + player.GetLogName() );
                        } else {
                            player.Message( offender.name + " is already banned." );
                        }
                    }
                }

                // ban offline players
            } else if( info != null ) {
                if( !player.info.playerClass.CanBan( info.playerClass ) ) {
                    PlayerClass maxRank = player.info.playerClass.maxBan;
                    if( maxRank == null ) {
                        player.Message( "You can only ban players ranked " + player.info.playerClass.color + player.info.playerClass.name + Color.Sys + " or lower." );
                    } else {
                        player.Message( "You can only ban players ranked " + maxRank.color + maxRank.name + Color.Sys + " or lower." );
                    }
                    player.Message( info.name + " is ranked " + info.playerClass.name + "." );
                } else {
                    address = info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.name, banAll, unban );
                    if( unban ) {
                        if( info.ProcessUnban( player.name, reason ) ) {
                            Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity, info.name, player.GetLogName() );
                            Server.SendToAll( Color.Red + info.name + " (offline) was unbanned by " + player.nick );
                            if( Config.GetBool(ConfigKey.AnnounceKickAndBanReasons) && reason != null && reason.Length > 0 ) {
                                Server.SendToAll( Color.Red + "Unban reason: " + reason );
                            }
                        } else {
                            player.Message( info.name + " (offline) is not currenty banned." );
                        }
                    } else {
                        if( info.ProcessBan( player, reason ) ) {
                            Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity, info.name, player.GetLogName() );
                            Server.SendToAll( Color.Red + info.name + " (offline) was banned by " + player.nick );
                            if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                                Server.SendToAll( Color.Red + "Ban reason: " + reason );
                            }
                        } else {
                            player.Message( info.name + " (offline) is already banned." );
                        }
                    }
                }
            } else if( Player.IsValidName( nameOrIP ) ) {
                if( unban ) {
                    player.Message( info.name + " (unrecognized) is not currenty banned." );
                } else {
                    info = PlayerDB.AddFakeEntry( nameOrIP );
                    info.ProcessBan( player, reason );
                    player.Message( "Previously-unseen player \"" + nameOrIP + "\" was banned." );
                    Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity, info.name, player.GetLogName() );
                    Server.SendToAll( Color.Red + info.name + " (offline) was banned by " + player.nick );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }
                }
            } else {
                player.Message( "Please specify valid player name or IP." );
            }
        }

        internal static void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {
            Player other;
            if( unban ) {
                if( IPBanList.Remove( address ) ) {
                    player.Message( address.ToString() + " has been removed from the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was unbanned by " + player.nick );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Unban reason: " + reason );
                    }
                } else {
                    player.Message( address.ToString() + " is not currently banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( otherInfo.ProcessUnban( player.name, reason + "~UnBanAll" ) ) {
                            Server.SendToAll( Color.Red + otherInfo.name + " was unbanned by " + player.nick + " (UnbanAll)" );
                            player.Message( otherInfo.name + " matched the IP and was also unbanned." );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    player.Message( address.ToString() + " has been added to the IP ban list." );
                    Server.SendToAll( Color.Red + address.ToString() + " was banned by " + player.nick );
                    if( Config.GetBool( ConfigKey.AnnounceKickAndBanReasons ) && reason != null && reason.Length > 0 ) {
                        Server.SendToAll( Color.Red + "Ban reason: " + reason );
                    }

                } else {
                    player.Message( address.ToString() + " is already banned." );
                }
                foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                    if( banAll && otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                        player.Message( otherInfo.name + " matched the IP and was also banned." );
                    }
                    other = player.world.FindPlayerExact( otherInfo.name );
                    Server.SendToAll( Color.Red + otherInfo.name + " was banned by " + player.nick + " (BanAll)" );
                    if( other != null ) {
                        other.session.Kick( "Your IP was just banned by " + player.GetLogName() );
                    }
                }
            }
        }



        static CommandDescriptor cdKick = new CommandDescriptor {
            name = "kick",
            aliases = new string[] { "k" },
            consoleSafe = true,
            permissions = new Permission[] { Permission.Kick },
            usage = "/kick PlayerName [Message]",
            help = "Kicks the specified player from the server. " +
                   "Kicked player gets to see the specified message on their disconnect screen.",
            handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string msg = cmd.NextAll();
                Player target = Server.FindPlayer( name );
                if( target != null ) {
                    DoKick( player, target, msg, false );
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
                player.Message( "You can only kick players ranked " + player.info.playerClass.maxKick.color + player.info.playerClass.maxKick.name + Color.Sys + " or lower." );
                player.Message( target.GetLogName() + " is ranked " + target.info.playerClass.name + "." );
                return false;
            } else {
                if( !silent ) Server.SendToAll( Color.Red + target.nick + " was kicked by " + player.nick );
                target.info.ProcessKick( player );
                if( reason != null && reason.Length > 0 ) {
                    if( !silent ) Server.SendToAll( Color.Red + "Kick reason: " + reason );
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.GetLogName(),
                                player.GetLogName(),
                                reason );
                    target.session.Kick( "Kicked by " + player.GetLogName() + ": " + reason );
                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.GetLogName(),
                                player.GetLogName() );
                    target.session.Kick( "You have been kicked by " + player.GetLogName() );
                }
                return true;
            }
        }



        static CommandDescriptor cdChangeClass = new CommandDescriptor {
            name = "user",
            aliases = new string[] { "rank", "promote", "demote" },
            consoleSafe = true,
            usage = "/user PlayerName ClassName [Reason]",
            help = "Changes the class/rank of a player to a specified class. "+
                   "Any text specified after the ClassName will be saved as a memo.",
            handler = ChangeClass
        };

        internal static void ChangeClass( Player player, Command cmd ) {
            string name = cmd.Next();
            string newClassName = cmd.Next();

            // Check arguments
            if( newClassName == null ) {
                player.Message( "Usage: " + Color.Help + "/user PlayerName ClassName" );
                player.Message( "To see a list of classes and permissions, use " + Color.Help + "/class" );
                return;
            }

            // Parse class name
            PlayerClass newClass = ClassList.FindClass( newClassName );
            if( newClass == null ) {
                player.Message( "Unrecognized player class: " + newClassName );
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
                player.Message( "Note: \"" + name + "\" was not found in PlayerDB." );
            }



            DoChangeClass( player, info, target, newClass, cmd.NextAll() );
        }

        internal static void DoChangeClass( Player player, PlayerInfo targetInfo, Player target, PlayerClass newClass, string reason ) {

            bool promote = (targetInfo.playerClass.rank < newClass.rank);
            string targetFullName = (target == null ? targetInfo.name : target.GetLogName());

            // Make sure it's not same rank
            if( targetInfo.playerClass == newClass ) {
                player.Message( targetFullName + " is already " + newClass.color + newClass.name );
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
                player.Message( "You can only promote players up to " + player.info.playerClass.maxPromote.color + player.info.playerClass.maxPromote.name );
                player.Message( targetFullName + " is ranked " + targetInfo.playerClass.name + "." );
                return;
            } else if( !promote && !player.info.playerClass.CanDemote( targetInfo.playerClass ) ) {
                player.Message( "You can only demote players that are " + player.info.playerClass.maxDemote.color + player.info.playerClass.maxDemote.name + Color.Sys + " or lower." );
                player.Message( targetFullName + " is ranked " + targetInfo.playerClass.name + "." );
                return;
            }

            if( Config.GetBool( ConfigKey.RequireClassChangeReason ) && (reason == null || reason.Length == 0) ) {
                if( promote ) {
                    player.Message( Color.Red + "Please specify a promotion reason." );
                } else {
                    player.Message( Color.Red + "Please specify a demotion reason." );
                }
                cdChangeClass.PrintUsage( player );
            }

            // Do the class change
            if( (promote && targetInfo.playerClass.rank < newClass.rank) ||
                (!promote && targetInfo.playerClass.rank > newClass.rank) ) {
                PlayerClass oldClass = targetInfo.playerClass;

                if( !Server.FirePlayerClassChange( targetInfo, player, oldClass, newClass ) ) return;

                Logger.Log( "{0} changed the class of {1} from {2} to {3}.", LogType.UserActivity,
                            player.GetLogName(), targetFullName, targetInfo.playerClass.name, newClass.name );

                targetInfo.ProcessClassChange( newClass, player, reason );

                Server.FirePlayerListChangedEvent();

                string verb = (promote ? "promoted" : "demoted");

                if( Config.GetBool( ConfigKey.AnnounceClassChanges ) ) {
                    Server.SendToAll( String.Format( "&S{0} was {1} from {2}{3} &Sto {4}{5}",
                                                    targetInfo.name,
                                                    verb,
                                                    oldClass.color,
                                                    oldClass.name,
                                                    newClass.color,
                                                    newClass.name ) );
                } else {
                    player.Message( "You {0} {1} from {2}{3} &Sto {4}{5}",
                                    verb,
                                    targetInfo.name,
                                    oldClass.color,
                                    oldClass.name,
                                    newClass.color,
                                    newClass.name );
                }

                if( target != null ) {
                    target.Send( PacketWriter.MakeSetPermission( target ) );
                    target.Message( "You have been {0} to {1}{2} &Sby {3}",
                                    verb,
                                    newClass.color,
                                    newClass.name,
                                    player.GetLogName() );
                    target.world.UpdatePlayer( target );
                }
            } else {
                if( promote ) {
                    player.Message( targetFullName + " is already same or lower rank than " + newClass.name );
                } else {
                    player.Message( targetFullName + " is already same or higher rank than " + newClass.name );
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
                    Position pos = target.pos; // fix for off-by-1 error in the way teleports are handled by the client
                    pos.x += 1;
                    pos.y += 1;
                    pos.h += 1;
                    player.Send( PacketWriter.MakeTeleport( 255, pos ) );
                } else if( cmd.Next() == null ) {
                    target = Server.FindPlayer( name );
                    if( target == null ) {
                        player.NoPlayerMessage( name );
                    } else {
                        if( player.CanJoin( target.world ) ) {
                            player.session.JoinWorld( target.world, target.pos );
                        } else {
                            player.Message( "Cannot teleport to {0} because world \"{1}\"requires {2}{3}+&S to join.",
                                            target.name,
                                            target.world.name,
                                            target.world.classAccess.color,
                                            target.world.classAccess.name );
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
            Player target = player.world.FindPlayer( name );
            if( target != null ) {
                Position pos = player.pos;
                pos.x += 1;
                pos.y += 1;
                pos.h += 1;
                target.Send( PacketWriter.MakeTeleport( 255, pos ) );
            } else {
                target = Server.FindPlayer( name );
                if( target == null ) {
                    player.NoPlayerMessage( name );
                } else {
                    if( target.CanJoin( player.world ) ) {
                        target.session.JoinWorld( player.world, player.pos );
                    } else {
                        player.Message( "Cannot bring \"{0}\" because this world requires {0}{1}+&S to join.",
                                        target.name,
                                        player.world.classAccess.color,
                                        player.world.classAccess.name );
                    }
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
            Player target = Server.FindPlayer( name );
            if( target != null ) {
                if( !target.isFrozen ) {
                    Server.SendToAll( Color.Sys + target.nick + " has been frozen by " + player.nick );
                    target.isFrozen = true;
                } else {
                    player.Message( target.GetLogName() + " is already frozen." );
                }
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
            Player target = Server.FindPlayer( name );
            if( target != null ) {
                if( target.isFrozen ) {
                    Server.SendToAll( Color.Sys + target.nick + " is no longer frozen." );
                    target.isFrozen = false;
                } else {
                    player.Message( target.GetLogName() + " is currently not frozen." );
                }
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
                Server.SendToAll( PacketWriter.MakeRemoveEntity( player.id ), null );
                Server.SendToAll( Color.Sys + player.nick + " left the server." );
                player.isHidden = true;
                player.Message( Color.Gray + "You are now hidden." );
                player.nick = player.name;
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
                    player.Message( Color.Gray + "You are no longer hidden." );
                    if( player.nick != player.name ) {
                        player.nick = player.name;
                        player.Message( "For security reasons, your nick was reset." );
                    }
                    player.world.SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
                    Server.ShowPlayerConnectedMessage( player );
                    player.isHidden = false;
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
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity, player.GetLogName() );
            } else {
                player.NoAccessMessage( Permission.SetSpawn );
            }
        }



        static CommandDescriptor cdSpectate = new CommandDescriptor {
            name = "spectate",
            permissions = new Permission[]{Permission.Spectate},
            help = "spectate",
            handler = Spectate
        };

        internal static void Spectate( Player player, Command cmd ) {
        }
    }
}