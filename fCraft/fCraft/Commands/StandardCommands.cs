// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;


namespace fCraft {
    sealed class StandardCommands {
        World world;

        // Register standard commands.
        internal StandardCommands( World _world, Commands commands ){
            world = _world; 
            commands.AddCommand( "k", Kick, true );
            commands.AddCommand( "kick", Kick, true );

            commands.AddCommand( "ban", Ban, true );
            commands.AddCommand( "banip", BanIP, true );
            commands.AddCommand( "banall", BanAll, true );
            commands.AddCommand( "unban", Unban, true );
            commands.AddCommand( "unbanip", UnbanIP, true );
            commands.AddCommand( "unbanall", UnbanAll, true );

            commands.AddCommand( "user", ChangeClass, true );

            commands.AddCommand( "tp", TP, false );
            commands.AddCommand( "bring", Bring, false );
            commands.AddCommand( "freeze", Freeze, false );
            commands.AddCommand( "unfreeze", Unfreeze, false );
            commands.AddCommand( "setspawn", SetSpawn, false );

            commands.AddCommand( "hide", Hide, false );
            commands.AddCommand( "unhide", Unhide, false );

            commands.AddCommand( "say", Say, true );

            commands.AddCommand( "roll", Roll, true );

            commands.AddCommand( "d", Dummy, false );
            commands.AddCommand( "dummy", Dummy, false );

            commands.AddCommand( "nick", Nick, true );

            commands.AddCommand( "me", Me, true );

            //commands.AddCommand( "reloadconfig", ReloadConfig, true );
        }

        // broken... for now
        /*void ReloadConfig( Player player, Command cmd ) {
            if( player.Can( Permissions.SaveAndLoad ) ) {
                world.config.LoadDefaults();
                world.config.Load( "config.xml" );
                world.config.ApplyConfig();
            }
        }*/


        public void Me( Player player, Command cmd ) {
            string msg = cmd.NextAll().Trim();
            if( msg != null ) {
                world.SendToAll( "*" + Color.Purple + player.name + " " + msg, null );
            }
        }

        void Nick( Player player, Command cmd ) {
            if( !player.Can( Permissions.ChangeName ) ) {
                world.NoAccessMessage( player );
                return;
            }
            string name = cmd.Next();
            if( name == null ) {
                if( player.nick != player.name ) {
                    world.SendToAll( Color.Sys + player.nick + " is now known as " + player.name, player );
                    player.Message( "You are now known as " + name + ". Use " + Color.Help + "/nick" + Color.Sys + " again to reset." );
                    player.nick = player.name;
                    world.UpdatePlayer( player );
                } else {
                    player.Message( "You do not have an alias set." );
                }
            } else if( Player.IsValidName( name ) ) {
                world.SendToAll( Color.Sys + player.nick + " is now known as " + name, player );
                player.Message( "You are now known as " + name + ". Use " + Color.Help + "/nick" + Color.Sys + " again to reset." );
                player.nick = name;
                world.UpdatePlayer( player );
            } else {
                player.Message( "Invalid player name." );
            }
        }

        void Dummy( Player player, Command cmd ) {
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
            Player dummy = new Player( world, name );
            dummy.id = player.id + 100;
            world.SendToAll( PacketWriter.MakeAddEntity( dummy, pos ), null );
        }


        void Roll( Player player, Command cmd ) {
            Random rand = new Random();
            int min = 1, max = 100, num, t1, t2;
            if( cmd.NextInt( out t1 ) ) {
                if( cmd.NextInt( out t2 ) ) {
                    if( t2 >= t1 ) {
                        min = t1;
                        max = t2;
                    }
                } else if( t1 >= 1 ){
                    max = t1;
                }
            }
            num = rand.Next( min, max+1 );
            string msg = Color.Silver + player.name + " rolled " + num + " ("+min+"..."+max+")";
            world.log.LogConsole( msg );
            world.SendToAll( PacketWriter.MakeMessage( msg ), null );
        }

        void Say( Player player, Command cmd ) {
            if( player.Can( Permissions.Say ) ) {
                string msg = cmd.NextAll();
                if( msg != null && msg.Trim().Length > 0 ) {
                    world.SendToAll( PacketWriter.MakeMessage( Color.Say + msg.Trim() ), null );
                } else {
                    player.Message( "Usage: " + Color.Help + "/say message" );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Ban( Player player, Command cmd ) {
            DoBan( player, cmd, false, false, false );
        }

        void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd, true, false, false );
        }

        void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd, true, true, false );
        }

