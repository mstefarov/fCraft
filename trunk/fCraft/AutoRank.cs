using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;


namespace fCraft {

    public static class AutoRank {
        static List<Criterion> criteria = new List<Criterion>();

        public static void Add( Criterion criterion ) {
            criteria.Add( criterion );
        }

        public static Rank Check( PlayerInfo info ) {
            foreach( Criterion c in criteria ) {
                if( c.FromRank == info.rank && c.Condition.Eval( info ) ) {
                    return c.ToRank;
                }
            }
            return null;
        }

        public static void InitTest() {
            if( criteria.Count != 0 ) return;
            AutoRank.Add( new Criterion {
                Type = CriterionType.Automatic,
                FromRank = RankList.LowestRank,
                ToRank = RankList.HighestRank,
                Condition = new ConditionAND( new Condition[]{
                                new ConditionIntRange(ConditionField.BlocksBuilt, ComparisonOperation.gte, 20),
                                new ConditionIntRange(ConditionField.BlocksDeleted, ComparisonOperation.gte, 10)
                            } )
            } );
        }
    }

    public class Criterion {
        public CriterionType Type { get; set; }
        public Rank FromRank { get; set; }
        public Rank ToRank { get; set; }
        public Condition Condition { get; set; }

        public Criterion() { }

        public Criterion( CriterionType _type, Rank _fromRank, Rank _toRank, Condition _condition ) {
            Type = _type;
            FromRank = _fromRank;
            ToRank = _toRank;
            Condition = _condition;
        }

        public Criterion( XElement el ) {
            Type = (CriterionType)Enum.Parse( typeof( CriterionType ), el.Attribute( "type" ).Value );
            FromRank = RankList.ParseRank( el.Attribute( "fromRank" ).Value );
            ToRank = RankList.ParseRank( el.Attribute( "toRank" ).Value );
            if( el.Elements().Count() == 1 ) {
                Condition = Condition.Parse( el );
            } else if( el.Elements().Count() > 1 ) {
                Condition = new ConditionAND( el );
            } else {
                throw new FormatException( "At least one condition required." );
            }
        }
    }


    #region Condition Sets

