// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Xml.Linq;

namespace fCraft.AutoRank {
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

            FromRank = RankManager.ParseRank( el.Attribute( "fromRank" ).Value );
            if( FromRank == null ) throw new FormatException( "Could not parse \"fromRank\"" );

            ToRank = RankManager.ParseRank( el.Attribute( "toRank" ).Value );
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
}
