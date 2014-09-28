// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Describes attributes and metadata of a configuration key. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class ConfigKeyAttribute : DescriptionAttribute {
        protected internal ConfigKeyAttribute(ConfigSection section, [NotNull] Type valueType,
                                              [NotNull] object defaultValue, [NotNull] string description)
            : base(description) {
            if (valueType == null) throw new ArgumentNullException("valueType");
            if (defaultValue == null) throw new ArgumentNullException("defaultValue");
            if (description == null) throw new ArgumentNullException("description");
            ValueType = valueType;
            DefaultValue = defaultValue;
            Section = section;
            NotBlank = false;
        }


        /// <summary> Underlying type of this key's value, e.g. System.String </summary>
        [NotNull]
        public Type ValueType { get; protected set; }

        /// <summary> Default value for this key. May be blank.
        /// Call "Process" on this value before using. </summary>
        [NotNull]
        public object DefaultValue { get; protected set; }

        /// <summary> Section to which this key belongs. </summary>
        public ConfigSection Section { get; private set; }

        /// <summary> Whether the value of this key may be left blank. </summary>
        public bool NotBlank { get; set; }

        /// <summary> Associated config key. </summary>
        public ConfigKey Key { get; internal set; }


        /// <summary> Attempts to validate the given value against this key's rules.
        /// Returns without incident if validation succeeds. Throws exceptions on failure. </summary>
        /// <param name="value"> Value to validate. </param>
        /// <exception cref="ArgumentNullException"> value is null. </exception>
        /// <exception cref="FormatException"> Given value did not pass the validation rules. </exception>
        public virtual void Validate([NotNull] string value) {
            if (value == null) throw new ArgumentNullException("value");
            if (NotBlank && value.Length == 0) {
                throw new FormatException("Value cannot be blank.");
            }
        }


        /// <summary> Converts raw value of this key to a processed, usable value.
        /// Notably, semantic values are replaced with literal values. For example:
        /// A RankKey with a blank value and BlankValueMeaning.DefaultRank will return
        /// the actual FullName of the default rank. </summary>
        /// <param name="value"> Raw value to process. May not be null. </param>
        /// <returns> Processed, usable value. </returns>
        /// <exception cref="ArgumentNullException"> value is null </exception>
        [NotNull]
        [DebuggerStepThrough]
        public virtual string Process([NotNull] string value) {
            if (value == null) throw new ArgumentNullException("value");
            return value;
        }
    }


    internal sealed class StringKeyAttribute : ConfigKeyAttribute {
        public const int NoLengthRestriction = -1;


        public StringKeyAttribute(ConfigSection section, [NotNull] object defaultValue, [NotNull] string description)
            : base(section, typeof(string), defaultValue, description) {
            MinLength = NoLengthRestriction;
            MaxLength = NoLengthRestriction;
        }


        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        Regex regex;

        [CanBeNull]
        public string RegexString {
            get {
                if (regex != null) {
                    return regex.ToString();
                } else {
                    return null;
                }
            }
            set {
                if (value == null) {
                    regex = null;
                } else {
                    regex = new Regex(value);
                }
            }
        }


        public override void Validate(string value) {
            base.Validate(value);
            if (MinLength != NoLengthRestriction && value.Length < MinLength) {
                throw new FormatException(String.Format(
                    "Value string is too short; expected at least {0} characters.",
                    MinLength));
            }
            if (MaxLength != NoLengthRestriction && value.Length > MaxLength) {
                throw new FormatException(String.Format("Value string too long; expected at most {0} characters.",
                                                        MaxLength));
            }
            if (regex != null && !regex.IsMatch(value)) {
                throw new FormatException(String.Format("Value does not match the expected format: /{0}/.",
                                                        RegexString));
            }
        }
    }


    internal sealed class IntKeyAttribute : ConfigKeyAttribute {
        public IntKeyAttribute(ConfigSection section, int defaultValue, [NotNull] string description)
            : base(section, typeof(int), defaultValue, description) {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
        }


        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool AlwaysAllowZero { get; set; }


        public override void Validate(string value) {
            base.Validate(value);
            int parsedValue;
            if (!Int32.TryParse(value, out parsedValue)) {
                throw new FormatException("Value cannot be parsed as an integer.");
            }

            if (AlwaysAllowZero && parsedValue == 0) {
                return;
            }

            if (MinValue != int.MinValue && parsedValue < MinValue) {
                String msg = String.Format("Value is too low ({0}); expected at least {1}.", parsedValue, MinValue);
                throw new FormatException(msg);
            }

            if (MaxValue != int.MaxValue && parsedValue > MaxValue) {
                String msg = String.Format("Value is too high ({0}); expected at most {1}.", parsedValue, MaxValue);
                throw new FormatException(msg);
            }
        }
    }


    internal sealed class RankKeyAttribute : ConfigKeyAttribute {
        public RankKeyAttribute(ConfigSection section, BlankValueMeaning blankMeaning, [NotNull] string description)
            : base(section, typeof(Rank), "", description) {
            CanBeLowest = true;
            CanBeHighest = true;
            BlankMeaning = blankMeaning;
            NotBlank = false;
        }


        public bool CanBeLowest { get; set; }
        public bool CanBeHighest { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override void Validate(string value) {
            base.Validate(value);

            Rank rank;
            if (value.Length == 0) {
                rank = GetBlankValueSubstitute();
                if (rank == null) return; // ranks must not have loaded yet; can't validate
            } else {
                rank = Rank.Parse(value);
                if (rank == null) {
                    throw new FormatException("Value cannot be parsed as a rank.");
                }
            }
            if (!CanBeLowest && rank == RankManager.LowestRank) {
                throw new FormatException("Value may not be the lowest rank.");
            }
            if (!CanBeHighest && rank == RankManager.HighestRank) {
                throw new FormatException("Value may not be the highest rank.");
            }
        }


        public override string Process(string value) {
            if (value == null) throw new ArgumentNullException("value");
            if (value.Length == 0) {
                Rank defaultRank = GetBlankValueSubstitute();
                if (defaultRank == null) {
                    return "";
                } else {
                    return defaultRank.FullName;
                }
            } else {
                return value;
            }
        }


        // can be null if ranks have not loaded yet
        [CanBeNull]
        Rank GetBlankValueSubstitute() {
            switch (BlankMeaning) {
                case BlankValueMeaning.DefaultRank:
                    return RankManager.DefaultRank;
                case BlankValueMeaning.HighestRank:
                    return RankManager.HighestRank;
                case BlankValueMeaning.LowestRank:
                    return RankManager.LowestRank;
                case BlankValueMeaning.Invalid:
                    throw new FormatException("Value may not be blank.");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public enum BlankValueMeaning {
            Invalid,
            LowestRank,
            DefaultRank,
            HighestRank
        }
    }


    internal sealed class BoolKeyAttribute : ConfigKeyAttribute {
        public BoolKeyAttribute(ConfigSection section, bool defaultValue, [NotNull] string description)
            : base(section, typeof(bool), defaultValue, description) {}


        public override void Validate(string value) {
            base.Validate(value);
            bool test;
            if (value.Length != 0 && !Boolean.TryParse(value, out test)) {
                throw new FormatException("Value cannot be parsed as a boolean.");
            }
        }


        public override string Process(string value) {
            if (value.Length == 0) {
                return DefaultValue.ToString();
            } else {
                return value;
            }
        }
    }


    internal sealed class IPKeyAttribute : ConfigKeyAttribute {
        public IPKeyAttribute(ConfigSection section, BlankValueMeaning defaultMeaning, [NotNull] string description)
            : base(section, typeof(IPAddress), "", description) {
            BlankMeaning = defaultMeaning;
            switch (BlankMeaning) {
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


        public override void Validate(string value) {
            base.Validate(value);

            IPAddress test;
            if (value.Length == 0) {
                test = GetBlankValueSubstitute();
            } else if (!IPAddress.TryParse(value, out test)) {
                throw new FormatException("Value cannot be parsed as an IP Address.");
            }
            if (NotAny && test.Equals(IPAddress.Any)) {
                throw new FormatException(String.Format("Value cannot be {0}", IPAddress.Any));
            }
            if (NotNone && test.Equals(IPAddress.None)) {
                throw new FormatException(String.Format("Value cannot be {0}", IPAddress.None));
            }
            if (NotLoopback && IPAddress.IsLoopback(test)) {
                throw new FormatException("Value cannot be a loopback address.");
            }
            if (NotLAN && test.IsLocal()) {
                throw new FormatException("Value cannot be a local address.");
            }
        }


        [NotNull]
        IPAddress GetBlankValueSubstitute() {
            switch (BlankMeaning) {
                case BlankValueMeaning.Any:
                    return IPAddress.Any;
                case BlankValueMeaning.Loopback:
                    return IPAddress.Loopback;
                case BlankValueMeaning.None:
                    return IPAddress.None;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public override string Process(string value) {
            if (value.Length == 0) {
                return GetBlankValueSubstitute().ToString();
            } else {
                return value;
            }
        }


        public enum BlankValueMeaning {
            Any,
            Loopback,
            None
        }
    }


    internal sealed class ColorKeyAttribute : ConfigKeyAttribute {
        public ColorKeyAttribute(ConfigSection section, [NotNull] string defaultColor, [NotNull] string description)
            : base(section, typeof(string), ChatColor.White, description) {
            if (defaultColor == null) throw new ArgumentNullException("defaultColor");
            string defaultColorName = ChatColor.GetName(defaultColor);
            if (defaultColorName == null) {
                throw new ArgumentException("Default color must be a valid color name.");
            }
            DefaultValue = defaultColorName;
        }


        public override void Validate(string value) {
            base.Validate(value);
            string parsedValue = ChatColor.Parse(value);
            if (parsedValue == null) {
                throw new FormatException("Value cannot be parsed as a color.");
            } else if (parsedValue.Length == 0 && NotBlank) {
                throw new FormatException("Value may not represent absence of color.");
            }
        }
    }


    internal sealed class EnumKeyAttribute : ConfigKeyAttribute {
        public EnumKeyAttribute(ConfigSection section, [NotNull] object defaultValue, [NotNull] string description)
            : base(section, defaultValue.GetType(), defaultValue, description) {
            ValueType = defaultValue.GetType();
        }


        public override void Validate(string value) {
            base.Validate(value);
            if (!NotBlank && String.IsNullOrEmpty(value)) return;
            try {
                // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                Enum.Parse(ValueType, value, true);
                // ReSharper restore ReturnValueOfPureMethodIsNotUsed
            } catch (ArgumentException) {
                string message = String.Format("Could not parse value as {0}. Valid values are: {1}",
                                               ValueType.Name,
                                               Enum.GetNames(ValueType).JoinToString());
                throw new FormatException(message);
            }
        }


        public override string Process(string value) {
            if (value.Length == 0) {
                return DefaultValue.ToString();
            } else {
                return value;
            }
        }
    }
}
