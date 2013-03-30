// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.AutoRank {
    static class AutoRankManager {
        internal static readonly TimeSpan TickInterval = TimeSpan.FromSeconds( 60 );

        static readonly List<Criterion> Criteria = new List<Criterion>();

        public const string TagName = "fCraftAutoRankConfig";

        // Whether any criteria are defined.
        static bool HasCriteria {
            get { return Criteria.Count > 0; }
        }


        //  Adds a new criterion to the list. Throws an ArgumentException on duplicates.
        static void Add( [NotNull] Criterion criterion ) {
            if( criterion == null ) throw new ArgumentNullException( "criterion" );
            if( Criteria.Contains( criterion ) ) throw new ArgumentException( "This criterion has already been added." );
            Criteria.Add( criterion );
        }


        /// <summary> Checks whether a given player is due for a promotion or demotion. </summary>
        /// <param name="info"> PlayerInfo to check. </param>
        /// <returns> Null if no rank change is needed, or a rank to promote/demote to. </returns>
        [CanBeNull]
        public static Rank Check( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            for( int i = 0; i < Criteria.Count; i++ ) {
                if( Criteria[i].FromRank == info.Rank &&
                    !info.IsBanned &&
                    Criteria[i].Condition.Eval( info ) ) {

                    return Criteria[i].ToRank;
                }
            }
            return null;
        }


        internal static void TaskCallback( SchedulerTask schedulerTask ) {
            if( !ConfigKey.AutoRankEnabled.Enabled() ) return;
            PlayerInfo[] onlinePlayers = Server.Players.Select( p => p.Info ).ToArray();
            DoAutoRankAll( Player.AutoRank, onlinePlayers, false, "(AutoRanked)" );
        }


        public static bool Init() {
            Criteria.Clear();

            if( File.Exists( Paths.AutoRankFileName ) ) {
                try {
                    XDocument doc = XDocument.Load( Paths.AutoRankFileName );
                    if( doc.Root == null ) return false;
                    foreach( XElement el in doc.Root.Elements( "Criterion" ) ) {
                        try {
                            Add( new Criterion( el ) );
                        } catch( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "AutoRank.Init: Could not parse an AutoRank criterion: {0}", ex );
                        }
                    }
                    if( Criteria.Count == 0 ) {
                        Logger.Log( LogType.Warning, "AutoRank.Init: No criteria loaded." );
                    }
                    return true;
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "AutoRank.Init: Could not parse the AutoRank file: {0}", ex );
                    return false;
                }
            } else {
                Logger.Log( LogType.Warning, "AutoRank.Init: autorank.xml not found. No criteria loaded." );
                return false;
            }
        }


        internal static void DoAutoRankAll( [NotNull] Player player, [NotNull] PlayerInfo[] list, bool silent, string message ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( list == null ) throw new ArgumentNullException( "list" );

            if( !HasCriteria ) {
                player.Message( "AutoRankAll: No criteria found." );
                return;
            }

            player.Message( "AutoRankAll: Evaluating {0} players...", list.Length );

            Stopwatch sw = Stopwatch.StartNew();
            int promoted = 0, demoted = 0;
            for( int i = 0; i < list.Length; i++ ) {
                Rank newRank = Check( list[i] );
                if( newRank != null ) {
                    if( newRank > list[i].Rank ) {
                        promoted++;
                    } else if( newRank < list[i].Rank ) {
                        demoted++;
                    }
                    try {
                        list[i].ChangeRank( player, newRank, message, !silent, true, true );
                    } catch( PlayerOpException ex ) {
                        player.Message( ex.MessageColored );
                    }
                }
            }
            sw.Stop();
            player.Message( "AutoRankAll: Worked for {0}ms, {1} players promoted, {2} demoted.", sw.ElapsedMilliseconds, promoted, demoted );
        }
    }


    #region Enums

    /// <summary>  Operators used to compare PlayerInfo fields. </summary>
    enum ComparisonOp {

        /// <summary> EQuals to </summary>
        Eq,

        /// <summary> Not EQual to </summary>
        Neq,

        /// <summary> Greater Than </summary>
        Gt,

        /// <summary> Greater Than or Equal </summary>
        Gte,

        /// <summary> Less Than </summary>
        Lt,

        /// <summary> Less Than or Equal </summary>
        Lte
    }


    /// <summary> Enumeration of quantifiable PlayerInfo fields (or field combinations) that may be used with AutoRank conditions. </summary>
    enum ConditionField {
        /// <summary> Time since first login (first time the player connected), in seconds.
        /// For players who have been entered into PlayerDB but have never logged in, this is a huge value. </summary>
        TimeSinceFirstLogin,

        /// <summary> Time since most recent login, in seconds.
        /// For players who have been entered into PlayerDB but have never logged in, this is a huge value.</summary>
        TimeSinceLastLogin,

        /// <summary> Time since player was last seen (0 if the player is online, otherwise time since last logout, in seconds).
        /// For players who have been entered into PlayerDB but have never logged in, this is a huge value.</summary>
        LastSeen,

        /// <summary> Total time spent on the server (including current session) in seconds.
        /// For players who have been entered into PlayerDB but have never logged in, this is 0.</summary>
        TotalTime,

        /// <summary> Number of blocks that were built manually (by clicking).
        /// Does not include drawn or pasted blocks. </summary>
        BlocksBuilt,

        /// <summary> Number of blocks deleted manually (by clicking).
        /// Does not include drawn or cut blocks. </summary>
        BlocksDeleted,

        /// <summary> Number of blocks changed (built + deleted) manually (by clicking).
        /// Does not include drawn or cut/paste blocks. </summary>
        BlocksChanged,

        /// <summary> Number of blocks affected by drawing commands, replacement, and cut/paste. </summary>
        BlocksDrawn,

        /// <summary> Number of separate visits/sessions on this server. </summary>
        TimesVisited,

        /// <summary> Number of messages written in chat.
        /// Includes normal chat, PMs, rank chat, /Staff, /Say, and /Me messages. </summary>
        MessagesWritten,

        /// <summary> Number of times kicked by other players or by console.
        /// Does not include any kind of automated kicks (AFK kicks, anti-grief or anti-spam, server shutdown, etc). </summary>
        TimesKicked,

        /// <summary> Time since last promotion or demotion, in seconds.
        /// For new players (who still have the default rank) this is a huge value. </summary>
        TimeSinceRankChange,

        /// <summary> Time since the player has been kicked by other players or by console.
        /// Does not reset from any kind of automated kicks (AFK kicks, anti-grief or anti-spam, server shutdown, etc). </summary>
        TimeSinceLastKick
    }

    #endregion
}