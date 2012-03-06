// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Several yet-undocumented commands. </summary>
    static class MaintenanceCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdMassRank );
            CommandManager.RegisterCommand( CdSetInfo );

            CommandManager.RegisterCommand( CdReload );

            CommandManager.RegisterCommand( CdShutdown );
            CommandManager.RegisterCommand( CdRestart );

            //CommandManager.RegisterCommand( CdPruneDB );

            CommandManager.RegisterCommand( CdImport );

            CommandManager.RegisterCommand( CdInfoSwap );

#if DEBUG
            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BUM",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "Bandwidth Use Mode statistics.",
                Handler = delegate( Player player, CommandReader cmd ) {
                    string newModeName = cmd.Next();
                    if( newModeName == null ) {
                        player.Message( "{0}: S: {1}  R: {2}  S/s: {3:0.0}  R/s: {4:0.0}",
                                        player.BandwidthUseMode,
                                        player.BytesSent,
                                        player.BytesReceived,
                                        player.BytesSentRate,
                                        player.BytesReceivedRate );
                    } else {
                        var newMode = (BandwidthUseMode)Enum.Parse( typeof( BandwidthUseMode ), newModeName, true );
                        player.BandwidthUseMode = newMode;
                        player.Info.BandwidthUseMode = newMode;
                    }
                }
            } );

            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BDBDB",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "BlockDB Debug",
                Handler = delegate( Player player, CommandReader cmd ) {
                    if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
                    BlockDB db = player.World.BlockDB;
                    lock( db.SyncRoot ) {
                        player.Message( "BlockDB: CAP={0} SZ={1} FI={2}",
                                        db.CacheCapacity, db.CacheSize, db.LastFlushedIndex );
                    }
                }
            } );