    // Base class for all conditions
    public abstract class Condition {
        public Condition() { }
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
            } else if( Enum.GetNames( typeof( ConditionField ) ).Contains( el.Name.ToString() ) ) {
                return new ConditionIntRange( el );
            } else if( el.Name == "RankStatus" ) {
                return new ConditionRankStatus( el );
            } else if( el.Name == "PreviousRank" ) {
                return new ConditionPreviousRank( el );
            } else {
                return null;
            }
        }
    }

    // base class for condition combinations
    public class ConditionSet : Condition {
        protected ConditionSet() {
            Conditions = new List<Condition>();
        }

        public List<Condition> Conditions {
            get;
            private set;
        }

        protected ConditionSet( IEnumerable<Condition> _conditions ) {
            Conditions = _conditions.ToList();
        }

        protected ConditionSet( XElement el ) {
            foreach( XElement cel in el.Elements() ) {
                Add( Condition.Parse( cel ) );
            }
        }

        public override bool Eval( PlayerInfo info ) {
            throw new NotImplementedException();
        }

        public void Add( Condition cond ) {
            Conditions.Add( cond );
        }
    }

    // Logical AND
    public sealed class ConditionAND : ConditionSet {
        public ConditionAND() { }
        public ConditionAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i=0; i<Conditions.Count; i++){
                if( !Conditions[i].Eval( info ) ) return false;
            }
            return true;
        }
    }

    // Logical NAND
    public sealed class ConditionNAND : ConditionSet {
        public ConditionNAND() { }
        public ConditionNAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( !Conditions[i].Eval( info ) ) return true;
            }
            return false;
        }
    }

    // Logical OR
    public sealed class ConditionOR : ConditionSet {
        public ConditionOR() { }
        public ConditionOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return true;
            }
            return false;
        }
    }

    // Logical NOR
    public sealed class ConditionNOR : ConditionSet {
        public ConditionNOR() { }
        public ConditionNOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return false;
            }
            return true;
        }
    }

    #endregion


    #region Conditions

    // range checks on countable PlayerInfo fields
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field;
        public ConditionScopeType Scope = ConditionScopeType.Total;
        public TimeSpan ScopeTimeSpan = TimeSpan.Zero;
        public ComparisonOperation Comparison = ComparisonOperation.eq;
        public int Value;

        public ConditionIntRange() { }

        public ConditionIntRange( XElement el ) {
            Field = (ConditionField)Enum.Parse( typeof( ConditionField ), el.Attribute( "field" ).Value );
            Value = Int32.Parse( el.Attribute( "val" ).Value );
            if( el.Attribute( "op" ) != null ) {
                Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value );
            }
            if( el.Attribute( "scope" ) != null ) {
                Scope = (ConditionScopeType)Enum.Parse( typeof( ConditionScopeType ), el.Attribute( "scope" ).Value );
            }
            if( el.Attribute( "timespan" ) != null ) {
                ScopeTimeSpan = TimeSpan.Parse( el.Attribute( "timespan" ).Value );
            }
        }

        public ConditionIntRange( ConditionField _field, ComparisonOperation _comparison, int _value ) {
            this.Field = _field;
            this.Comparison = _comparison;
            this.Value = _value;
        }

        public override bool Eval( PlayerInfo info ) {
            int value;
            switch( Field ) {
                case ConditionField.BlocksBuilt:
                    value = info.blocksBuilt;
                    break;
                case ConditionField.BlocksChanged:
                    value = info.blocksBuilt + info.blocksDeleted;
                    break;
                case ConditionField.BlocksDeleted:
                    value = info.blocksDeleted;
                    break;
                case ConditionField.MessagesWritten:
                    value = info.linesWritten;
                    break;
                case ConditionField.TimeSinceFirstLogin:
                    value = (int)DateTime.Now.Subtract( info.firstLoginDate ).TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastLogin:
                    value = (int)DateTime.Now.Subtract( info.lastLoginDate ).TotalSeconds;
                    break;
                case ConditionField.TimesKicked:
                    value = info.timesKicked;
                    break;
                case ConditionField.TimesVisited:
                    value = info.timesVisited;
                    break;
                case ConditionField.TotalTime:
                    value = (int)info.totalTimeOnServer.TotalSeconds;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch( this.Comparison ) {
                case ComparisonOperation.lt:
                    return (value < Value);
                case ComparisonOperation.lte:
                    return (value <= Value);
                case ComparisonOperation.gte:
                    return (value >= Value);
                case ComparisonOperation.gt:
                    return (value > Value);
                case ComparisonOperation.eq:
                    return (value == Value);
                case ComparisonOperation.neq:
                    return (value != Value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    // check RankStatus
    public sealed class ConditionRankStatus : Condition {
        public RankStatus Status;

        public ConditionRankStatus( RankStatus _status ) {
            this.Status = _status;
        }

        public ConditionRankStatus( XElement el ) {
            Status = (RankStatus)Enum.Parse( typeof( RankStatus ), el.Attribute( "val" ).Value );
        }

        public override bool Eval( PlayerInfo info ) {
            return (info.rankStatus & this.Status) > 0;
        }
    }


    // check previous Rank
    public sealed class ConditionPreviousRank : Condition {
        public Rank Rank;
        public ComparisonOperation Comparison;

        public ConditionPreviousRank( Rank _rank, ComparisonOperation _comparison ) {
            this.Rank = _rank;
            this.Comparison = _comparison;
        }

        public ConditionPreviousRank( XElement el ) {
            Rank = RankList.ParseRank( el.Attribute( "val" ).Value );
            Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value );
        }

        public override bool Eval( PlayerInfo info ) {
            switch( this.Comparison ) {
                case ComparisonOperation.lt:
                    return (info.previousRank < this.Rank);
                case ComparisonOperation.lte:
                    return (info.previousRank <= this.Rank);
                case ComparisonOperation.gte:
                    return (info.previousRank >= this.Rank);
                case ComparisonOperation.gt:
                    return (info.previousRank > this.Rank);
                case ComparisonOperation.eq:
                    return (info.previousRank == this.Rank);
                case ComparisonOperation.neq:
                    return (info.previousRank != this.Rank);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    #endregion


    #region Enums

    public enum ComparisonOperation {
        lt,
        lte,
        gte,
        gt,
        eq,
        neq
    }

    public enum ConditionField {
        TimeSinceFirstLogin,
        TimeSinceLastLogin,
        //LastSeen, TODO
        TotalTime,
        //NonIdleTime, TODO
        BlocksBuilt,
        BlocksDeleted,
        BlocksChanged,
        TimesVisited,
        MessagesWritten,
        TimesKicked
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

    public enum RankStatus {
        Promoted,
        Demoted,
        AutoPromoted,
        AutoDemoted,
        Default,
        Unknown
    }

    #endregion
}