// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace fCraft {

    [AttributeUsage( AttributeTargets.Field )]
    public class ConfigKeyAttribute : Attribute {
        public ConfigKeyAttribute( Type _type, object _defaultValue, ConfigSection _section ) {
            ValueType = _type;
            DefaultValue = _defaultValue;
            Section = _section;
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

        public virtual string GetErrorMessageFor( string value ) {
            return null;
        }
    }


    public class StringKeyAttribute : ConfigKeyAttribute {
        public const int NoLengthRestriction = -1;
        public StringKeyAttribute( object _defaultValue, ConfigSection _section )
            : base( typeof( string ), _defaultValue, _section ) {
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


    public class IntKeyAttribute : ConfigKeyAttribute {
        public IntKeyAttribute( int _defaultValue, ConfigSection _section )
            : base( typeof( int ), _defaultValue, _section ) {
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

        public override bool Validate( string _value ) {
            if( !base.Validate( _value ) ) return false;
            int value;
            if( !Int32.TryParse( _value, out value ) ) return false;
            if( MinValue != int.MinValue && value < MinValue ) return false;
            if( MaxValue != int.MaxValue && value > MaxValue ) return false;
            if( MultipleOf != 0 && (value % MultipleOf != 0) ) return false;
            if( PowerOfTwo ) {
                bool found = false;
                for( int i = 0; i < 31; i++ ) {
                    if( value == (1 << i) ) {
                        found = true;
                        break;
                    }
                }
                if( !found && value != 0 ) return false;
            }
            if( ValidValues != null ) {
                if( !ValidValues.Any( t => value == t ) ) return false;
            }
            if( InvalidValues != null ) {
                return InvalidValues.All( t => value != t );
            }
            return true;
        }
    }


    public class RankKeyAttribute : ConfigKeyAttribute {
        public RankKeyAttribute( BlankValueMeaning _blankMeaning, ConfigSection _section )
            : base( typeof( Rank ), "", _section ) {
            CanBeLowest = true;
            CanBeHighest = true;
            BlankMeaning = _blankMeaning;
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


    public class BoolKeyAttribute : ConfigKeyAttribute {
        public BoolKeyAttribute( bool _defaultValue, ConfigSection _section )
            : base( typeof( bool ), _defaultValue, _section ) {
        }

        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            bool test;
            return Boolean.TryParse( value, out test );
        }
    }


    public class IPKeyAttribute : ConfigKeyAttribute {
        public IPKeyAttribute( BlankValueMeaning _defaultMeaning, ConfigSection _section )
            : base( typeof( IPAddress ), "", _section ) {
            BlankMeaning = _defaultMeaning;
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


    public class ColorKeyAttribute : ConfigKeyAttribute {
        public ColorKeyAttribute( string _defaultColor, ConfigSection _section )
            : base( typeof( string ), _defaultColor, _section ) {
            NotBlank = false;
        }

        public override bool Validate( string value ) {
            if( !base.Validate( value ) ) return false;
            if( Color.Parse( value ) == null ) return false;
            return true;
        }
    }


    public class EnumKeyAttribute : ConfigKeyAttribute {
        public EnumKeyAttribute( object _defaultValue, ConfigSection _section )
            : base( null, _defaultValue, _section ) {
            ValueType = _defaultValue.GetType();
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