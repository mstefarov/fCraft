// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace fCraft.AutoRank {

    /// <summary> Base class for all AutoRank conditions. </summary>
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


    /// <summary> Class for checking ranges of countable PlayerInfo fields (see ConditionField enum). </summary>
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field;
        public ComparisonOperation Comparison = ComparisonOperation.Eq;
        public int Value;

        public ConditionIntRange() { }

        public ConditionIntRange( XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            Field = (ConditionField)Enum.Parse( typeof( ConditionField ), el.Attribute( "field" ).Value, true );
            Value = Int32.Parse( el.Attribute( "val" ).Value );
            if( el.Attribute( "op" ) != null ) {
                Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
            }
        }

        public ConditionIntRange( ConditionField field, ComparisonOperation comparison, int value ) {
            Field = field;
            Comparison = comparison;
            Value = value;
        }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            long givenValue;
            switch( Field ) {
                case ConditionField.TimeSinceFirstLogin:
                    givenValue = (int)info.TimeSinceFirstLogin.TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastLogin:
                    givenValue = (int)info.TimeSinceLastLogin.TotalSeconds;
                    break;
                case ConditionField.LastSeen:
                    givenValue = (int)info.TimeSinceLastSeen.TotalSeconds;
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
                    givenValue = (int)info.TimeSinceRankChange.TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastKick:
                    givenValue = (int)info.TimeSinceLastKick.TotalSeconds;
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
            el.Add( new XAttribute( "field", Field ) );
            el.Add( new XAttribute( "val", Value ) );
            el.Add( new XAttribute( "op", Comparison ) );
            return el;
        }

        public override string ToString() {
            return String.Format( "ConditionIntRange( {0} {1} {2} )",
                                  Field,
                                  Comparison,
                                  Value );
        }
    }


    /// <summary> Checks what caused player's last rank change (see RankChangeType enum). </summary>
    public sealed class ConditionRankChangeType : Condition {
        public RankChangeType Type;

        public ConditionRankChangeType( RankChangeType type ) {
            Type = type;
        }

        public ConditionRankChangeType( XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            Type = (RankChangeType)Enum.Parse( typeof( RankChangeType ), el.Attribute( "val" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            return (info.RankChangeType == Type);
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionRankChangeType" );
            el.Add( new XAttribute( "val", Type.ToString() ) );
            return el;
        }
    }


    /// <summary> Checks what rank the player held previously. </summary>
    public sealed class ConditionPreviousRank : Condition {
        public Rank Rank;
        public ComparisonOperation Comparison;

        public ConditionPreviousRank( Rank rank, ComparisonOperation comparison ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( !Enum.IsDefined( typeof( ComparisonOperation ), comparison ) ) {
                throw new ArgumentOutOfRangeException( "comparison", "Unknown comparison type" );
            }
            Rank = rank;
            Comparison = comparison;
        }

        public ConditionPreviousRank( XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            Rank = RankManager.ParseRank( el.Attribute( "val" ).Value );
            Comparison = (ComparisonOperation)Enum.Parse( typeof( ComparisonOperation ), el.Attribute( "op" ).Value, true );
        }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            Rank prevRank = info.PreviousRank ?? info.Rank;
            switch( Comparison ) {
                case ComparisonOperation.Lt:
                    return (prevRank < Rank);
                case ComparisonOperation.Lte:
                    return (prevRank <= Rank);
                case ComparisonOperation.Gte:
                    return (prevRank >= Rank);
                case ComparisonOperation.Gt:
                    return (prevRank > Rank);
                case ComparisonOperation.Eq:
                    return (prevRank == Rank);
                case ComparisonOperation.Neq:
                    return (prevRank != Rank);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionPreviousRank" );
            el.Add( new XAttribute( "val", Rank.GetFullName() ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            return el;
        }
    }


    #region Condition Sets

    /// <summary> Base class for condition sets/combinations. </summary>
    public class ConditionSet : Condition {
        protected ConditionSet() {
            Conditions = new List<Condition>();
        }

        public List<Condition> Conditions { get; private set; }

        protected ConditionSet( IEnumerable<Condition> conditions ) {
            if( conditions == null ) throw new ArgumentNullException( "conditions" );
            Conditions = conditions.ToList();
        }

        protected ConditionSet( XElement el )
            : this() {
            if( el == null ) throw new ArgumentNullException( "el" );
            foreach( XElement cel in el.Elements() ) {
                Add( Parse( cel ) );
            }
        }

        public override bool Eval( PlayerInfo info ) {
            throw new NotImplementedException();
        }

        public void Add( Condition condition ) {
            if( condition == null ) throw new ArgumentNullException( "condition" );
            Conditions.Add( condition );
        }

        public override XElement Serialize() {
            throw new NotImplementedException();
        }
    }


    /// <summary> Logical AND - true if ALL conditions are true. </summary>
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


    /// <summary> Logical AND - true if NOT ALL of the conditions are true. </summary>
    public sealed class ConditionNAND : ConditionSet {
        public ConditionNAND() { }
        public ConditionNAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNAND( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
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


    /// <summary> Logical AND - true if ANY of the conditions are true. </summary>
    public sealed class ConditionOR : ConditionSet {
        public ConditionOR() { }
        public ConditionOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
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


    /// <summary> Logical AND - true if NONE of the conditions are true. </summary>
    public sealed class ConditionNOR : ConditionSet {
        public ConditionNOR() { }
        public ConditionNOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNOR( XElement el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
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
}