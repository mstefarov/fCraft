// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace fCraft {
    public static class AutoRank {

        static readonly TimeSpan TickInterval = TimeSpan.FromSeconds( 60 );
        static Scheduler.Task task;


        public static void CheckAutoRankSetting() {
            if( ConfigKey.AutoRankEnabled.GetBool() ) {
                if( task == null ) {
                    task = Scheduler.AddBackgroundTask( TaskCallback );
                    task.RunForever( TickInterval );
                } else if( task.IsStopped ) {
                    task.RunForever( TickInterval );
                }
            } else if( task != null && !task.IsStopped ) {
                task.Stop();
            }
        }


        public static void TaskCallback( Scheduler.Task schedulerTask ) {
            MaintenanceCommands.DoAutoRankAll( Player.Console, PlayerDB.GetPlayerListCopy(), false, "~AutoRank" );
        }


        const string AutoRankFile = "autorank.xml";
        static readonly List<Criterion> Criteria = new List<Criterion>();


        public static void Add( Criterion criterion ) {
            Criteria.Add( criterion );
        }


        public static Rank Check( PlayerInfo info ) {
            foreach( Criterion c in Criteria ) {
                if( c.FromRank == info.Rank && !info.Banned && c.Condition.Eval( info ) ) {
                    return c.ToRank;
                }
            }
            return null;
        }


        public static void Init() {
            Criteria.Clear();

            if( File.Exists( AutoRankFile ) ) {
                try {
                    XDocument doc = XDocument.Load( AutoRankFile );
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


    public sealed class Criterion : ICloneable {
        public CriterionType Type { get; set; }
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public Condition Condition { get; set; }

        public Criterion() { }

        public Criterion( Criterion other ) {
            Type = other.Type;
            FromRank = other.FromRank;
            ToRank = other.ToRank;
            Condition = other.Condition;
        }

        public Criterion( CriterionType type, Rank fromRank, Rank toRank, Condition condition ) {
            Type = type;
            FromRank = fromRank;
            ToRank = toRank;
            Condition = condition;
        }

        public Criterion( XElement el ) {
            Type = (CriterionType)Enum.Parse( typeof( CriterionType ), el.Attribute( "type" ).Value, true );

            FromRank = RankList.ParseRank( el.Attribute( "fromRank" ).Value );
            if( FromRank == null ) throw new FormatException( "Could not parse \"fromRank\"" );

            ToRank = RankList.ParseRank( el.Attribute( "toRank" ).Value );
            if( ToRank == null ) throw new FormatException( "Could not parse \"toRank\"" );

            if( el.Elements().Count() == 1 ) {
                Condition = Condition.Parse( el.Elements().First() );

            } else if( el.Elements().Count() > 1 ) {
                ConditionAND cand = new ConditionAND();
                foreach( XElement cond in el.Elements() ) {
                    cand.Add( Condition.Parse( cond ) );
                }
                Condition = cand;

            } else {
                throw new FormatException( "At least one condition required." );
            }
        }

        public object Clone() {
            return new Criterion( this );
        }

        public override string ToString() {
            return String.Format( "Criteria( {0} from {1} to {2} )",
                                  (FromRank < ToRank ? "promote" : "demote"),
                                  FromRank.Name,
                                  ToRank.Name );
        }

        public XElement Serialize() {
            XElement el = new XElement( "Criterion" );
            el.Add( new XAttribute( "type", Type ) );
            el.Add( new XAttribute( "fromRank", FromRank ) );
            el.Add( new XAttribute( "toRank", ToRank ) );
            if( Condition != null ) {
                el.Add( Condition.Serialize() );
            }
            return el;
        }
    }


    #region Conditions

    // Base class for all conditions
    public abstract class Condition {
        public abstract bool Eval( PlayerInfo info );

        public static Condition Parse( XElement el ) {
            if( el.Name == "AND" ) {
                return new ConditionAND( el );
            } else if( el.Name == "OR" ) {
                return new ConditionOR( el );
            } else if( el.Name == "NOR" ) {
                return new ConditionNOR( el );
            } else if( el.Name == "NAND" ) {
                return new ConditionNAND( el );
            } else if( el.Name == "ConditionIntRange" ) {
                return new ConditionIntRange( el );
            } else if( el.Name == "ConditionRankChangeType" ) {
                return new ConditionRankChangeType( el );
            } else if( el.Name == "ConditionPreviousRank" ) {
                return new ConditionPreviousRank( el );
            } else {
                return null;
            }
        }

        public abstract XElement Serialize();
    }

    // range checks on countable PlayerInfo fields
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field;
        public ConditionScopeType Scope = ConditionScopeType.Total;
        public ComparisonOperation Comparison = ComparisonOperation.Eq;
        public int Value;

        public ConditionIntRange() { }

        public ConditionIntRange( XElement el ) {
            Field = (ConditionField)Enum.Parse( typeof( ConditionField ), el.Attribute( "field" ).Value, true );
            Value = Int32.Parse( el.Attribute( "val" ).Value );
            if( el.Attribute( "op" ) != null ) {
                Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
            }
            if( el.Attribute( "scope" ) != null ) {
                Scope = (ConditionScopeType)Enum.Parse( typeof( ConditionScopeType ), el.Attribute( "scope" ).Value, true );
            }
        }

        public ConditionIntRange( ConditionField field, ComparisonOperation comparison, int value ) {
            Field = field;
            Comparison = comparison;
            Value = value;
        }

        public override bool Eval( PlayerInfo info ) {
            long givenValue;
            switch( Field ) {
                case ConditionField.TimeSinceFirstLogin:
                    givenValue = (int)DateTime.Now.Subtract( info.FirstLoginDate ).TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastLogin:
                    givenValue = (int)DateTime.Now.Subtract( info.LastLoginDate ).TotalSeconds;
                    break;
                case ConditionField.LastSeen:
                    givenValue = (int)DateTime.Now.Subtract( info.LastSeen ).TotalSeconds;
                    break;
                case ConditionField.BlocksBuilt:
                    givenValue = info.BlocksBuilt;
                    break;
                case ConditionField.BlocksDeleted:
                    givenValue = info.BlocksDeleted;
                    break;
                case ConditionField.BlocksChanged:
                    givenValue = info.BlocksBuilt + info.BlocksDeleted;
                    break;
                case ConditionField.BlocksDrawn:
                    givenValue = info.BlocksDrawn;
                    break;
                case ConditionField.TimesVisited:
                    givenValue = info.TimesVisited;
                    break;
                case ConditionField.MessagesWritten:
                    givenValue = info.LinesWritten;
                    break;
                case ConditionField.TimesKicked:
                    givenValue = info.TimesKicked;
                    break;
                case ConditionField.TotalTime:
                    givenValue = (int)info.TotalTime.TotalSeconds;
                    break;
                case ConditionField.TimeSinceRankChange:
                    givenValue = (int)DateTime.Now.Subtract( info.RankChangeDate ).TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastKick:
                    givenValue = (int)DateTime.Now.Subtract( info.LastKickDate ).TotalSeconds;
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "Field", "Unknown field type" );
            }

            switch( Comparison ) {
                case ComparisonOperation.Lt:
                    return (givenValue < Value);
                case ComparisonOperation.Lte:
                    return (givenValue <= Value);
                case ComparisonOperation.Gte:
                    return (givenValue >= Value);
                case ComparisonOperation.Gt:
                    return (givenValue > Value);
                case ComparisonOperation.Eq:
                    return (givenValue == Value);
                case ComparisonOperation.Neq:
                    return (givenValue != Value);
                default:
                    throw new ArgumentOutOfRangeException( "Comparison", "Unknown comparison type" );
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionIntRange" );
            el.Add( new XAttribute( "field", Field.ToString() ) );
            el.Add( new XAttribute( "val", Value.ToString() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            el.Add( new XAttribute( "scope", Scope.ToString() ) );
            return el;
        }

        public override string ToString() {
            return String.Format( "ConditionIntRange( {0} {1} {2} )",
                                  Field,
                                  Comparison,
                                  Value );
        }
    }


    // check RankStatus
    public sealed class ConditionRankChangeType : Condition {
        public RankChangeType Type;

        public ConditionRankChangeType( RankChangeType type ) {
            Type = type;
        }

        public ConditionRankChangeType( XElement el ) {
            Type = (RankChangeType)Enum.Parse( typeof( RankChangeType ), el.Attribute( "val" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            return (info.RankChangeType == Type);
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionRankChangeType" );
            el.Add( new XAttribute( "val", Type.ToString() ) );
            return el;
        }
    }


    // check previous Rank
    public sealed class ConditionPreviousRank : Condition {
        public Rank Rank;
        public ComparisonOperation Comparison;

        public ConditionPreviousRank( Rank rank, ComparisonOperation comparison ) {
            if( !Enum.IsDefined( typeof( ComparisonOperation ), comparison ) ) {
                throw new ArgumentOutOfRangeException( "comparison", "Unknown comparison type" );
            }
            Rank = rank;
            Comparison = comparison;
        }

        public ConditionPreviousRank( XElement el ) {
            Rank = RankList.ParseRank( el.Attribute( "val" ).Value );
            Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            Rank prevRank = info.PreviousRank ?? info.Rank;
            switch( Comparison ) {
                case ComparisonOperation.Lt:
                    return ( prevRank < Rank );
                case ComparisonOperation.Lte:
                    return ( prevRank <= Rank );
                case ComparisonOperation.Gte:
                    return ( prevRank >= Rank );
                case ComparisonOperation.Gt:
                    return ( prevRank > Rank );
                case ComparisonOperation.Eq:
                    return ( prevRank == Rank );
                case ComparisonOperation.Neq:
                    return ( prevRank != Rank );
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionPreviousRank" );
            el.Add( new XAttribute( "val", Rank.ToString() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            return el;
        }
    }

    #endregion


    #region Condition Sets

    // base class for condition combinations
    public class ConditionSet : Condition {
        protected ConditionSet() {
            Conditions = new List<Condition>();
        }

        public List<Condition> Conditions { get; private set; }

        protected ConditionSet( IEnumerable<Condition> conditions ) {
            Conditions = conditions.ToList();
        }

        protected ConditionSet( XElement el )
            : this() {
            foreach( XElement cel in el.Elements() ) {
                Add( Parse( cel ) );
            }
        }

        public override bool Eval( PlayerInfo info ) {
            throw new NotImplementedException();
        }

        public void Add( Condition cond ) {
            Conditions.Add( cond );
        }

        public override XElement Serialize() {
            throw new NotImplementedException();
        }
    }

    // Logical AND
    public sealed class ConditionAND : ConditionSet {
        public ConditionAND() { }
        public ConditionAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            return Conditions == null || Conditions.All( t => t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "AND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical NAND
    public sealed class ConditionNAND : ConditionSet {
        public ConditionNAND() { }
        public ConditionNAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            return Conditions == null || Conditions.Any( t => !t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "NAND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical OR
    public sealed class ConditionOR : ConditionSet {
        public ConditionOR() { }
        public ConditionOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            return Conditions == null || Conditions.Any( t => t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "OR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    // Logical NOR
    public sealed class ConditionNOR : ConditionSet {
        public ConditionNOR() { }
        public ConditionNOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            return Conditions == null || Conditions.All( t => !t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "NOR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    #endregion


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

    public enum RankChangeType {
        Default = 0,
        Promoted = 1,
        Demoted = 2,
        AutoPromoted = 3,
        AutoDemoted = 4
    }

    #endregion
}