        void Unban( Player player, Command cmd ) {
            DoBan( player, cmd, false, false, true );
        }

        void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd, true, false, true );
        }

        void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd, true, true, true );
        }


        void DoBan( Player player, Command cmd, bool banIP, bool banAll, bool unban ) {
            if( !banAll && !banIP && player.Can( Permissions.Ban ) ||
                !banAll && player.Can( Permissions.BanIP ) ||
                player.Can( Permissions.BanAll ) ) {

                string arg = cmd.Next();
                string reason = cmd.NextAll();
                IPAddress address;
                Player offender = world.FindPlayer( arg );
                PlayerInfo info = world.db.FindPlayerInfoExact( arg );

                // ban by IP address
                if( banIP && IPAddress.TryParse( arg, out address ) ) {
                    if( banIP ) DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
                } else if( !unban && offender != null ) {
                    address = offender.info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, offender.name, banAll, unban );
                    if( unban ) {
                        if( offender.info.ProcessUnBan( player.name, reason ) ) {
                            world.log.Log( "{0} was unbanned by {1}.", LogType.UserActivity, offender.info.name, player.name );
                            world.SendToAll( PacketWriter.MakeMessage( Color.Red + offender.name + " was unbanned by " + player.name ), offender );
                        } else {
                            player.Message( offender.name + " is not currently banned." );
                        }
                    }else{
                        if( offender.info.ProcessBan( player.name, reason ) ) {
                            world.log.Log( "{0} was banned by {1}.", LogType.UserActivity, offender.info.name, player.name );
                            world.SendToAll( PacketWriter.MakeMessage( Color.Red + offender.name + " was banned by " + player.name ), offender );
                            offender.session.Kick( "You were banned by " + player.name + "!" );
                        } else {
                            player.Message( offender.name + " is already banned." );
                        }
                    }

                // ban offline players
                } else if( info != null ) {
                    address = info.lastIP;
                    if( banIP ) DoIPBan( player, address, reason, info.name, banAll, unban );
                    if( unban ) {
                        if( info.ProcessUnBan( player.name, reason ) ) {
                            world.log.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity, info.name, player.name );
                            world.SendToAll( PacketWriter.MakeMessage( Color.Red + info.name + " (offline) was unbanned by " + player.name ), null );
                        } else {
                            player.Message( info.name + " (offline) is not currenty banned." );
                        }
                    } else {
                        if( info.ProcessBan( player.name, reason ) ) {
                            world.log.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity, info.name, player.name );
                            world.SendToAll( PacketWriter.MakeMessage( Color.Red + info.name + " (offline) was banned by " + player.name ), null );
                        } else {
                            player.Message( info.name + " (offline) is already banned." );
                        }
                    }
                } else {
                    world.NoPlayerMessage( player, arg );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void DoIPBan( Player player, IPAddress address, string reason, string playerName, bool banAll, bool unban ) {
            Player other;
            if( unban ) {
                if( world.bans.Remove( address ) ) {
                    player.Message( address.ToString() + " has been removed from the IP ban list." );
                } else {
                    player.Message( address.ToString() + " is not currently banned." );
                }
                if( banAll ) {
                    foreach( PlayerInfo otherInfo in world.db.FindPlayersByIP( address ) ) {
                        if( otherInfo.ProcessUnBan( player.name, reason + "~UnBanAll" ) ) {
                            player.Message( otherInfo.name + " matched the IP and was also unbanned." );
                        }
                    }
                }

            } else {
                if( world.bans.Add( new IPBanInfo( address, playerName, player.name, reason ) ) ) {
                    player.Message( address.ToString() + " has been added to the IP ban list." );

                } else {
                    player.Message( address.ToString() + " is already banned." );
                }
                foreach( PlayerInfo otherInfo in world.db.FindPlayersByIP( address ) ) {
                    if( banAll && otherInfo.ProcessBan( player.name, reason + "~BanAll" ) ) {
                        player.Message( otherInfo.name + " matched the IP and was also banned." );
                    }
                    other = world.FindPlayerExact( otherInfo.name );
                    if( other != null ) {
                        other.session.Kick( "Your IP was just banned by " + player.name );
                    }
                }
            }
        }


        // Kick a player. One argument (mandatory) - player name (can be partial).
        void Kick( Player player, Command cmd ) {
            if( player.Can( Permissions.Kick ) ) {
                string name = cmd.Next();
                if( name != null ) {
                    string msg = cmd.NextAll();
                    Player offender = world.FindPlayer( name );
                    if( offender != null ) {
                        world.SendToAll( PacketWriter.MakeMessage(
                                Color.Red + offender.name + " was kicked by " + player.name ), offender );
                        if( msg != null && msg != ""  ) {
                            world.log.Log( "{0} was kicked by {1}. Message: {2}", LogType.UserActivity, offender.name, player.name, msg );
                            offender.session.Kick( "Kicked by " + player.name + ": " + msg );
                        } else {
                            world.log.Log( "{0} was kicked by {1}", LogType.UserActivity, offender.name, player.name );
                            offender.session.Kick( "You have been kicked by " + player.name );
                        }
                    } else {
                        world.NoPlayerMessage( player, name );
                    }
                } else {
                    player.Message( "Usage: " + Color.Help + "/kick PlayerName [Message]" +
                                       Color.Sys + " or " + Color.Help + "/k PlayerName [Message]" );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }



        // Change player class
        void ChangeClass( Player player, Command cmd ) {
            string name = cmd.Next();
            string newClassName = cmd.Next();
            if( name == null || newClassName == null ) {
                player.Message( "Usage: " + Color.Help + "/user PlayerName ClassName" );
                player.Message( "To see a list of classes and permissions, use " + Color.Help + "/class" );
                return;
            }

            Player target = world.FindPlayer( name );
            if( target == null ) {
                world.NoPlayerMessage( player, name );
                return;
            }

            PlayerClass newClass = world.classes.FindClass( newClassName );
            if( newClass == null ) {
                player.Message( "Unrecognized player class: " + newClassName );
                return;
            }

            if( target.info.playerClass == newClass ) {
                player.Message( target.name + "'s class is already " + newClass.color + newClass.name );
                return;
            }

            bool promote = target.info.playerClass.rank < newClass.rank;

            if( (promote && !player.Can( Permissions.Promote )) || !promote && !player.Can( Permissions.Demote ) ) {
                world.NoAccessMessage( player );
                return;
            }

            if( promote && !player.info.playerClass.CanPromote(newClass) ) {
                PlayerClass maxRank = player.info.playerClass.maxPromote;
                if( maxRank == null ) {
                    player.Message( "You can only promote players up to " + player.info.playerClass.color + player.info.playerClass.name );
                } else {
                    player.Message( "You can only promote players up to " + maxRank.color + maxRank.name );
                }
                return;
            } else if( !promote && !player.info.playerClass.CanDemote(target.info.playerClass) ) {
                PlayerClass maxRank = player.info.playerClass.maxDemote;
                if( maxRank == null ) {
                    player.Message( "You can only demote players that are " + player.info.playerClass.color + player.info.playerClass.name + Color.Sys + " or lower." );
                } else {
                    player.Message( "You can only demote players that are " + maxRank.color + maxRank.name + Color.Sys + " or lower." );
                }
                return;
            }

            if( promote && target.info.playerClass.rank < newClass.rank ||
                target.info.playerClass.rank > newClass.rank ) {
                world.log.Log( "{0} changed the class of {1} from {2} to {3}.", LogType.UserActivity, 
                            player.name, target.info.name, target.info.playerClass.name, newClass.name );
                target.info.playerClass = newClass;
                target.info.classChangeDate = DateTime.Now;
                target.info.classChangedBy = player.name;

                target.Send( PacketWriter.MakeSetPermission( target ) );

                if( promote ) {
                    player.Message( "You promoted " + target.name + " to " + newClass.color + newClass.name + "." );
                    target.Message( "You have been promoted to " + newClass.color + newClass.name + Color.Sys + " by " + player.name + "!" );
                } else {
                    player.Message( "You demoted " + target.name + " to " + newClass.color + newClass.name + "." );
                    target.Message( "You have been demoted to " + newClass.color + newClass.name + Color.Sys + " by " + player.name + "!" );
                }
                if( world.config.GetBool( "ClassPrefixesInList" ) || world.config.GetBool( "ClassColorsInChat" ) ) {// TODO: colors in player names
                    world.UpdatePlayer( target );
                }
                target.mode = BlockPlacementMode.Normal;
            } else {
                if( promote ) {
                    player.Message( target.name + " is already same or lower rank than " + newClass.name );
                } else {
                    player.Message( target.name + " is already same or higher rank than " + newClass.name );
                }
            }
        }


        void TP( Player player, Command cmd ) {
            if( player.Can( Permissions.Teleport ) ) {
                string name = cmd.Next();
                if ( name == null ) {
                    player.Send( PacketWriter.MakeTeleport( 255, world.map.spawn ) );
                } else {
                    Player target = world.FindPlayer( name );
                    if ( target != null ) {
                        Position pos = target.pos;
                        pos.x += 1;
                        pos.y += 1;
                        pos.h += 1;
                        player.Send( PacketWriter.MakeTeleport( 255, pos ) );
                    } else if ( cmd.Next() == null ) {
                        world.NoPlayerMessage( player, name );
                    } else {
                        cmd.Rewind();
                        int x, y, h;
                        if ( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out h ) ) {
                            if ( x < 0 || x > world.map.widthX ||
                                 y < 0 || y > world.map.widthY ||
                                 y < 0 || y > world.map.height ) {
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
                world.NoAccessMessage( player );
            }
        }


        void Bring( Player player, Command cmd ) {
            if( player.Can( Permissions.Bring ) ) {
                string name = cmd.Next();
                Player target = world.FindPlayer( name );
                if( target != null ) {
                    Position pos = player.pos;
                    pos.x += 1;
                    pos.y += 1;
                    pos.h += 1;
                    target.Send( PacketWriter.MakeTeleport( 255, pos ) );
                } else {
                    world.NoPlayerMessage( player, name );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Freeze( Player player, Command cmd ) {
            if( player.Can( Permissions.Freeze ) ) {
                string name = cmd.Next();
                Player target = world.FindPlayer( name );
                if( target != null ) {
                    if( !target.isFrozen ) {
                        world.SendToAll( PacketWriter.MakeMessage( Color.Yellow + target.name + " has been frozen by " + player.name ), null );
                        target.isFrozen = true;
                    } else {
                        player.Message( target.name + " is already frozen." );
                    }
                } else {
                    world.NoPlayerMessage( player, name );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Unfreeze( Player player, Command cmd ) {
            if( player.Can( Permissions.Freeze ) ) {
                string name = cmd.Next();
                Player target = world.FindPlayer( name );
                if( target != null ) {
                    if( target.isFrozen ) {
                        world.SendToAll( PacketWriter.MakeMessage( Color.Yellow + target.name + " is no longer frozen." ), null );
                        target.isFrozen = false;
                    } else {
                        player.Message( target.name + " is currently not frozen." );
                    }
                } else {
                    world.NoPlayerMessage( player, name );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Hide( Player player, Command cmd ) {
            if( player.Can( Permissions.Hide ) ) {
                if( !player.isHidden ) {
                    world.SendToAll( PacketWriter.MakeRemoveEntity( player.id ), null );
                    world.SendToAll( PacketWriter.MakeMessage( Color.Sys + player.name + " left the server." ), null );
                    player.isHidden = true;
                    player.Message( Color.Gray, "You are now hidden." );
                } else {
                    player.Message( "You are already hidden." );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void Unhide( Player player, Command cmd ) {
            if( player.Can( Permissions.Hide ) ) {
                if( player.isHidden ) {
                    world.SendToAll( PacketWriter.MakeAddEntity( player, player.pos ), player );
                    world.SendToAll( PacketWriter.MakeMessage( Color.Sys + player.name + " (" + player.info.playerClass.color +
                                                                  player.info.playerClass.name + Color.Sys + ") has joined the server." ),
                                                                  player );
                    player.isHidden = false;
                    player.Message(Color.Gray, "You are no longer hidden." );
                } else {
                    player.Message( "You are not currently hidden." );
                }
            } else {
                world.NoAccessMessage( player );
            }
        }


        void SetSpawn( Player player, Command cmd ) {
            if( player.Can( Permissions.SetSpawn ) ) {
                world.map.spawn = player.pos;
                world.map.Save();
                world.map.changesSinceBackup++;
                player.Send( PacketWriter.MakeTeleport( 255, world.map.spawn ), true );
                player.Message( "New spawn point saved." );
                world.log.Log( "{0} changed the spawned point.", LogType.UserActivity, player.name );
            } else {
                world.NoAccessMessage( player );
            }
        }
    }
}