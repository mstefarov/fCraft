// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;


namespace fCraft {
    static class StandardCommands {

        // Register standard commands.
        internal static void Init() {
            Commands.AddCommand( "k", Kick, true );
            Commands.AddCommand( "kick", Kick, true );

            Commands.AddCommand( "ban", Ban, true );
            Commands.AddCommand( "banip", BanIP, true );
            Commands.AddCommand( "banall", BanAll, true );
            Commands.AddCommand( "unban", Unban, true );
            Commands.AddCommand( "unbanip", UnbanIP, true );
            Commands.AddCommand( "unbanall", UnbanAll, true );

            Commands.AddCommand( "user", ChangeClass, true );

            Commands.AddCommand( "tp", TP, false );
            Commands.AddCommand( "bring", Bring, false );
            Commands.AddCommand( "freeze", Freeze, false );
            Commands.AddCommand( "unfreeze", Unfreeze, false );
            Commands.AddCommand( "setspawn", SetSpawn, false );

            Commands.AddCommand( "hide", Hide, false );
            Commands.AddCommand( "unhide", Unhide, false );

            Commands.AddCommand( "say", Say, true );

            Commands.AddCommand( "roll", Roll, true );

            Commands.AddCommand( "d", Dummy, false );
            Commands.AddCommand( "dummy", Dummy, false );

            Commands.AddCommand( "nick", Nick, true );

            Commands.AddCommand( "me", Me, true );

            //commands.AddCommand( "reloadconfig", ReloadConfig. true );
        }

        // broken... for now
        /*void ReloadConfig( Player player, Command cmd ) {
            if( player.Can( Permissions.SaveAndLoad ) ) {
                Config.LoadDefaults();
                Config.Load( "config.xml" );
                Config.ApplyConfig();
            }
        }*/


        internal static void Me( Player player, Command cmd ) {
            string msg = cmd.NextAll().Trim();
            if( msg != null ) {
                Server.SendToAll( "*" + Color.Purple + player.name + " " + msg );
            }
        }

        internal static void Nick( Player player, Command cmd ) {
            if( !player.Can( Permissions.ChangeName ) ) {
                player.NoAccessMessage( Permissions.ChangeName );
                return;
            }

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
                player.Message( "You are now known as " + name + ". Use " + Color.Help + "/nick" + Color.Sys + " again to reset." );
                player.nick = name;
                if( player.world != null ) player.world.UpdatePlayer( player );
            } else {
                player.Message( "Invalid player name." );
            }
        }


