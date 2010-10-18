using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;


namespace fCraft {
    class AutoRankCommands {
        internal static void Init() {
            CommandList.RegisterCommand( cdAutoRankTest );
            CommandList.RegisterCommand( cdAutoRankReload );
            CommandList.RegisterCommand( cdMassRank );
            CommandList.RegisterCommand( cdAutoRankAll );
            CommandList.RegisterCommand( cdDumpStats );
            CommandList.RegisterCommand( cdEditPlayerInfo );
        }


        static CommandDescriptor cdDumpStats = new CommandDescriptor {
            name = "dumpstats",
            consoleSafe = true,
            permissions = new Permission[] { Permission.Import },
            help = "",
            handler = DumpStats
        };

        const int TopPlayersToList = 3;

        internal static void DumpStats( Player player, Command cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                cdDumpStats.PrintUsage( player );
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
        }

        static void DumpPlayerGroupStats( StreamWriter writer, PlayerInfo[] infos, string groupName ) {

            RankStats stat = new RankStats();
            foreach( Rank rank2 in RankList.Ranks ) {
                stat.PreviousRank.Add( rank2, 0 );
            }

            infos = infos.Where( ( info ) => (DateTime.Now.Subtract( info.lastLoginDate ).TotalDays < 30) ).ToArray();

            for( int i = 0; i < infos.Length; i++ ) {
                stat.TimeSinceFirstLogin += DateTime.Now.Subtract( infos[i].firstLoginDate );
                stat.TimeSinceLastLogin += DateTime.Now.Subtract( infos[i].lastLoginDate );
                stat.TotalTime += infos[i].totalTime;
                stat.BlocksBuilt += infos[i].blocksBuilt;
                stat.BlocksDeleted += infos[i].blocksDeleted;
                stat.TimesVisited += infos[i].timesVisited;
                stat.MessagesWritten += infos[i].linesWritten;
                stat.TimesKicked += infos[i].timesKicked;
                stat.TimesKickedOthers += infos[i].timesKickedOthers;
                stat.TimesBannedOthers += infos[i].timesBannedOthers;
                if( infos[i].banned ) stat.Banned++;
                if( infos[i].previousRank != null ) stat.PreviousRank[infos[i].previousRank]++;
            }

            stat.BlockRatio = stat.BlocksBuilt / (double)Math.Max( stat.BlocksDeleted, 1 );
            stat.BlocksChanged = stat.BlocksDeleted + stat.BlocksBuilt;


            stat.TimeSinceFirstLoginMedian = DateTime.Now.Subtract( infos.OrderByDescending( ( info ) => info.firstLoginDate )
                                                    .ElementAt( infos.Length / 2 ).firstLoginDate );
            stat.TimeSinceLastLoginMedian = DateTime.Now.Subtract( infos.OrderByDescending( ( info ) => info.lastLoginDate )
                                                    .ElementAt( infos.Length / 2 ).lastLoginDate );
            stat.TotalTimeMedian = infos.OrderByDescending( ( info ) => info.totalTime ).ElementAt( infos.Length / 2 ).totalTime;
            stat.BlocksBuiltMedian = infos.OrderByDescending( ( info ) => info.blocksBuilt ).ElementAt( infos.Length / 2 ).blocksBuilt;
            stat.BlocksDeletedMedian = infos.OrderByDescending( ( info ) => info.blocksDeleted ).ElementAt( infos.Length / 2 ).blocksDeleted;
            PlayerInfo medianBlocksChangedPlayerInfo = infos.OrderByDescending( ( info ) => (info.blocksDeleted + info.blocksBuilt) ).ElementAt( infos.Length / 2 );
            stat.BlocksChangedMedian = medianBlocksChangedPlayerInfo.blocksDeleted + medianBlocksChangedPlayerInfo.blocksBuilt;
            PlayerInfo medianBlockRatioPlayerInfo = infos.OrderByDescending( ( info ) => (info.blocksBuilt / (double)Math.Max( info.blocksDeleted, 1 )) )
                                                    .ElementAt( infos.Length / 2 );
            stat.BlockRatioMedian = medianBlockRatioPlayerInfo.blocksBuilt / (double)Math.Max( medianBlockRatioPlayerInfo.blocksDeleted, 1 );
            stat.TimesVisitedMedian = infos.OrderByDescending( ( info ) => info.timesVisited ).ElementAt( infos.Length / 2 ).timesVisited;
            stat.MessagesWrittenMedian = infos.OrderByDescending( ( info ) => info.linesWritten ).ElementAt( infos.Length / 2 ).linesWritten;
            stat.TimesKickedMedian = infos.OrderByDescending( ( info ) => info.timesKicked ).ElementAt( infos.Length / 2 ).timesKicked;
            stat.TimesKickedOthersMedian = infos.OrderByDescending( ( info ) => info.timesKickedOthers ).ElementAt( infos.Length / 2 ).timesKickedOthers;
            stat.TimesBannedOthersMedian = infos.OrderByDescending( ( info ) => info.timesBannedOthers ).ElementAt( infos.Length / 2 ).timesBannedOthers;


            stat.TopTimeSinceFirstLogin = infos.OrderBy( ( info ) => info.firstLoginDate ).ToArray();
            stat.TopTimeSinceLastLogin = infos.OrderBy( ( info ) => info.lastLoginDate ).ToArray();
            stat.TopTotalTime = infos.OrderByDescending( ( info ) => info.totalTime ).ToArray();
            stat.TopBlocksBuilt = infos.OrderByDescending( ( info ) => info.blocksBuilt ).ToArray();
            stat.TopBlocksDeleted = infos.OrderByDescending( ( info ) => info.blocksDeleted ).ToArray();
            stat.TopBlocksChanged = infos.OrderByDescending( ( info ) => (info.blocksDeleted + info.blocksBuilt) ).ToArray();
            stat.TopBlockRatio = infos.OrderByDescending( ( info ) => (info.blocksBuilt / (double)Math.Max( info.blocksDeleted, 1 )) ).ToArray();
            stat.TopTimesVisited = infos.OrderByDescending( ( info ) => info.timesVisited ).ToArray();
            stat.TopMessagesWritten = infos.OrderByDescending( ( info ) => info.linesWritten ).ToArray();
            stat.TopTimesKicked = infos.OrderByDescending( ( info ) => info.timesKicked ).ToArray();
            stat.TopTimesKickedOthers = infos.OrderByDescending( ( info ) => info.timesKickedOthers ).ToArray();
            stat.TopTimesBannedOthers = infos.OrderByDescending( ( info ) => info.timesBannedOthers ).ToArray();


            writer.WriteLine( "{0}: {1} players, {2} banned", groupName, infos.Length, stat.Banned );
            writer.WriteLine( "    TimeSinceFirstLogin: {0} mean,  {1} median,  {2} total",
                              FormatTimeSpan( TimeSpan.FromTicks( stat.TimeSinceFirstLogin.Ticks / infos.Length ) ),
                              FormatTimeSpan( stat.TimeSinceFirstLoginMedian ),
                              FormatTimeSpan( stat.TimeSinceFirstLogin ) );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.firstLoginDate ) ), info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.firstLoginDate ) ), info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.firstLoginDate ) ), info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimeSinceLastLogin: {0} mean,  {1} median,  {2} total",
                              FormatTimeSpan( TimeSpan.FromTicks( stat.TimeSinceLastLogin.Ticks / infos.Length ) ),
                              FormatTimeSpan( stat.TimeSinceLastLoginMedian ),
                              FormatTimeSpan( stat.TimeSinceLastLogin ) );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.lastLoginDate ) ), info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.lastLoginDate ) ), info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( DateTime.Now.Subtract( info.lastLoginDate ) ), info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TotalTime: {0} mean,  {1} median,  {2} total",
                              FormatTimeSpan( TimeSpan.FromTicks( stat.TotalTime.Ticks / infos.Length ) ),
                              FormatTimeSpan( stat.TotalTimeMedian ),
                              FormatTimeSpan( stat.TotalTime ) );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTotalTime.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( info.totalTime ), info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTotalTime.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( info.totalTime ), info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTotalTime ) {
                    writer.WriteLine( "        {0,20}  {1}", FormatTimeSpan( info.totalTime ), info.name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksBuilt: {0} mean,  {1} median,  {2} total",
                              stat.BlocksBuilt / infos.Length,
                              stat.BlocksBuiltMedian,
                              stat.BlocksBuilt );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksBuilt, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksBuilt, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksBuilt ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksBuilt, info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDeleted: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDeleted / infos.Length,
                              stat.BlocksDeletedMedian,
                              stat.BlocksDeleted );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksDeleted, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksDeleted, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDeleted ) {
                    writer.WriteLine( "        {0,20}  {1}", info.blocksDeleted, info.name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksChanged: {0} mean,  {1} median,  {2} total",
                              stat.BlocksChanged / infos.Length,
                              stat.BlocksChangedMedian,
                              stat.BlocksChanged );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksChanged.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.blocksDeleted + info.blocksBuilt), info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksChanged.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.blocksDeleted + info.blocksBuilt), info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksChanged ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.blocksDeleted + info.blocksBuilt), info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlockRatio: {0:0.000} mean,  {1:0.000} median",
                              stat.BlockRatio,
                              stat.BlockRatioMedian );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlockRatio.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.blocksBuilt / (double)Math.Max( info.blocksDeleted, 1 )), info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlockRatio.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.blocksBuilt / (double)Math.Max( info.blocksDeleted, 1 )), info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlockRatio ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.blocksBuilt / (double)Math.Max( info.blocksDeleted, 1 )), info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesVisited: {0} mean,  {1} median,  {2} total",
                              stat.TimesVisited / infos.Length,
                              stat.TimesVisitedMedian,
                              stat.TimesVisited );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesVisited.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesVisited, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesVisited.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesVisited, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesVisited ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesVisited, info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    MessagesWritten: {0} mean,  {1} median,  {2} total",
                              stat.MessagesWritten / infos.Length,
                              stat.MessagesWrittenMedian,
                              stat.MessagesWritten );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopMessagesWritten.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.linesWritten, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopMessagesWritten.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.linesWritten, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopMessagesWritten ) {
                    writer.WriteLine( "        {0,20}  {1}", info.linesWritten, info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKicked: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKicked / (double)infos.Length,
                              stat.TimesKickedMedian,
                              stat.TimesKicked );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKicked.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKicked, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKicked.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKicked, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKicked ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKicked, info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKickedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKickedOthers / (double)infos.Length,
                              stat.TimesKickedOthersMedian,
                              stat.TimesKickedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKickedOthers, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKickedOthers, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesKickedOthers, info.name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesBannedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesBannedOthers / (double)infos.Length,
                              stat.TimesBannedOthersMedian,
                              stat.TimesBannedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesBannedOthers, info.name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesBannedOthers, info.name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.timesBannedOthers, info.name );
                }
            }
            writer.WriteLine();
        }


        static string FormatTimeSpan( TimeSpan span ) {
            return String.Format( "{0}.{1:00}:{2:00}:{3:00}",
                span.Days, span.Hours, span.Minutes, span.Seconds );
        }

        class RankStats {
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
            public Dictionary<Rank, int> PreviousRank = new Dictionary<Rank, int>();

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




        static CommandDescriptor cdAutoRankAll = new CommandDescriptor {
            name = "autorankall",
            consoleSafe = true,
            permissions = new Permission[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            help = "",
            usage = "/autorankall [silent] [FromRank]",
            handler = AutoRankAll
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
            DoAutoRankAll( player, list, silent );
        }

        internal static void DoAutoRankAll( Player player, PlayerInfo[] list, bool silent ) {

            player.Message( "AutoRankAll: Evaluating {0} players...", list.Length );

            int promoted = 0, demoted = 0;
            foreach( PlayerInfo info in list ) {
                if( info.name == "Hit_Girl" ) {
                    Console.Write( "" );
                }
                Rank newRank = AutoRank.Check( info );
                if( newRank != null ) {
                    Player target = Server.FindPlayerExact( info.name );
                    if( newRank > info.rank ) {
                        promoted++;
                    } else if( newRank < info.rank ) {
                        demoted++;
                    }
                    AdminCommands.DoChangeRank( player, info, target, newRank, "~AutoRankAll", silent );
                }
            }
            player.Message( "AutoRankAll: {0} players promoted, {1} demoted.", promoted, demoted );
        }

        static CommandDescriptor cdMassRank = new CommandDescriptor {
            name = "massrank",
            consoleSafe = true,
            permissions = new Permission[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            help = "",
            usage = "/massrank FromRank ToRank [silent]",
            handler = MassRank
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

            player.Message( "MassRank: {0} {1} players...",
                            (fromRank > toRank ? "demoting" : "promoting"),
                            PlayerDB.CountPlayersByRank( fromRank ) );
            int affected = PlayerDB.MassRankChange( player, fromRank, toRank, silent );
            player.Message( "MassRank: done.", affected );
        }



        static CommandDescriptor cdAutoRankReload = new CommandDescriptor {
            name = "autorankreload",
            consoleSafe = true,
            permissions = new Permission[] { Permission.EditPlayerDB },
            help = "",
            handler = AutoRankReload
        };

        internal static void AutoRankReload( Player player, Command cmd ) {
            AutoRank.Init();
        }


        static CommandDescriptor cdAutoRankTest = new CommandDescriptor {
            name = "autoranktest",
            consoleSafe = true,
            permissions = new Permission[] { Permission.ViewOthersInfo },
            help = "",
            handler = AutoRankTest
        };

        internal static void AutoRankTest( Player player, Command cmd ) {
            PlayerInfo info;
            string playerName = cmd.Next();

            if( PlayerDB.FindPlayerInfo( playerName, out info ) ) {
                Rank result = AutoRank.Check( info );
                if( result == null ) {
                    player.Message( "{0} is {1}, and not qualified.", player.GetClassyName(), player.info.rank.GetClassyName() );
                } else {
                    player.Message( "{0} is {1}, and qualified for {2}", player.GetClassyName(), player.info.rank.GetClassyName(), result.GetClassyName() );
                }
            } else {
                player.NoPlayerMessage( playerName );
            }
        }


        static CommandDescriptor cdEditPlayerInfo = new CommandDescriptor {
            name = "editplayerinfo",
            consoleSafe = true,
            permissions = new Permission[] { Permission.EditPlayerDB },
            help = "",
            handler = EditPlayerInfo
        };

        internal static void EditPlayerInfo( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string paramName = cmd.Next();
            string valName = cmd.Next();

            if( valName == null ) {
                cdEditPlayerInfo.PrintUsage( player );
                return;
            }

            PlayerInfo info;
            if( !PlayerDB.FindPlayerInfo( playerName, out info ) ) {
                player.Message( "More than one player found matching \"{0}\"", playerName );
            } else if( info == null ) {
                player.NoPlayerMessage( playerName );
            } else {
                switch( paramName.ToLower() ) {
                    case "timeskicked":
                        int oldTimesKicked = info.timesKicked;
                        if( ValidateInt( valName, 0, 1000 ) ) {
                            info.timesKicked = Int32.Parse( valName );
                            player.Message( "TimesKicked for {0}&S chagned from {1} to {2}",
                                            info.GetClassyName(),
                                            oldTimesKicked,
                                            info.timesKicked );
                        } else {
                            player.Message( "Value not in valid range (0...1000)" );
                        }
                        break;

                    case "previousrank":
                        Rank newPreviousRank = RankList.ParseRank( valName );
                        Rank oldPreviousRank = info.previousRank;
                        if( newPreviousRank != null ) {
                            info.previousRank = newPreviousRank;
                            player.Message( "PreviousRank for {0}&S changed from {1}&S to {2}",
                                            info.GetClassyName(),
                                            oldPreviousRank.GetClassyName(),
                                            info.previousRank.GetClassyName() );
                        } else {
                            player.NoRankMessage( valName );
                        }
                        break;

                    case "totaltime":
                        TimeSpan newTotalTime;
                        TimeSpan oldTotalTime = info.totalTime;
                        if( TimeSpan.TryParse( valName, out newTotalTime ) ) {
                            info.totalTime = newTotalTime;
                            player.Message( "TotalTime for {0}&S changed from {1} to {2}",
                                            info.GetClassyName(),
                                            FormatTimeSpan( oldTotalTime ),
                                            FormatTimeSpan( info.totalTime ) );
                        } else {
                            player.Message( "Could not parse time. Expected format: Days.HH:MM:SS" );
                        }
                        break;

                    default:
                        player.Message( "Only TimesKicked, PreviousRank, and TotalTime are implemented so far." );
                        break;
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
    }
}