using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace fCraft {
    public static class AutoRank {
        const string AutoRankFile = "autorank.xml";
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


        public static void Init() {
            criteria.Clear();
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
                    if( criteria.Count == 0 ) {
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

        public XElement Serialize() {
            XElement el = new XElement( "Criterion" );
            el.Add( new XAttribute( "fromRank", FromRank ) );
            el.Add( new XAttribute( "toRank", ToRank ) );
            el.Add( Condition.Serialize() );
            return el;
        }
    }


    #region Conditions

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

        public abstract XElement Serialize();
    }

    // range checks on countable PlayerInfo fields
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field;
        public ConditionScopeType Scope = ConditionScopeType.Total;
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

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionIntRange" );
            el.Add( new XAttribute( "field", Field.ToString() ) );
            el.Add( new XAttribute( "val", Value.ToString() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            el.Add( new XAttribute( "scope", Scope.ToString() ) );
            return el;
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

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionRankStatus" );
            el.Add( new XAttribute( "val", Status.ToString() ) );
            return el;
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
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( !Conditions[i].Eval( info ) ) return false;
            }
            return true;
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
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( !Conditions[i].Eval( info ) ) return true;
            }
            return false;
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
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return true;
            }
            return false;
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
            if( Conditions == null ) return true;
            for( int i = 0; i < Conditions.Count; i++ ) {
                if( Conditions[i].Eval( info ) ) return false;
            }
            return true;
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