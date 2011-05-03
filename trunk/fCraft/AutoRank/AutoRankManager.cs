// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace fCraft.AutoRank {
    public static class AutoRankManager {

        static readonly TimeSpan TickInterval = TimeSpan.FromSeconds( 60 );
        static SchedulerTask task;

        public static bool HasCriteria {
            get {
                return Criteria.Count > 0;
            }
        }


        public static void CheckAutoRankSetting() {
            if( ConfigKey.AutoRankEnabled.GetBool() ) {
                if( task == null ) {
                    task = Scheduler.NewBackgroundTask( TaskCallback );
                    task.RunForever( TickInterval );
                } else if( task.IsStopped ) {
                    task.RunForever( TickInterval );
                }
            } else if( task != null && !task.IsStopped ) {
                task.Stop();
            }
        }


        public static void TaskCallback( SchedulerTask schedulerTask ) {
            MaintenanceCommands.DoAutoRankAll( Player.Console, PlayerDB.GetPlayerListCopy(), false, "~AutoRank" );
        }


        static readonly List<Criterion> Criteria = new List<Criterion>();


        public static void Add( Criterion criterion ) {
            if( criterion == null ) throw new ArgumentNullException( "criterion" );
            Criteria.Add( criterion );
        }


        public static Rank Check( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            foreach( Criterion c in Criteria ) {
                if( c.FromRank == info.Rank && !info.Banned && c.Condition.Eval( info ) ) {
                    return c.ToRank;
                }
            }
            return null;
        }


        public static void Init() {
            Criteria.Clear();

            if( File.Exists( Paths.AutoRankFile ) ) {
                try {
                    XDocument doc = XDocument.Load( Paths.AutoRankFile );
                    foreach( XElement el in doc.Root.Elements( "Criterion" ) ) {
                        try {
                            Add( new Criterion( el ) );
                        } catch( Exception ex ) {
                            Logger.Log( "AutoRank.Init: Could not parse an AutoRank criterion: {0}", LogType.Error, ex );
                        }
                    }
                    if( Criteria.Count == 0 ) {
                        Logger.Log( "AutoRank.Init: No criteria loaded.", LogType.Warning );
                    }
                } catch( Exception ex ) {
                    Logger.Log( "AutoRank.Init: Could not parse the AutoRank file: {0}", LogType.Error, ex );
                }
            } else {
                Logger.Log( "AutoRank.Init: autorank.xml not found. No criteria loaded.", LogType.Warning );
            }
        }
    }


    #region Enums

    /// <summary>  Operators used to compare PlayerInfo fields. </summary>
    public enum ComparisonOperation {
        /// <summary> Less Than </summary>
        Lt,

        /// <summary> Less Than or Equal </summary>
        Lte,

        /// <summary> Greater Than or Equal </summary>
        Gte,

        /// <summary> Greater Than </summary>
        Gt,

        /// <summary> EQuals to </summary>
        Eq,

        /// <summary> Not EQual to </summary>
        Neq
    }


    /// <summary> Enumeration of quantifiable PlayerInfo fields (or field combinations) that may be used with AutoRank conditions. </summary>
    public enum ConditionField {
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
        /// Includes normal chat, PMs, rank chat, /staff, /say, and /me messages. </summary>
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


    // Not yet implemented.
    public enum ConditionScopeType {
        Total,
        SinceRankChange,
        SinceKick,
        TimeSpan
    }


    // Not yet implemented.
    public enum CriterionType {
        Required,
        Suggested,
        Automatic
    }

    #endregion
}