#endif
        }


        #region MassRank

        static readonly CommandDescriptor CdMassRank = new CommandDescriptor {
            Name = "MassRank",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            Help = "",
            Usage = "/MassRank FromRank ToRank Reason",
            Handler = MassRankHandler
        };

        static void MassRankHandler( Player player, CommandReader cmd ) {
            string fromRankName = cmd.Next();
            string toRankName = cmd.Next();
            string reason = cmd.NextAll();
            if( fromRankName == null || toRankName == null ) {
                CdMassRank.PrintUsage( player );
                return;
            }

            Rank fromRank = RankManager.FindRank( fromRankName );
            if( fromRank == null ) {
                player.MessageNoRank( fromRankName );
                return;
            }

            Rank toRank = RankManager.FindRank( toRankName );
            if( toRank == null ) {
                player.MessageNoRank( toRankName );
                return;
            }

            if( fromRank == toRank ) {
                player.Message( "Ranks must be different" );
                return;
            }

            int playerCount;
            using( PlayerDB.GetReadLock() ) {
                playerCount = PlayerDB.List.Count( t => t.Rank == fromRank );
            }
            string verb = (fromRank > toRank ? "demot" : "promot");

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "MassRank: {0}e {1} players?", verb.UppercaseFirst(), playerCount );
                return;
            }

            player.Message( "MassRank: {0}ing {1} players...",
                            verb, playerCount );

            int affected = PlayerDB.MassRankChange( player, fromRank, toRank, reason );
            player.Message( "MassRank: done, {0} records affected.", affected );
        }

        #endregion


        #region SetInfo

        static readonly CommandDescriptor CdSetInfo = new CommandDescriptor {
            Name = "SetInfo",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Allows direct editing of players' database records. List of editable properties: " +
                   "BanReason, DisplayedName, KickReason, Name, PreviousRank, RankChangeType, " +
                   "RankReason, TimesKicked, TotalTime, UnbanReason. For detailed help see &H/Help SetInfo <Property>",
            HelpSections = new Dictionary<string, string>{
                { "banreason",      "&H/SetInfo <PlayerName> BanReason <Reason>\n&S" +
                                    "Changes ban reason for the given player. Original ban reason is preserved in the logs." },
                { "displayedname",  "&H/SetInfo <RealPlayerName> DisplayedName <DisplayedName>\n&S" +
                                    "Sets or resets the way player's name is displayed in chat. "+
                                    "Any printable symbols or color codes may be used in the displayed name. "+
                                    "Note that player's real name is still used in logs and on the in-game player list. "+
                                    "To remove a custom name, type \"&H/SetInfo <RealName> DisplayedName&S\" (omit the name)." },
                { "kickreason",     "&H/SetInfo <PlayerName> KickReason <Reason>\n&S" +
                                    "Changes reason of most-recent kick for the given player. " +
                                    "Original kick reason is preserved in the logs." },
                { "name",           "&H/SetInfo <PlayerName> Name <Name>\n&S" +
                                    "Changes capitalization of player's name." },
                { "previousrank",   "&H/SetInfo <PlayerName> PreviousRank <RankName>\n&S" +
                                    "Changes previous rank held by the player. " +
                                    "To reset previous rank to \"none\" (will show as \"default\" in &H/Info&S), " +
                                    "type \"&H/SetInfo <Name> PreviousRank&S\" (omit the rank name)." },
                { "rankchangetype", "&H/SetInfo <PlayerName> RankChangeType <Type>\n&S" +
                                    "Sets the type of rank change. <Type> can be: Promoted, Demoted, AutoPromoted, AutoDemoted." },
                { "rankreason",     "&H/SetInfo <PlayerName> RankReason <Reason>\n&S" +
                                    "Changes promotion/demotion reason for the given player. "+
                                    "Original promotion/demotion reason is preserved in the logs." },
                { "timeskicked",    "&H/SetInfo <PlayerName> TimesKicked <#>\n&S" +
                                    "Changes the number of times that a player has been kicked. "+
                                    "Acceptable value range: 0-9999" },
                { "totaltime",      "&H/SetInfo <PlayerName> TotalTime <Time>\n&S" +
                                    "Changes the amount of game time that the player has on record. " +
                                    "Accepts values in the common compact time-span format." },
                { "unbanreason",    "&H/SetInfo <PlayerName> UnbanReason <Reason>\n&S" +
                                    "Changes unban reason for the given player. " +
                                    "Original unban reason is preserved in the logs." }
            },
            Usage = "/SetInfo <PlayerName> <Property> <Value>",
            Handler = SetInfoHandler
        };

        static void SetInfoHandler( Player player, CommandReader cmd ) {
            string targetName = cmd.Next();
            string propertyName = cmd.Next();
            string valName = cmd.NextAll();

            if( targetName == null || propertyName == null ) {
                CdSetInfo.PrintUsage( player );
                return;
            }

            PlayerInfo info = PlayerDB.FindByPartialNameOrPrintMatches( player, targetName );
            if( info == null ) return;

            switch( propertyName.ToLower() ) {
                case "banreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "BanReason", info, info.BanReason, valName ) ) {
                        info.BanReason = valName;
                    }
                    break;

                case "displayedname":
                    string oldDisplayedName = info.DisplayedName;
                    if( valName.Length == 0 ) valName = null;
                    if( valName == info.DisplayedName ) {
                        if( valName == null ) {
                            player.Message( "SetInfo: DisplayedName for {0} is not set.",
                                            info.Name );
                        } else {
                            player.Message( "SetInfo: DisplayedName for {0} is already set to \"{1}&S\"",
                                            info.Name,
                                            valName );
                        }
                        break;
                    }
                    info.DisplayedName = valName;

                    if( oldDisplayedName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} set to \"{1}&S\"",
                                        info.Name,
                                        valName );
                    } else if( valName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} was reset (was \"{1}&S\")",
                                        info.Name,
                                        oldDisplayedName );
                    } else {
                        player.Message( "SetInfo: DisplayedName for {0} changed from \"{1}&S\" to \"{2}&S\"",
                                        info.Name,
                                        oldDisplayedName,
                                        valName );
                    }
                    break;

                case "kickreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "KickReason", info, info.LastKickReason, valName ) ) {
                        info.LastKickReason = valName;
                    }
                    break;

                case "name":
                    if( valName.Equals( info.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        player.Message( "SetInfo: You may change capitalization of player's real name. " +
                                        "If you'd like to make other changes to the way player's name is displayed, " +
                                        "use &H/SetInfo <Name> DisplayedName <NewName>" );
                        break;
                    }
                    string oldName = info.Name;
                    if( oldName != valName ) {
                        info.Name = valName;
                        player.Message( "Name capitalization changed from \"{0}\" to \"{1}\"",
                                        oldName, valName );
                    } else {
                        player.Message( "Name capitalization is already \"{0}\"", oldName );
                    }
                    break;

                case "previousrank":
                    Rank newPreviousRank;
                    if( valName.Length > 0 ) {
                        newPreviousRank = RankManager.FindRank( valName );
                        if( newPreviousRank == null ) {
                            player.MessageNoRank( valName );
                            break;
                        }
                    } else {
                        newPreviousRank = null;
                    }

                    Rank oldPreviousRank = info.PreviousRank;

                    if( newPreviousRank == oldPreviousRank ) {
                        if( newPreviousRank == null ) {
                            player.Message( "SetInfo: PreviousRank for {0}&S is not set.",
                                            info.ClassyName );
                        } else {
                            player.Message( "SetInfo: PreviousRank for {0}&S is already set to {1}",
                                            info.ClassyName,
                                            newPreviousRank.ClassyName );
                        }
                        break;
                    }

                    if( oldPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S set to {1}&",
                                        info.ClassyName,
                                        newPreviousRank.ClassyName );
                    } else if( newPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S was reset (was {1}&S)",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName );
                    } else {
                        player.Message( "SetInfo: PreviousRank for {0}&S changed from {1}&S to {2}",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName,
                                        newPreviousRank.ClassyName );
                    }
                    break;

                case "rankchangetype":
                    RankChangeType oldType = info.RankChangeType;
                    try {
                        info.RankChangeType = (RankChangeType)Enum.Parse( typeof( RankChangeType ), valName, true );
                    } catch( ArgumentException ) {
                        player.Message( "SetInfo: Could not parse RankChangeType. Allowed values: {0}",
                                        String.Join( ", ", Enum.GetNames( typeof( RankChangeType ) ) ) );
                        break;
                    }
                    player.Message( "SetInfo: RankChangeType for {0}&S changed from {1} to {2}",
                                    info.ClassyName,
                                    oldType,
                                    info.RankChangeType );
                    break;

                case "rankreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "RankReason", info, info.RankChangeReason, valName ) ) {
                        info.RankChangeReason = valName;
                    }
                    break;

                case "timeskicked":
                    int oldTimesKicked = info.TimesKicked;
                    if( ValidateInt( valName, 0, 9999 ) ) {
                        info.TimesKicked = Int32.Parse( valName );
                        player.Message( "SetInfo: TimesKicked for {0}&S changed from {1} to {2}",
                                        info.ClassyName,
                                        oldTimesKicked,
                                        info.TimesKicked );
                    } else {
                        player.Message( "SetInfo: TimesKicked value out of range (Acceptable value range: 0-9999)" );
                    }
                    break;

                case "totaltime":
                    TimeSpan newTotalTime;
                    TimeSpan oldTotalTime = info.TotalTime;
                    if( valName.TryParseMiniTimespan( out newTotalTime ) ) {
                        if( newTotalTime > DateTimeUtil.MaxTimeSpan ) {
                            player.MessageMaxTimeSpan();
                            break;
                        }
                        info.TotalTime = newTotalTime;
                        player.Message( "SetInfo: TotalTime for {0}&S changed from {1} ({2}) to {3} ({4})",
                                        info.ClassyName,
                                        oldTotalTime.ToMiniString(),
                                        oldTotalTime.ToCompactString(),
                                        info.TotalTime.ToMiniString(),
                                        info.TotalTime.ToCompactString() );
                    } else {
                        player.Message( "SetInfo: Could not parse value given for TotalTime." );
                    }
                    break;

                case "unbanreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "UnbanReason", info, info.UnbanReason, valName ) ) {
                        info.UnbanReason = valName;
                    }
                    break;

                default:
                    player.Message( "Only the following properties are editable: " +
                                    "TimesKicked, PreviousRank, TotalTime, RankChangeType, " +
                                    "BanReason, UnbanReason, RankReason, KickReason, DisplayedName" );
                    return;
            }
        }

        static bool SetPlayerInfoField( [NotNull] Player player, [NotNull] string fieldName, [NotNull] IClassy info,
                                        [CanBeNull] string oldValue, [CanBeNull] string newValue ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fieldName == null ) throw new ArgumentNullException( "fieldName" );
            if( info == null ) throw new ArgumentNullException( "info" );
            if( newValue == oldValue ) {
                if( newValue == null ) {
                    player.Message( "SetInfo: {0} for {1}&S is not set.",
                                    fieldName, info.ClassyName );
                } else {
                    player.Message( "SetInfo: {0} for {1}&S is already set to \"{2}&S\"",
                                    fieldName, info.ClassyName, oldValue );
                }
                return false;
            }

            if( oldValue == null  ) {
                player.Message( "SetInfo: {0} for {1}&S set to \"{2}&S\"",
                                fieldName, info.ClassyName, newValue );
            } else if( newValue == null ) {
                player.Message( "SetInfo: {0} for {1}&S was reset (was \"{2}&S\")",
                                fieldName, info.ClassyName, oldValue );
            } else {
                player.Message( "SetInfo: {0} for {1}&S changed from \"{2}&S\" to \"{3}&S\"",
                                fieldName, info.ClassyName,
                                oldValue, newValue );
            }
            return true;
        }

        static bool ValidateInt( string stringVal, int min, int max ) {
            int val;
            if( Int32.TryParse( stringVal, out val ) ) {
                return (val >= min && val <= max);
            } else {
                return false;
            }
        }

        #endregion


        #region Reload

        static readonly CommandDescriptor CdReload = new CommandDescriptor {
            Name = "Reload",
            Aliases = new[] { "configreload", "reloadconfig" },
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ReloadConfig },
            IsConsoleSafe = true,
            Usage = "/Reload config or salt",
            Help = "Reloads a given configuration file or setting. "+
                   "Config note: changes to ranks and IRC settings still require a full restart. "+
                   "Salt note: Until server synchronizes with Minecraft.net, " +
                   "connecting players may have trouble verifying names.",
            Handler = ReloadHandler
        };

        static void ReloadHandler( Player player, CommandReader cmd ) {
            string whatToReload = cmd.Next();
            if( whatToReload == null ) {
                CdReload.PrintUsage( player );
                return;
            }

            whatToReload = whatToReload.ToLower();

            using( LogRecorder rec = new LogRecorder() ) {
                bool success;

                switch( whatToReload ) {
                    case "config":
                        try {
                            Config.Reload( false );
                            success = true;
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error, "Error reloading config: {0}", ex );
                            player.Message( "An error occurred while trying to reload config: {0}: {1}", ex.GetType().Name, ex.Message );
                            success = false;
                        }
                        break;

                    case "salt":
                        Heartbeat.Salt = Server.GetRandomString( 32 );
                        player.Message( "&WNote: Until server synchronizes with Minecraft.net, " +
                                        "connecting players may have trouble verifying names." );
                        success = true;
                        break;

                    default:
                        CdReload.PrintUsage( player );
                        return;
                }

                if( rec.HasMessages ) {
                    foreach( string msg in rec.MessageList ) {
                        player.Message( msg );
                    }
                }

                if( success ) {
                    player.Message( "Reload: reloaded {0}.", whatToReload );
                } else {
                    player.Message( "&WReload: Error(s) occurred while reloading {0}.", whatToReload );
                }
            }
        }

        #endregion


        #region Shutdown, Restart

        static readonly CommandDescriptor CdShutdown = new CommandDescriptor {
            Name = "Shutdown",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Shuts down the server remotely after a given delay. " +
                   "A shutdown reason or message can be specified to be shown to players. " +
                   "Type &H/Shutdown abort&S to cancel.",
            Usage = "/Shutdown Delay [Reason]&S or &H/Shutdown abort",
            Handler = ShutdownHandler
        };

        static readonly TimeSpan DefaultShutdownTime = TimeSpan.FromSeconds( 5 );

        static void ShutdownHandler( Player player, CommandReader cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Shutdown aborted by {0}.", player.Name );
                        Server.Message( "&WShutdown aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort shutdown - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimespan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "WShutdown: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer shutting down in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delayTime, true, false );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&SShutdown reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delayTime, true, false, reason, player );
                Server.Shutdown( sp, false );
            }
        }



        static readonly CommandDescriptor CdRestart = new CommandDescriptor {
            Name = "Restart",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Restarts the server remotely after a given delay. " +
                   "A restart reason or message can be specified to be shown to players. " +
                   "Type &H/Restart abort&S to cancel.",
            Usage = "/Restart Delay [Reason]&S or &H/Restart abort",
            Handler = RestartHandler
        };

        static void RestartHandler( Player player, CommandReader cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Restart aborted by {0}.", player.Name );
                        Server.Message( "&WRestart aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort restart - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimespan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "Restart: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer restarting in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.Restarting, delayTime, true, true );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&WRestart reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.Restarting, delayTime, true, true, reason, player );
                Server.Shutdown( sp, false );
            }
        }

        #endregion


        #region PruneDB

        /*static readonly CommandDescriptor CdPruneDB = new CommandDescriptor {
            Name = "PruneDB",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Removes inactive players from the player database. Use with caution.",
            Handler = PruneDBHandler
        };

        static void PruneDBHandler( Player player, Command cmd ) {
            if( !cmd.IsConfirmed ) {
                player.MessageNow( "PruneDB: Finding inactive players..." );
                int inactivePlayers = PlayerDB.CountInactivePlayers();
                if( inactivePlayers == 0 ) {
                    player.Message( "PruneDB: No inactive players found." );
                } else {
                    player.Confirm( cmd, "PruneDB: Erase {0} records of inactive players?",
                                    inactivePlayers );
                }
            } else {
                Scheduler.NewBackgroundTask( PruneDBTask, player ).RunOnce();
            }
        }


        static void PruneDBTask( SchedulerTask task ) {
            int removedCount = PlayerDB.RemoveInactivePlayers();
            Player player = (Player)task.UserState;
            player.Message( "PruneDB: Removed {0} inactive players!", removedCount );
        }*/

        #endregion


        #region Importing

        static readonly CommandDescriptor CdImport = new CommandDescriptor {
            Name = "Import",
            Aliases = new[] { "importbans", "importranks" },
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Import },
            Usage = "/Import bans Software File&S or &H/Import ranks Software File Rank",
            Help = "Imports data from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            Handler = ImportHandler
        };

        static void ImportHandler( Player player, CommandReader cmd ) {
            string action = cmd.Next();
            if( action == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            switch( action.ToLower() ) {
                case "bans":
                    if( !player.Can( Permission.Ban ) ) {
                        player.MessageNoAccess( Permission.Ban );
                        return;
                    }
                    ImportBans( player, cmd );
                    break;

                case "ranks":
                    if( !player.Can( Permission.Promote ) ) {
                        player.MessageNoAccess( Permission.Promote );
                        return;
                    }
                    ImportRanks( player, cmd );
                    break;

                default:
                    CdImport.PrintUsage( player );
                    break;
            }
        }


        static void ImportBans( Player player, CommandReader cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();

            // Make sure all parameters are specified
            if( serverName == null || fileName == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Could not open \"{0}\" to import bans: {1}",
                                    fileName, ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "Import {0} bans from \"{1}\"?",
                                     names.Length, Path.GetFileName( fileName ) );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    PlayerInfo info = PlayerDB.FindExact( name ) ??
                                      PlayerDB.AddUnrecognizedPlayer( name, RankChangeType.Default );
                    info.Ban( player, reason, true, true );

                } else {
                    IPAddress ip;
                    if( IPAddressUtil.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                        ip.BanIP( player, reason, true, true );
                    } else {
                        player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                    }
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }


        static void ImportRanks( Player player, CommandReader cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            bool silent = (cmd.Next() != null);


            // Make sure all parameters are specified
            if( serverName == null || fileName == null || rankName == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            Rank targetRank = RankManager.FindRank( rankName );
            if( targetRank == null ) {
                player.MessageNoRank( rankName );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Could not open \"{0}\" to import ranks: {1}",
                                    fileName, ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "Import {0} player ranks from \"{1}\"?",
                                     names.Length, Path.GetFileName( fileName ) );
                return;
            }

            string reason = "(Import from " + serverName + ")";
            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindExact( name ) ??
                                  PlayerDB.AddUnrecognizedPlayer( name, RankChangeType.Promoted );
                try {
                    info.ChangeRank( player, targetRank, reason, !silent, true, false );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            }

            PlayerDB.Save();
        }

        #endregion


        static readonly CommandDescriptor CdInfoSwap = new CommandDescriptor {
            Name = "InfoSwap",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/InfoSwap Player1 Player2",
            Help = "Swaps records between two players. EXPERIMENTAL, use at your own risk.",
            Handler = InfoSwapHandler
        };

        static void InfoSwapHandler( Player player, CommandReader cmd ) {
            string p1Name = cmd.Next();
            string p2Name = cmd.Next();
            if( p1Name == null || p2Name == null ) {
                CdInfoSwap.PrintUsage( player );
                return;
            }

            PlayerInfo p1 = PlayerDB.FindByPartialNameOrPrintMatches( player, p1Name );
            if( p1 == null ) return;
            PlayerInfo p2 = PlayerDB.FindByPartialNameOrPrintMatches( player, p2Name );
            if( p2 == null ) return;

            if( p1 == p2 ) {
                player.Message( "InfoSwap: Please specify 2 different players." );
                return;
            }

            if( p1.IsOnline || p2.IsOnline ) {
                player.Message( "InfoSwap: Both players must be offline to swap info." );
                return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "InfoSwap: Swap stats of players {0}&S and {1}&S?", p1.ClassyName, p2.ClassyName );
            } else {
                PlayerDB.SwapPlayerInfo( p1, p2 );
                player.Message( "InfoSwap: Stats of {0}&S and {1}&S have been swapped.",
                                p1.ClassyName, p2.ClassyName );
            }
        }
    }
}