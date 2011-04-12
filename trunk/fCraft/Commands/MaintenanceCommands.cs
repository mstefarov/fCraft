// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace fCraft {
    /// <summary>
    /// Several yet-undocumented commands, mostly related to AutoRank.
    /// </summary>
    static class MaintenanceCommands {

        internal static void Init() {
            CommandList.RegisterCommand( cdDumpStats );

            CommandList.RegisterCommand( cdAutoRankReload );
            CommandList.RegisterCommand( cdMassRank );
            CommandList.RegisterCommand( cdAutoRankAll );
            CommandList.RegisterCommand( cdSetInfo );


            CommandList.RegisterCommand( cdReloadConfig );

            CommandList.RegisterCommand( cdShutdown );
            CommandList.RegisterCommand( cdRestart );

            CommandList.RegisterCommand( cdPruneDB );

            CommandList.RegisterCommand( cdImportBans );
            CommandList.RegisterCommand( cdImportRanks );
        }


        #region Stats

        static readonly CommandDescriptor cdDumpStats = new CommandDescriptor {
            Name = "dumpstats",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.Import },
            Help = "Writes out a number of statistics about the server. " +
                   "Only non-banned players active in the last 30 days are counted.",
            Usage = "/dumpstats FileName",
            Handler = DumpStats
        };

        const int TopPlayersToList = 3;

        internal static void DumpStats( Player player, Command cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                cdDumpStats.PrintUsage( player );
                return;
            }

            if( !Paths.TestFile( "dumpstats file", fileName, false, true, false ) ) {
                cdDumpStats.PrintUsage( player );
                return;
            }

            if( Paths.IsProtectedFileName( fileName ) ) {
                player.Message( "You may not use this file." );
                return;
            }

            if( !Paths.Contains( Paths.WorkingPath, fileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            if( Path.HasExtension( fileName ) &&
                !Path.GetExtension( fileName ).Equals( ".txt", StringComparison.OrdinalIgnoreCase ) ) {
                player.Message( "Stats filename must end with .txt" );
                return;
            }

            if( !Paths.TestFile( "dumpstats file", fileName, false, true, false ) ) {
                player.Message( "Cannot create specified file. See log for details." );
                return;
            }

            if( File.Exists( fileName ) && !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "File \"{0}\" already exists. Overwrite?", Path.GetFileName( fileName ) );
                return;
            }

            PlayerInfo[] infos;
            using( FileStream fs = File.Create( fileName ) ) {
                using( StreamWriter writer = new StreamWriter( fs ) ) {
                    infos = PlayerDB.GetPlayerListCopy();
                    if( infos.Length == 0 ) {
                        writer.WriteLine( "{0} (0 players)", "(TOTAL)" );
                        writer.WriteLine();
                    } else {
                        DumpPlayerGroupStats( writer, infos, "(TOTAL)" );
                    }

                    foreach( Rank rank in RankList.Ranks ) {
                        infos = PlayerDB.GetPlayerListCopy( rank );
                        if( infos.Length == 0 ) {
                            writer.WriteLine( "{0} (0 players)", rank.Name );
                            writer.WriteLine();
                        } else {
                            DumpPlayerGroupStats( writer, infos, rank.Name );
                        }
                    }
                }
            }

            player.Message( "Stats saved to \"{0}\"", Path.GetFileName( fileName ) );
        }

        static void DumpPlayerGroupStats( TextWriter writer, PlayerInfo[] infos, string groupName ) {

            RankStats stat = new RankStats();
            foreach( Rank rank2 in RankList.Ranks ) {
                stat.PreviousRank.Add( rank2, 0 );
            }

            infos = infos.Where( info => (DateTime.Now.Subtract( info.LastLoginDate ).TotalDays < 30) ).ToArray();
            infos = infos.Where( info => (!info.Banned) ).ToArray();

            if( infos.Length == 0 ) {
                writer.WriteLine( "{0}: 0 players, 0 banned", groupName );
                writer.WriteLine();
                return;
            }

            for( int i = 0; i < infos.Length; i++ ) {
                stat.TimeSinceFirstLogin += DateTime.Now.Subtract( infos[i].FirstLoginDate );
                stat.TimeSinceLastLogin += DateTime.Now.Subtract( infos[i].LastLoginDate );
                stat.TotalTime += infos[i].TotalTime;
                stat.BlocksBuilt += infos[i].BlocksBuilt;
                stat.BlocksDeleted += infos[i].BlocksDeleted;
                stat.TimesVisited += infos[i].TimesVisited;
                stat.MessagesWritten += infos[i].LinesWritten;
                stat.TimesKicked += infos[i].TimesKicked;
                stat.TimesKickedOthers += infos[i].TimesKickedOthers;
                stat.TimesBannedOthers += infos[i].TimesBannedOthers;
                if( infos[i].Banned ) stat.Banned++;
                if( infos[i].PreviousRank != null ) stat.PreviousRank[infos[i].PreviousRank]++;
            }

            stat.BlockRatio = stat.BlocksBuilt / (double)Math.Max( stat.BlocksDeleted, 1 );
            stat.BlocksChanged = stat.BlocksDeleted + stat.BlocksBuilt;


            stat.TimeSinceFirstLoginMedian = DateTime.Now.Subtract( infos.OrderByDescending( info => info.FirstLoginDate )
                                                    .ElementAt( infos.Length / 2 ).FirstLoginDate );
            stat.TimeSinceLastLoginMedian = DateTime.Now.Subtract( infos.OrderByDescending( info => info.LastLoginDate )
                                                    .ElementAt( infos.Length / 2 ).LastLoginDate );
            stat.TotalTimeMedian = infos.OrderByDescending( info => info.TotalTime ).ElementAt( infos.Length / 2 ).TotalTime;
            stat.BlocksBuiltMedian = infos.OrderByDescending( info => info.BlocksBuilt ).ElementAt( infos.Length / 2 ).BlocksBuilt;
            stat.BlocksDeletedMedian = infos.OrderByDescending( info => info.BlocksDeleted ).ElementAt( infos.Length / 2 ).BlocksDeleted;
            PlayerInfo medianBlocksChangedPlayerInfo = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ElementAt( infos.Length / 2 );
            stat.BlocksChangedMedian = medianBlocksChangedPlayerInfo.BlocksDeleted + medianBlocksChangedPlayerInfo.BlocksBuilt;
            PlayerInfo medianBlockRatioPlayerInfo = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) )
                                                    .ElementAt( infos.Length / 2 );
            stat.BlockRatioMedian = medianBlockRatioPlayerInfo.BlocksBuilt / (double)Math.Max( medianBlockRatioPlayerInfo.BlocksDeleted, 1 );
            stat.TimesVisitedMedian = infos.OrderByDescending( info => info.TimesVisited ).ElementAt( infos.Length / 2 ).TimesVisited;
            stat.MessagesWrittenMedian = infos.OrderByDescending( info => info.LinesWritten ).ElementAt( infos.Length / 2 ).LinesWritten;
            stat.TimesKickedMedian = infos.OrderByDescending( info => info.TimesKicked ).ElementAt( infos.Length / 2 ).TimesKicked;
            stat.TimesKickedOthersMedian = infos.OrderByDescending( info => info.TimesKickedOthers ).ElementAt( infos.Length / 2 ).TimesKickedOthers;
            stat.TimesBannedOthersMedian = infos.OrderByDescending( info => info.TimesBannedOthers ).ElementAt( infos.Length / 2 ).TimesBannedOthers;


            stat.TopTimeSinceFirstLogin = infos.OrderBy( info => info.FirstLoginDate ).ToArray();
            stat.TopTimeSinceLastLogin = infos.OrderBy( info => info.LastLoginDate ).ToArray();
            stat.TopTotalTime = infos.OrderByDescending( info => info.TotalTime ).ToArray();
            stat.TopBlocksBuilt = infos.OrderByDescending( info => info.BlocksBuilt ).ToArray();
            stat.TopBlocksDeleted = infos.OrderByDescending( info => info.BlocksDeleted ).ToArray();
            stat.TopBlocksChanged = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ToArray();
            stat.TopBlockRatio = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) ).ToArray();
            stat.TopTimesVisited = infos.OrderByDescending( info => info.TimesVisited ).ToArray();
            stat.TopMessagesWritten = infos.OrderByDescending( info => info.LinesWritten ).ToArray();
            stat.TopTimesKicked = infos.OrderByDescending( info => info.TimesKicked ).ToArray();
            stat.TopTimesKickedOthers = infos.OrderByDescending( info => info.TimesKickedOthers ).ToArray();
            stat.TopTimesBannedOthers = infos.OrderByDescending( info => info.TimesBannedOthers ).ToArray();


            writer.WriteLine( "{0}: {1} players, {2} banned", groupName, infos.Length, stat.Banned );
            writer.WriteLine( "    TimeSinceFirstLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceFirstLogin.Ticks / infos.Length ).ToCompactString(),
                              stat.TimeSinceFirstLoginMedian.ToCompactString(),
                              stat.TimeSinceFirstLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.FirstLoginDate ).ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.FirstLoginDate ).ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.FirstLoginDate ).ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimeSinceLastLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceLastLogin.Ticks / infos.Length ).ToCompactString(),
                              stat.TimeSinceLastLoginMedian.ToCompactString(),
                              stat.TimeSinceLastLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.LastLoginDate ).ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.LastLoginDate ).ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", DateTime.Now.Subtract( info.LastLoginDate ).ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TotalTime: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TotalTime.Ticks / infos.Length ).ToCompactString(),
                              stat.TotalTimeMedian.ToCompactString(),
                              stat.TotalTime.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTotalTime.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTotalTime.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTotalTime ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksBuilt: {0} mean,  {1} median,  {2} total",
                              stat.BlocksBuilt / infos.Length,
                              stat.BlocksBuiltMedian,
                              stat.BlocksBuilt );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksBuilt ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDeleted: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDeleted / infos.Length,
                              stat.BlocksDeletedMedian,
                              stat.BlocksDeleted );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDeleted ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksChanged: {0} mean,  {1} median,  {2} total",
                              stat.BlocksChanged / infos.Length,
                              stat.BlocksChangedMedian,
                              stat.BlocksChanged );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksChanged.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksChanged.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksChanged ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlockRatio: {0:0.000} mean,  {1:0.000} median",
                              stat.BlockRatio,
                              stat.BlockRatioMedian );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlockRatio.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlockRatio.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlockRatio ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesVisited: {0} mean,  {1} median,  {2} total",
                              stat.TimesVisited / infos.Length,
                              stat.TimesVisitedMedian,
                              stat.TimesVisited );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesVisited.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesVisited.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesVisited ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    MessagesWritten: {0} mean,  {1} median,  {2} total",
                              stat.MessagesWritten / infos.Length,
                              stat.MessagesWrittenMedian,
                              stat.MessagesWritten );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopMessagesWritten.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.LinesWritten, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopMessagesWritten.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.LinesWritten, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopMessagesWritten ) {
                    writer.WriteLine( "        {0,20}  {1}", info.LinesWritten, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKicked: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKicked / (double)infos.Length,
                              stat.TimesKickedMedian,
                              stat.TimesKicked );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKicked.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKicked.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKicked ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKickedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKickedOthers / (double)infos.Length,
                              stat.TimesKickedOthersMedian,
                              stat.TimesKickedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesBannedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesBannedOthers / (double)infos.Length,
                              stat.TimesBannedOthersMedian,
                              stat.TimesBannedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            }
            writer.WriteLine();
        }


        sealed class RankStats {
            public TimeSpan TimeSinceFirstLogin;
            public TimeSpan TimeSinceLastLogin;
            public TimeSpan TotalTime;
            public long BlocksBuilt;
            public long BlocksDeleted;
            public long BlocksChanged;
            public double BlockRatio;
            public long TimesVisited;
            public long MessagesWritten;
            public long TimesKicked;
            public long TimesKickedOthers;
            public long TimesBannedOthers;
            public int Banned;
            public readonly Dictionary<Rank, int> PreviousRank = new Dictionary<Rank, int>();

            public TimeSpan TimeSinceFirstLoginMedian;
            public TimeSpan TimeSinceLastLoginMedian;
            public TimeSpan TotalTimeMedian;
            public int BlocksBuiltMedian;
            public int BlocksDeletedMedian;
            public int BlocksChangedMedian;
            public double BlockRatioMedian;
            public int TimesVisitedMedian;
            public int MessagesWrittenMedian;
            public int TimesKickedMedian;
            public int TimesKickedOthersMedian;
            public int TimesBannedOthersMedian;

            public PlayerInfo[] TopTimeSinceFirstLogin;
            public PlayerInfo[] TopTimeSinceLastLogin;
            public PlayerInfo[] TopTotalTime;
            public PlayerInfo[] TopBlocksBuilt;
            public PlayerInfo[] TopBlocksDeleted;
            public PlayerInfo[] TopBlocksChanged;
            public PlayerInfo[] TopBlockRatio;
            public PlayerInfo[] TopTimesVisited;
            public PlayerInfo[] TopMessagesWritten;
            public PlayerInfo[] TopTimesKicked;
            public PlayerInfo[] TopTimesKickedOthers;
            public PlayerInfo[] TopTimesBannedOthers;
        }

        #endregion


        #region AutoRank

        static readonly CommandDescriptor cdAutoRankAll = new CommandDescriptor {
            Name = "autorankall",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            Help = "If AutoRank is disabled, it can still be called manually using this command.",
            Usage = "/autorankall [silent] [FromRank]",
            Handler = AutoRankAll
        };

        internal static void AutoRankAll( Player player, Command cmd ) {
            bool silent = (cmd.Next() != null);
            string rankName = cmd.Next();
            Rank rank = null;
            if( rankName != null ) {
                rank = RankList.ParseRank( rankName );
                if( rank == null ) {
                    player.NoRankMessage( rankName );
                    return;
                }
            }

            PlayerInfo[] list;
            if( rank == null ) {
                list = PlayerDB.GetPlayerListCopy();
            } else {
                list = PlayerDB.GetPlayerListCopy( rank );
            }
            DoAutoRankAll( player, list, silent, "~AutoRankAll" );
        }

        internal static void DoAutoRankAll( Player player, PlayerInfo[] list, bool silent, string message ) {

            if( player == null ) throw new ArgumentNullException( "player" );
            if( list == null ) throw new ArgumentNullException( "list" );

            if( !AutoRank.HasCriteria ) {
                player.Message( "AutoRankAll: No criteria found." );
                return;
            }

            player.Message( "AutoRankAll: Evaluating {0} players...", list.Length );

            Stopwatch sw = Stopwatch.StartNew();
            int promoted = 0, demoted = 0;
            for( int i = 0; i < list.Length; i++ ) {
                Rank newRank = AutoRank.Check( list[i] );
                if( newRank != null ) {
                    if( newRank > list[i].Rank ) {
                        promoted++;
                    } else if( newRank < list[i].Rank ) {
                        demoted++;
                    }
                    ModerationCommands.DoChangeRank( player, list[i], newRank, message, silent, true );
                }
            }
            sw.Stop();
            player.Message( "AutoRankAll: Worked for {0}ms, {1} players promoted, {2} demoted.", sw.ElapsedMilliseconds, promoted, demoted );
        }



        static readonly CommandDescriptor cdAutoRankReload = new CommandDescriptor {
            Name = "autorankreload",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "",
            Handler = AutoRankReload
        };

        internal static void AutoRankReload( Player player, Command cmd ) {
            AutoRank.Init();
        }

        #endregion


        #region MassRank

        static readonly CommandDescriptor cdMassRank = new CommandDescriptor {
            Name = "massrank",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            Help = "",
            Usage = "/massrank FromRank ToRank [silent]",
            Handler = MassRank
        };

        internal static void MassRank( Player player, Command cmd ) {
            string fromRankName = cmd.Next();
            string toRankName = cmd.Next();
            bool silent = (cmd.Next() != null);
            if( toRankName == null ) {
                cdMassRank.PrintUsage( player );
                return;
            }

            Rank fromRank = RankList.ParseRank( fromRankName );
            if( fromRank == null ) {
                player.NoRankMessage( fromRankName );
                return;
            }

            Rank toRank = RankList.ParseRank( toRankName );
            if( toRank == null ) {
                player.NoRankMessage( toRankName );
                return;
            }

            if( fromRank == toRank ) {
                player.Message( "Ranks must be different" );
                return;
            }

            int playerCount = PlayerDB.CountPlayersByRank( fromRank );
            string verb = (fromRank > toRank ? "demot" : "promot");


            if( !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "About to {0}e {1} players.", verb, playerCount );
                return;
            }

            player.Message( "MassRank: {0}ing {1} players...",
                            verb, playerCount );

            int affected = PlayerDB.MassRankChange( player, fromRank, toRank, silent );
            player.Message( "MassRank: done.", affected );
        }

        #endregion


        #region SetInfo

        static readonly CommandDescriptor cdSetInfo = new CommandDescriptor {
            Name = "setinfo",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Allows direct editing of player information. Editable properties: " +
                   "TimesKicked, PreviousRank, TotalTime, RankChangeType, " +
                   "BanReason, UnbanReason, RankChangeReason, LastKickReason",
            Usage = "/setinfo PlayerName Key Value",
            Handler = SetInfo
        };

        internal static void SetInfo( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string propertyName = cmd.Next();
            string valName = cmd.NextAll();

            if( targetName == null || propertyName == null ) {
                cdSetInfo.PrintUsage( player );
                return;
            }

            PlayerInfo info;
            if( !PlayerDB.FindPlayerInfo( targetName, out info ) ) {
                player.Message( "More than one player found matching \"{0}\"", targetName );
            } else if( info == null ) {
                player.NoPlayerMessage( targetName );
            } else {
                switch( propertyName.ToLower() ) {
                    case "timeskicked":
                        int oldTimesKicked = info.TimesKicked;
                        if( ValidateInt( valName, 0, 1000 ) ) {
                            info.TimesKicked = Int32.Parse( valName );
                            player.Message( "TimesKicked for {0}&S changed from {1} to {2}",
                                            info.GetClassyName(),
                                            oldTimesKicked,
                                            info.TimesKicked );
                        } else {
                            player.Message( "Value not in valid range (0...1000)" );
                        }
                        return;

                    case "previousrank":
                        Rank newPreviousRank = RankList.ParseRank( valName );
                        Rank oldPreviousRank = info.PreviousRank;
                        if( newPreviousRank != null ) {
                            info.PreviousRank = newPreviousRank;
                            player.Message( "PreviousRank for {0}&S changed from {1}&S to {2}",
                                            info.GetClassyName(),
                                            oldPreviousRank.GetClassyName(),
                                            info.PreviousRank.GetClassyName() );
                        } else {
                            player.NoRankMessage( valName );
                        }
                        return;

                    case "totaltime":
                        TimeSpan newTotalTime;
                        TimeSpan oldTotalTime = info.TotalTime;
                        if( TimeSpan.TryParse( valName, out newTotalTime ) ) {
                            info.TotalTime = newTotalTime;
                            player.Message( "TotalTime for {0}&S changed from {1} to {2}",
                                            info.GetClassyName(),
                                            oldTotalTime.ToCompactString(),
                                            info.TotalTime.ToCompactString() );
                        } else {
                            player.Message( "Could not parse time. Expected format: Days.HH:MM:SS" );
                        }
                        return;

                    case "rankchangetype":
                        RankChangeType oldType = info.RankChangeType;
                        foreach( string val in Enum.GetNames( typeof( RankChangeType ) ) ) {
                            if( val.Equals( valName, StringComparison.OrdinalIgnoreCase ) ) {
                                info.RankChangeType = (RankChangeType)Enum.Parse( typeof( RankChangeType ), valName, true );
                                player.Message( "RankChangeType for {0}&S changed from {1} to {2}",
                                                info.GetClassyName(),
                                                oldType,
                                                info.RankChangeType );
                                return;
                            }
                        }
                        player.Message( "Could not parse RankChangeType. Allowed values: {0}",
                                        String.Join( ", ", Enum.GetNames( typeof( RankChangeType ) ) ) );
                        return;

                    case "banreason":
                        string oldBanReason = info.BanReason;
                        info.BanReason = valName;
                        player.Message( "BanReason for {0}&S changed from \"{1}\" to \"{2}\"",
                                        info.GetClassyName(),
                                        oldBanReason,
                                        info.BanReason );
                        return;

                    case "unbanreason":
                        string oldUnbanReason = info.UnbanReason;
                        info.UnbanReason = valName;
                        player.Message( "UnbanReason for {0}&S changed from \"{1}\" to \"{2}\"",
                                        info.GetClassyName(),
                                        oldUnbanReason,
                                        info.UnbanReason );
                        return;

                    case "rankchangereason":
                        string oldRankChangeReason = info.RankChangeReason;
                        info.RankChangeReason = valName;
                        player.Message( "RankChangeReason for {0}&S changed from \"{1}\" to \"{2}\"",
                                        info.GetClassyName(),
                                        oldRankChangeReason,
                                        info.RankChangeReason );
                        return;

                    case "lastkickreason":
                        string oldLastKickReason = info.LastKickReason;
                        info.LastKickReason = valName;
                        player.Message( "LastKickReason for {0}&S changed from \"{1}\" to \"{2}\"",
                                        info.GetClassyName(),
                                        oldLastKickReason,
                                        info.LastKickReason );
                        return;

                    default:
                        player.Message( "Only the following properties are editable: " +
                                        "TimesKicked, PreviousRank, TotalTime, RankChangeType, " +
                                        "BanReason, UnbanReason, RankChangeReason, LastKickReason" );
                        return;
                }
            }
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


        #region ReloadConfig

        static readonly CommandDescriptor cdReloadConfig = new CommandDescriptor {
            Name = "reloadconfig",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ReloadConfig },
            IsConsoleSafe = true,
            Help = "Reloads most of server's configuration file. " +
                   "NOTE: THIS COMMAND IS EXPERIMENTAL! Excludes rank changes and IRC bot settings. " +
                   "Server has to be restarted to change those.",
            Handler = ReloadConfig
        };

        static void ReloadConfig( Player player, Command cmd ) {
            player.Message( "Attempting to reload config..." );
            if( Config.Load( true, true ) ) {
                Config.ApplyConfig();
                player.Message( "Config reloaded." );
            } else {
                player.Message( "An error occured while trying to reload the config. See server log for details." );
            }
        }

        #endregion


        #region Shutdown, Restart

        static readonly CommandDescriptor cdShutdown = new CommandDescriptor {
            Name = "shutdown",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Shuts down the server remotely. " +
                   "The default delay before shutdown is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A shutdown reason or message can be specified to be shown to players. You can also cancel a shutdown-in-progress " +
                   "by calling &H/shutdown abort",
            Usage = "/shutdown [Delay] [Reason]",
            Handler = Shutdown
        };

        static void Shutdown( Player player, Command cmd ) {
            int delay;
            if( !cmd.NextInt( out delay ) ) {
                delay = 5;
                cmd.Rewind();
            }
            string reason = cmd.NextAll();

            if( reason.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                if( Server.CancelShutdown() ) {
                    Logger.Log( "Shutdown aborted by {0}.", LogType.UserActivity, player.Name );
                    Server.SendToAll( "&WShutdown aborted by {0}", player.GetClassyName() );
                } else {
                    player.MessageNow( "Cannot abort shutdown - too late." );
                }
                return;
            }

            Server.SendToAll( "&WServer shutting down in {0} seconds.", delay );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( "{0} shut down the server ({1} second delay).", LogType.UserActivity,
                            player.Name, delay );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delay, true, false );
                Server.Shutdown( sp, false );
            } else {
                Server.SendToAll( "&WShutdown reason: {0}", reason );
                Logger.Log( "{0} shut down the server ({1} second delay). Reason: {2}", LogType.UserActivity,
                            player.Name, delay, reason );
                ShutdownParams sp = new ShutdownParams( reason, delay, true, false, player );
                Server.Shutdown( sp, false );
            }
        }



        static readonly CommandDescriptor cdRestart = new CommandDescriptor {
            Name = "restart",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Restarts the server remotely. " +
                   "The default delay before restart is 5 seconds (can be changed by specifying a custom number of seconds). " +
                   "A restart reason or message can be specified to be shown to players.",
            Usage = "/restart [Delay [Reason]]",
            Handler = Restart
        };

        static void Restart( Player player, Command cmd ) {
            int delay;
            if( !cmd.NextInt( out delay ) ) {
                delay = 5;
                cmd.Rewind();
            }
            string reason = cmd.Next();

            Server.SendToAll( "&WServer restarting in {0} seconds.", delay );

            if( reason == null ) {
                Logger.Log( "{0} restarted the server ({1} second delay).", LogType.UserActivity,
                            player.Name, delay );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delay, true, true );
                Server.Shutdown( sp, false );
            } else {
                Logger.Log( "{0} restarted the server ({1} second delay). Reason: {2}", LogType.UserActivity,
                            player.Name, delay, reason );
                ShutdownParams sp = new ShutdownParams( reason, delay, true, true, player );
                Server.Shutdown( sp, false );
            }
        }

        #endregion


        #region PruneDB

        static readonly CommandDescriptor cdPruneDB = new CommandDescriptor {
            Name = "prunedb",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Removes inactive players from the player database. Use with caution.",
            Handler = PruneDB
        };

        internal static void PruneDB( Player player, Command cmd ) {
            if( !cmd.Confirmed ) {
                player.MessageNow( "PruneDB: Finding inactive players..." );
                player.AskForConfirmation( cmd, "Remove {0} inactive players from the database?",
                                           PlayerDB.CountInactivePlayers() );
                return;
            }
            player.MessageNow( "PruneDB: Removing inactive players... (this may take a while)" );
            Scheduler.AddBackgroundTask( delegate {
                player.MessageNow( "PruneDB: Removed {0} inactive players!", PlayerDB.RemoveInactivePlayers() );
            } ).RunOnce();
        }

        #endregion


        #region Importing

        static readonly CommandDescriptor cdImportBans = new CommandDescriptor {
            Name = "importbans",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.Import, Permission.Ban },
            Usage = "/importbans SoftwareName File",
            Help = "Imports ban list from formats used by other servers. " +
                   "Currently only MCSharp/MCZall files are supported.",
            Handler = ImportBans
        };

        static void ImportBans( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string file = cmd.Next();

            // Make sure all parameters are specified
            if( file == null ) {
                cdImportBans.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: {0}", file );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( "Could not open \"{0}\" to import bans: {1}", LogType.Error,
                                    file,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} bans.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                if( Player.IsValidName( name ) ) {
                    ModerationCommands.DoBan( player, name, reason, false, false, false );
                } else {
                    IPAddress ip;
                    if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                        ModerationCommands.DoIPBan( player, ip, reason, "", false, false );
                    } else {
                        player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                    }
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }



        static readonly CommandDescriptor cdImportRanks = new CommandDescriptor {
            Name = "importranks",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.Import, Permission.Promote, Permission.Demote },
            Usage = "/importranks SoftwareName File RankToAssign",
            Help = "Imports player list from formats used by other servers. " +
                   "All players listed in the specified file are added to PlayerDB with the specified rank. " +
                   "Currently only MCSharp/MCZall files are supported.",
            Handler = ImportRanks
        };

        static void ImportRanks( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            bool silent = (cmd.Next() != null);


            // Make sure all parameters are specified
            if( rankName == null ) {
                cdImportRanks.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            Rank targetRank = RankList.ParseRank( rankName );
            if( targetRank == null ) {
                player.NoRankMessage( rankName );
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
                        Logger.Log( "Could not open \"{0}\" to import ranks: {1}", LogType.Error,
                                    fileName,
                                    ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "fCraft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.Confirmed ) {
                player.AskForConfirmation( cmd, "You are about to import {0} player ranks.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                  PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                ModerationCommands.DoChangeRank( player, info, targetRank, reason, silent, false );
            }

            PlayerDB.Save();
        }

        #endregion
    }
}