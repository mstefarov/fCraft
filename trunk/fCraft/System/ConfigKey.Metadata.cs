// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace fCraft {

    [AttributeUsage( AttributeTargets.Field )]
    public class ConfigKeyAttribute : Attribute {
        protected ConfigKeyAttribute( ConfigSection section, Type valueType, object defaultValue ) {
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


        public bool TryValidate( string value ) {
            try {
                Validate( value );
                return true;
            } catch( ArgumentException ) {
                return false;
            }
        }


        public virtual void Validate( string value ) {
            if( NotBlank && String.IsNullOrEmpty( value ) ) {
                throw new FormatException( "Value cannot be blank or null." );
            }
        }
    }


    public sealed class StringKeyAttribute : ConfigKeyAttribute {
        public const int NoLengthRestriction = -1;
        public StringKeyAttribute( ConfigSection section, object defaultValue )
            : base( section, typeof( string ), defaultValue ) {
            MinLength = NoLengthRestriction;
            MaxLength = NoLengthRestriction;
            Regex = null;
        }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public Regex Regex { get; set; }
        public bool RestrictedChars { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );
            if( MinLength != NoLengthRestriction && value.Length < MinLength ) {
                throw new FormatException( String.Format( "Value string is too short; expected at least {0} characters.",
                                                          MinLength ) );
            }
            if( MaxLength != NoLengthRestriction && value.Length > MaxLength ) {
                throw new FormatException( String.Format( "Value string too long; expected at most {1} characters.",
                                                          MaxLength ) );
            }
            if( RestrictedChars && Player.CheckForIllegalChars( value ) ) {
                throw new FormatException( String.Format( "Value contains restricted characters." ) );
            }
            if( Regex != null && !Regex.IsMatch( value ) ) {
                throw new FormatException( String.Format( "Value does not match the expected format: /{0}/.",
                                                          Regex ) );
            }
        }
    }


    public sealed class IntKeyAttribute : ConfigKeyAttribute {
        public IntKeyAttribute( ConfigSection section, int defaultValue )
            : base( section, typeof( int ), defaultValue ) {
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
        public bool AlwaysAllowZero { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );
            int parsedValue;
            if( !Int32.TryParse( value, out parsedValue ) ) {
                throw new FormatException( "Value cannot be parsed as an integer." );
            }

            if( AlwaysAllowZero && parsedValue == 0 ) {
                return;
            }

            if( MinValue != int.MinValue && parsedValue < MinValue ) {
                throw new FormatException( String.Format( "Value is too low; expected at least {0}.", MinValue ) );
            }

            if( MaxValue != int.MaxValue && parsedValue > MaxValue ) {
                throw new FormatException( String.Format( "Value is too high; expected at most {0}.", MaxValue ) );
            }

            if( MultipleOf != 0 && (parsedValue % MultipleOf != 0) ) {
                throw new FormatException( String.Format( "Value is not a multiple of {0}.", MultipleOf ) );
            }
            if( PowerOfTwo ) {
                bool found = false;
                for( int i = 0; i < 31; i++ ) {
                    if( parsedValue == (1 << i) ) {
                        found = true;
                        break;
                    }
                }
                if( !found && parsedValue != 0 ) {
                    throw new FormatException( "Value is not a power of two." );
                }
            }
            if( ValidValues != null ) {
                if( !ValidValues.Any( t => parsedValue == t ) ) {
                    throw new FormatException( "Value is not on the list of valid values." );
                }
            }
            if( InvalidValues != null ) {
                if( !InvalidValues.All( t => parsedValue != t ) ) {
                    throw new FormatException( "Value is on the list of invalid values." );
                }
            }
        }
    }


    public sealed class RankKeyAttribute : ConfigKeyAttribute {
        public RankKeyAttribute( BlankValueMeaning blankMeaning, ConfigSection section )
            : base( section, typeof( Rank ), "" ) {
            CanBeLowest = true;
            CanBeHighest = true;
            BlankMeaning = blankMeaning;
            NotBlank = false;
        }
        public bool CanBeLowest { get; set; }
        public bool CanBeHighest { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override void Validate( string value ) {
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
                    case BlankValueMeaning.Invalid:
                        throw new FormatException( "Value may not be blank." );
                        break;
                    default:
                        throw new ArgumentOutOfRangeException( "Invalid value of BlankMeaning." );
                }
                if( rank == null ) return;
            } else {
                rank = RankList.ParseRank( value );
                if( rank == null ) {
                    throw new FormatException( "Value cannot be parsed as a rank." );
                }
            }
            if( !CanBeLowest && rank == RankList.LowestRank ) {
                throw new FormatException( "Value may not be the lowest rank." );
            }
            if( !CanBeHighest && rank == RankList.HighestRank ) {
                throw new FormatException( "Value may not be the highest rank." );
            }
        }

        public enum BlankValueMeaning {
            Invalid,
            LowestRank,
            DefaultRank,
            HighestRank
        }
    }


    public sealed class BoolKeyAttribute : ConfigKeyAttribute {
        public BoolKeyAttribute( ConfigSection section, bool defaultValue )
            : base( section, typeof( bool ), defaultValue ) {
        }


        public override void Validate( string value ) {
            base.Validate( value );
            bool test;
            if( !Boolean.TryParse( value, out test ) ) {
                throw new FormatException( "Value cannot be parsed as a boolean." );
            }
        }
    }


    public sealed class IPKeyAttribute : ConfigKeyAttribute {
        public IPKeyAttribute( ConfigSection section, BlankValueMeaning defaultMeaning )
            : base( section, typeof( IPAddress ), "" ) {
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


        public override void Validate( string value ) {
            base.Validate( value );
            IPAddress test;
            if( !IPAddress.TryParse( value, out test ) ) {
                throw new FormatException( "Value cannot be parsed as an IP Address." );
            }
            if( NotAny && test.ToString() == IPAddress.Any.ToString() ) {
                throw new FormatException( String.Format( "Value cannot be {0}", IPAddress.Any ) );
            }
            if( NotNone && test.ToString() == IPAddress.None.ToString() ) {
                throw new FormatException( String.Format( "Value cannot be {0}", IPAddress.None ) );
            }
            if( NotLAN && test.IsLAN() ) {
                throw new FormatException( "Value cannot be a LAN address." );
            }
            if( NotLoopback && IPAddress.IsLoopback( test ) ) {
                throw new FormatException( "Value cannot be a loopback address." );
            }
        }

        public enum BlankValueMeaning {
            Any,
            Loopback,
            None
        }
    }


    public sealed class ColorKeyAttribute : ConfigKeyAttribute {
        public ColorKeyAttribute( ConfigSection section, string defaultColor )
            : base( section, typeof( string ), defaultColor ) {
            NotBlank = false;
        }


        public override void Validate( string value ) {
            base.Validate( value );
            if( Color.Parse( value ) == null ) {
                throw new FormatException( "Value cannot be parsed as a color." );
            } else if( Color.Parse( value ) == "" && NotBlank ) {
                throw new FormatException( "Value may not represent absence of color." );
            }
        }
    }


    public sealed class EnumKeyAttribute : ConfigKeyAttribute {
        public EnumKeyAttribute( ConfigSection section, object defaultValue )
            : base( section,null, defaultValue ) {
            ValueType = defaultValue.GetType();
        }


        public override void Validate( string value ) {
            base.Validate( value );
            try {
                Enum.Parse( ValueType, value, true );
            } catch( ArgumentException ) {
                throw new FormatException( String.Format( "Could not parse value as {0}", ValueType.Name ) );
            }
        }
    }
}