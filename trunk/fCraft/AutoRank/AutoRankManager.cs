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

    public enum ComparisonOperation {
        Lt,
        Lte,
        Gte,
        Gt,
        Eq,
        Neq
    }


    public enum ConditionField {
        TimeSinceFirstLogin,
        TimeSinceLastLogin,
        LastSeen,
        TotalTime,
        BlocksBuilt,
        BlocksDeleted,
        BlocksChanged, // BlocksBuilt+BlocksDeleted
        BlocksDrawn,
        TimesVisited,
        MessagesWritten,
        TimesKicked,
        TimeSinceRankChange,
        TimeSinceLastKick
    }


    public enum ConditionScopeType {
        Total,
        SinceRankChange,
        SinceKick,
        TimeSpan
    }


    public enum CriterionType {
        Required,
        Suggested,
        Automatic
    }

    #endregion
}