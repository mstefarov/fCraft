﻿// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.AutoRank {
    public sealed class Criterion : ICloneable {

        /// <summary> Rank that the player is currently </summary>
        public Rank FromRank { get; set; }
        /// <summary> Rank that the player will be changed to </summary>
        public Rank ToRank { get; set; }

        /// <summary> The conditions that must be met in order for this to take affect. </summary>
        public ConditionSet Condition { get; set; }

        public Criterion() { }

        public Criterion( [NotNull] Criterion other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            FromRank = other.FromRank;
            ToRank = other.ToRank;
            Condition = other.Condition;
        }

        public Criterion( [NotNull] Rank fromRank, [NotNull] Rank toRank, [NotNull] ConditionSet condition ) {
            if( fromRank == null ) throw new ArgumentNullException( "fromRank" );
            if( toRank == null ) throw new ArgumentNullException( "toRank" );
            if( condition == null ) throw new ArgumentNullException( "condition" );
            FromRank = fromRank;
            ToRank = toRank;
            Condition = condition;
        }

        public Criterion( [NotNull] XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            FromRank = Rank.Parse( el.Attribute( "fromRank" ).Value );
            if( FromRank == null ) throw new SerializationException( "Could not parse \"fromRank\"" );

            ToRank = Rank.Parse( el.Attribute( "toRank" ).Value );
            if( ToRank == null ) throw new SerializationException( "Could not parse \"toRank\"" );

            Condition = (ConditionSet)AutoRank.Condition.Parse( el.Elements().First() );
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
            el.Add( new XAttribute( "fromRank", FromRank.FullName ) );
            el.Add( new XAttribute( "toRank", ToRank.FullName ) );
            if( Condition != null ) {
                el.Add( Condition.Serialize() );
            }
            return el;
        }
    }
}