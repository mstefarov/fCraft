// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace fCraft {

    [AttributeUsage( AttributeTargets.Field )]
    public class ConfigKeyAttribute : Attribute {
        protected ConfigKeyAttribute( Type valueType, object defaultValue, ConfigSection section ) {
            ValueType = valueType;
            DefaultValue = defaultValue;
            Section = section;
            NotBlank = false;
        }
        public Type ValueType { get; protected set; }
        public object DefaultValue { get; protected set; }
        public ConfigSection Section { get; protected set; }
        public bool NotBlank { get; set; }
        public ConfigKey Key { get; set; }


        public virtual bool Validate( string value ) {
            if( NotBlank && String.IsNullOrEmpty( value ) ) {
                return false;
            } else {
                return true;
            }
        }
    }


    public sealed class StringKeyAttribute : ConfigKeyAttribute {
        public const int NoLengthRestriction = -1;
        public StringKeyAttribute( object defaultValue, ConfigSection section )
            : base( typeof( string ), defaultValue, section ) {
            MinLength = NoLengthRestriction;
            MaxLength = NoLengthRestriction;
            Regex = null;
        }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public Regex Regex { get; set; }
        public bool RestrictedChars { get; set; }


        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            if( MinLength != NoLengthRestriction && value.Length < MinLength ) return false;
            if( MaxLength != NoLengthRestriction && value.Length > MaxLength ) return false;
            if( RestrictedChars && Player.CheckForIllegalChars( value ) ) return false;
            if( Regex != null && !Regex.IsMatch( value ) ) return false;

            return true;
        }
    }


    public sealed class IntKeyAttribute : ConfigKeyAttribute {
        public IntKeyAttribute( int defaultValue, ConfigSection section )
            : base( typeof( int ), defaultValue, section ) {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
            PowerOfTwo = false;
            MultipleOf = 0;
            ValidValues = null;
            InvalidValues = null;
        }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool PowerOfTwo { get; set; }
        public int MultipleOf { get; set; }
        public int[] ValidValues { get; set; }
        public int[] InvalidValues { get; set; }


        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            int parsedValue;
            if( !Int32.TryParse( value, out parsedValue ) ) return false;
            if( MinValue != int.MinValue && parsedValue < MinValue ) return false;
            if( MaxValue != int.MaxValue && parsedValue > MaxValue ) return false;
            if( MultipleOf != 0 && (parsedValue % MultipleOf != 0) ) return false;
            if( PowerOfTwo ) {
                bool found = false;
                for( int i = 0; i < 31; i++ ) {
                    if( parsedValue == (1 << i) ) {
                        found = true;
                        break;
                    }
                }
                if( !found && parsedValue != 0 ) return false;
            }
            if( ValidValues != null ) {
                if( !ValidValues.Any( t => parsedValue == t ) ) return false;
            }
            if( InvalidValues != null ) {
                return InvalidValues.All( t => parsedValue != t );
            }
            return true;
        }
    }


    public sealed class RankKeyAttribute : ConfigKeyAttribute {
        public RankKeyAttribute( BlankValueMeaning blankMeaning, ConfigSection section )
            : base( typeof( Rank ), "", section ) {
            CanBeLowest = true;
            CanBeHighest = true;
            BlankMeaning = blankMeaning;
            NotBlank = false;
        }
        public bool CanBeLowest { get; set; }
        public bool CanBeHighest { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override bool Validate( string value ) {
            Rank rank;
            if( string.IsNullOrEmpty( value ) ) {
                switch( BlankMeaning ) {
                    case BlankValueMeaning.DefaultRank:
                        rank = RankList.DefaultRank;
                        break;
                    case BlankValueMeaning.HighestRank:
                        rank = RankList.HighestRank;
                        break;
                    case BlankValueMeaning.LowestRank:
                        rank = RankList.LowestRank;
                        break;
                    default:
                        return false;
                }
            } else {
                rank = RankList.ParseRank( value );
            }
            if( rank == null ) return false;
            if( !CanBeLowest && rank == RankList.LowestRank ) return false;
            if( !CanBeHighest && rank == RankList.HighestRank ) return false;
            return true;
        }

        public enum BlankValueMeaning {
            Invalid,
            LowestRank,
            DefaultRank,
            HighestRank
        }
    }


    public sealed class BoolKeyAttribute : ConfigKeyAttribute {
        public BoolKeyAttribute( bool defaultValue, ConfigSection section )
            : base( typeof( bool ), defaultValue, section ) {
        }


        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            bool test;
            return Boolean.TryParse( value, out test );
        }
    }


    public sealed class IPKeyAttribute : ConfigKeyAttribute {
        public IPKeyAttribute( BlankValueMeaning defaultMeaning, ConfigSection section )
            : base( typeof( IPAddress ), "", section ) {
            BlankMeaning = defaultMeaning;
            switch( BlankMeaning ) {
                case BlankValueMeaning.Any:
                    DefaultValue = IPAddress.Any;
                    break;
                case BlankValueMeaning.Loopback:
                    DefaultValue = IPAddress.Loopback;
                    break;
                default:
                    DefaultValue = IPAddress.None;
                    break;
            }
        }

        public bool NotAny { get; set; }
        public bool NotNone { get; set; }
        public bool NotLAN { get; set; }
        public bool NotLoopback { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            IPAddress test;
            if( !IPAddress.TryParse( value, out test ) ) return false;
            if( NotAny && test.ToString() == IPAddress.Any.ToString() ) return false;
            if( NotNone && test.ToString() == IPAddress.None.ToString() ) return false;
            if( NotLAN && test.IsLAN() ) return false;
            if( NotLoopback && IPAddress.IsLoopback( test ) ) return false;
            return true;
        }

        public enum BlankValueMeaning {
            Any,
            Loopback,
            None
        }
    }


    public sealed class ColorKeyAttribute : ConfigKeyAttribute {
        public ColorKeyAttribute( string defaultColor, ConfigSection section )
            : base( typeof( string ), defaultColor, section ) {
            NotBlank = false;
        }


        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            return Color.Parse( value ) != null;
        }
    }


    public sealed class EnumKeyAttribute : ConfigKeyAttribute {
        public EnumKeyAttribute( object defaultValue, ConfigSection section )
            : base( null, defaultValue, section ) {
            ValueType = defaultValue.GetType();
        }


        public override bool Validate( string value ) {
            if( String.IsNullOrEmpty( value ) ) {
                return !NotBlank;
            }
            try {
                Enum.Parse( ValueType, value, true );
                return true;
            } catch( ArgumentException ) {
                return false;
            }
        }
    }
}