        internal static void Dummy( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( Color.Sys, "Usage: " + Color.Help + "/dummy name" );
                return;
            }
            if( !Player.IsValidName( name ) ) {
                player.Message( Color.Sys, "Invalid name format." );
                return;
            }
            Position pos = player.pos;
            Player dummy = new Player( player.world, name );
            dummy.id = player.id + 100;
            player.world.SendToAll( PacketWriter.MakeAddEntity( dummy, pos ), null );
        }


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

        internal static void Say( Player player, Command cmd ) {
            if( player.Can( Permissions.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    Server.SendToAll( Color.Say + msg.Trim() );
                } else {
                    player.Message( "Usage: " + Color.Help + "/say message" );
                }
            } else {
                player.NoAccessMessage( Permissions.Say );
            }
        }


        internal static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd, false, false, false );
        }

        internal static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd, true, false, false );
        }

        internal static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd, true, true, false );
        }

        internal static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd, false, false, true );
        }

        internal static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd, true, false, true );
        }

        internal static void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd, true, true, true );
        }


        internal static void DoBan( Player player, Command cmd, bool banIP, bool banAll, bool unban ) {
            if( !player.Can( Permissions.Ban ) ) {
                player.NoAccessMessage( Permissions.Ban );
                return;
            } else if( banIP && !player.Can( Permissions.BanIP ) ) {
                player.NoAccessMessage( Permissions.BanIP );
                return;
            } else if( banAll && !player.Can( Permissions.BanAll ) ) {
                player.NoAccessMessage( Permissions.BanAll );
                return;
            }

            string arg = cmd.Next();
            string reason = cmd.NextAll();
            IPAddress address;
            Player offender = Server.FindPlayer( arg );
            PlayerInfo info = PlayerDB.FindPlayerInfoExact( arg );

            // ban by IP address
            if( banIP && IPAddress.TryParse( arg, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
            } else if( !unban && offender != null ) {

                // check permissions
                if( !player.info.playerClass.CanBan( offender.info.playerClass ) ) {
                    player.Message( "You can only ban players ranked " + player.info.playerClass.maxBan.color + player.info.playerClass.maxBan.name + Color.Sys + " or lower." );
                    player.Message( offender.GetLogName() + " is ranked " + offender.info.playerClass.name + "." );
                } else {
                    address = offender.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, offender.name, banAll, unban );
                    if( offender.info.ProcessBan( player.name, reason ) ) {
                        Logger.Log( "{0} was banned by {1}.", LogType.UserActivity, offender.info.name, player.GetLogName() );
                        Server.SendToAll( Color.Red + offender.name + " was banned by " + player.nick, offender );
                        offender.session.Kick( "You were just banned by " + player.GetLogName() );
                    } else {
                        player.Message( offender.name + " is already banned." );
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
                        if( info.ProcessUnBan( player.name, reason ) ) {
                            Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity, info.name, player.GetLogName() );
                            Server.SendToAll( Color.Red + info.name + " (offline) was unbanned by " + player.nick );
                        } else {
                            player.Message( info.name + " (offline) is not currenty banned." );
                        }
                    } else {
                        if( info.ProcessBan( player.name, reason ) ) {
                            Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity, info.name, player.GetLogName() );
                            Server.SendToAll( Color.Red + info.name + " (offline) was banned by " + player.nick );
                        } else {
                            player.Message( info.name + " (offline) is already banned." );
                        }
                    }
                }
            } else {
                player.NoPlayerMessage( arg );
                player.Message( "Use the FULL player name for ban/unban commands." );
            }
        }


        internal static void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {
            Player other;
            if( unban ) {
                if( IPBanList.Remove( address ) ) {
                    player.Message( address.ToString() + " has been removed from the IP ban list." );
                } else {
                    player.Message( address.ToString() + " is not currently banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                        if( otherInfo.ProcessUnBan( player.name, reason + "~UnBanAll" ) ) {
                            player.Message( otherInfo.name + " matched the IP and was also unbanned." );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    player.Message( address.ToString() + " has been added to the IP ban list." );

                } else {
                    player.Message( address.ToString() + " is already banned." );
                }
                foreach( PlayerInfo otherInfo in PlayerDB.FindPlayersByIP( address ) ) {
                    if( banAll && otherInfo.ProcessBan( player.name, reason + "~BanAll" ) ) {
                        player.Message( otherInfo.name + " matched the IP and was also banned." );
                    }
                    other = player.world.FindPlayerExact( otherInfo.name );
                    if( other != null ) {
                        other.session.Kick( "Your IP was just banned by " + player.GetLogName() );
                    }
                }
            }
        }


        // Kick a player. One argument (mandatory) - player name (can be partial).
        internal static void Kick( Player player, Command cmd ) {
            if( !player.Can( Permissions.Kick ) ) {
                player.NoAccessMessage( Permissions.Kick );
                return;
            }

            string name = cmd.Next();
            if( name != null ) {
                string msg = cmd.NextAll();
                Player offender = Server.FindPlayer( name );
                if( offender != null ) {
                    if( !player.info.playerClass.CanKick( offender.info.playerClass ) ) {
                        player.Message( "You can only kick players ranked " + player.info.playerClass.maxKick.color + player.info.playerClass.maxKick.name + Color.Sys + " or lower." );
                        player.Message( offender.GetLogName() + " is ranked " + offender.info.playerClass.name + "." );
                    } else {
                        Server.SendToAll( Color.Red + offender.nick + " was kicked by " + player.nick );
                        if( msg != null && msg != "" ) {
                            Logger.Log( "{0} was kicked by {1}. Memo: {2}", LogType.UserActivity, offender.GetLogName(), player.GetLogName(), msg );
                            offender.session.Kick( "Kicked by " + player.GetLogName() + ": " + msg );
                        } else {
                            Logger.Log( "{0} was kicked by {1}", LogType.UserActivity, offender.GetLogName(), player.GetLogName() );
                            offender.session.Kick( "You have been kicked by " + player.GetLogName() );
                        }
                    }
                } else {
                    player.NoPlayerMessage( name );
                }
            } else {
                player.Message( "Usage: " + Color.Help + "/kick PlayerName [Message]" +
                                   Color.Sys + " or " + Color.Help + "/k PlayerName [Message]" );
            }
        }



        // Change player class
        internal static void ChangeClass( Player player, Command cmd ) {
            string name = cmd.Next();
            string newClassName = cmd.Next();
            if( name == null || newClassName == null ) {
                player.Message( "Usage: " + Color.Help + "/user PlayerName ClassName" );
                player.Message( "To see a list of classes and permissions, use " + Color.Help + "/class" );
                return;
            }

            Player target = Server.FindPlayer( name );
            if( target == null ) {
                player.NoPlayerMessage( name );
                return;
            }

            PlayerClass newClass = ClassList.FindClass( newClassName );
            if( newClass == null ) {
                player.Message( "Unrecognized player class: " + newClassName );
                return;
            }

            if( target.info.playerClass == newClass ) {
                player.Message( target.GetLogName() + " is already " + newClass.color + newClass.name );
                return;
            }

            bool promote = target.info.playerClass.rank < newClass.rank;

            if( (promote && !player.Can( Permissions.Promote )) ) {
                player.NoAccessMessage( Permissions.Promote );
                return;
            } else if( !promote && !player.Can( Permissions.Demote ) ) {
                player.NoAccessMessage( Permissions.Demote );
                return;
            }

            if( promote && !player.info.playerClass.CanPromote( newClass ) ) {
                player.Message( "You can only promote players up to " + player.info.playerClass.maxPromote.color + player.info.playerClass.maxPromote.name );
                player.Message( target.GetLogName() + " is ranked " + target.info.playerClass.name + "." );
                return;
            } else if( !promote && !player.info.playerClass.CanDemote( target.info.playerClass ) ) {
                player.Message( "You can only demote players that are " + player.info.playerClass.maxDemote.color + player.info.playerClass.maxDemote.name + Color.Sys + " or lower." );
                player.Message( target.GetLogName() + " is ranked " + target.info.playerClass.name + "." );
                return;
            }

            if( promote && target.info.playerClass.rank < newClass.rank ||
                target.info.playerClass.rank > newClass.rank ) {
                PlayerClass oldClass = target.info.playerClass;
                if( !Server.FirePlayerClassChange( target, player, oldClass, newClass ) ) return;

                Logger.Log( "{0} changed the class of {1} from {2} to {3}.", LogType.UserActivity,
                            player.GetLogName(), target.GetLogName(), target.info.playerClass.name, newClass.name );
                target.info.playerClass = newClass;
                target.info.classChangeDate = DateTime.Now;
                target.info.classChangedBy = player.name;

                Server.FirePlayerListChangedEvent();

                target.Send( PacketWriter.MakeSetPermission( target ) );

                target.mode = BlockPlacementMode.Normal;
                if( promote ) {
                    player.Message( "You promoted " + target.name + " to " + newClass.color + newClass.name );
                    target.Message( "You have been promoted to " + newClass.color + newClass.name + Color.Sys + " by " + player.nick );
                } else {
                    player.Message( "You demoted " + target.name + " to " + newClass.color + newClass.name );
                    target.Message( "You have been demoted to " + newClass.color + newClass.name + Color.Sys + " by " + player.nick );
                }
                if( Config.GetBool( ConfigKey.ClassPrefixesInList ) || Config.GetBool( ConfigKey.ClassColorsInChat ) ) {
                    target.world.UpdatePlayer( target );
                }
            } else {
                if( promote ) {
                    player.Message( target.GetLogName() + " is already same or lower rank than " + newClass.name );
                } else {
                    player.Message( target.GetLogName() + " is already same or higher rank than " + newClass.name );
                }
            }
        }


        internal static void TP( Player player, Command cmd ) {
            if( player.Can( Permissions.Teleport ) ) {
                string name = cmd.Next();
                if( name == null ) {
                    player.Send( PacketWriter.MakeTeleport( 255, player.world.map.spawn ) );
                } else {
                    Player target = player.world.FindPlayer( name );
                    if( target != null ) {
                        Position pos = target.pos;
                        pos.x += 1;
                        pos.y += 1;
                        pos.h += 1;
                        player.Send( PacketWriter.MakeTeleport( 255, pos ) );
                    } else if( cmd.Next() == null ) {
                        player.NoPlayerMessage( name );
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
                            player.Message( "See " + Color.Help + "/help tp" + Color.Sys + " for information on using /tp" );
                        }
                    }
                }
            } else {
                player.NoAccessMessage( Permissions.Teleport );
            }
        }


        internal static void Bring( Player player, Command cmd ) {
            if( player.Can( Permissions.Bring ) ) {
                string name = cmd.Next();
                Player target = player.world.FindPlayer( name );
                if( target != null ) {
                    Position pos = player.pos;
                    pos.x += 1;
                    pos.y += 1;
                    pos.h += 1;
                    target.Send( PacketWriter.MakeTeleport( 255, pos ) );
                } else {
                    player.NoPlayerMessage( name );
                }
            } else {
                player.NoAccessMessage( Permissions.Bring );
            }
        }


        internal static void Freeze( Player player, Command cmd ) {
            if( player.Can( Permissions.Freeze ) ) {
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
            } else {
                player.NoAccessMessage( Permissions.Freeze );
            }
        }


        internal static void Unfreeze( Player player, Command cmd ) {
            if( player.Can( Permissions.Freeze ) ) {
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
            } else {
                player.NoAccessMessage( Permissions.Freeze );
            }
        }



        internal static void Hide( Player player, Command cmd ) {
            if( player.Can( Permissions.Hide ) ) {
                if( !player.isHidden ) {
                    Server.SendToAll( PacketWriter.MakeRemoveEntity( player.id ), null );
                    Server.SendToAll( Color.Sys + player.nick + " left the server." );
                    player.isHidden = true;
                    player.Message( Color.Gray, "You are now hidden." );
                    player.nick = player.name;
                } else {
                    player.Message( "You are already hidden." );
                }
            } else {
                player.NoAccessMessage( Permissions.Hide );
            }
        }


        internal static void Unhide( Player player, Command cmd ) {
            if( player.Can( Permissions.Hide ) ) {
                if( player.isHidden ) {
                    player.Message( Color.Gray, "You are no longer hidden." );
                    if( player.nick != player.name ) {
                        player.nick = player.name;
                        player.Message( "For security reasons, your nick was reset." );
                    }
                    player.world.SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
                    Server.SendToAll( String.Format( "{0}{1} ({2}{3}{0}) has joined the server.",
                                                     Color.Sys,
                                                     player.name,
                                                     player.info.playerClass.color,
                                                     player.info.playerClass.name ),
                                      player );
                    player.isHidden = false;
                } else {
                    player.Message( "You are not currently hidden." );
                }
            } else {
                player.NoAccessMessage( Permissions.Hide );
            }
        }


        internal static void SetSpawn( Player player, Command cmd ) {
            if( player.Can( Permissions.SetSpawn ) ) {
                player.world.map.spawn = player.pos;
                player.world.map.changesSinceSave++;
                player.Send( PacketWriter.MakeTeleport( 255, player.world.map.spawn ), true );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity, player.GetLogName() );
            } else {
                player.NoAccessMessage( Permissions.SetSpawn );
            }
        }
    }
}