using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Little JSON parsing/serialization library. </summary>
    public sealed class JsonObject : IDictionary<string, object>, ICloneable {
        readonly Dictionary<string, object> data = new Dictionary<string, object>();


        #region Parsing

        int index;
        string str;
        readonly StringBuilder stringParserBuffer = new StringBuilder();
        readonly List<object> arrayParserBuffer = new List<object>();

        /// <summary> Creates an empty JSONObject. </summary>
        public JsonObject() {}


        /// <summary> Creates a JSONObject from a serialized string. </summary>
        /// <param name="inputString"> Serialized JSON object to parse. </param>
        /// <exception cref="ArgumentNullException"> If inputString is null. </exception>
        public JsonObject( [NotNull] string inputString ) {
            if( inputString == null ) throw new ArgumentNullException( "inputString" );
            ReadJSONObject( inputString, 0 );
            Token token = FindNextToken();
            if( token != Token.None ) {
                ThrowUnexpected( token, "None" );
            }
            stringParserBuffer = null;
            arrayParserBuffer = null;
        }


        int ReadJSONObject( string inputString, int offset ) {
            str = inputString;
            index = offset;
            Token token = FindNextToken();
            if( token != Token.BeginObject ) {
                ThrowUnexpected( token, "BeginObject" );
            }

            index++;
            bool first = true;
            do {
                token = FindNextToken();

                if( token == Token.EndObject ) {
                    index++;
                    return index;
                }

                if( first ) {
                    first = false;
                } else if( token == Token.ValueSeparator ) {
                    index++;
                    token = FindNextToken();
                } else {
                    ThrowUnexpected( token, "EndObject or ValueSeparator" );
                }

                if( token != Token.String ) {
                    ThrowUnexpected( token, "String" );
                }

                string key = ReadString();
                token = FindNextToken();
                if( token != Token.NameSeparator ) {
                    ThrowUnexpected( token, "NameSeparator" );
                }
                index++;
                object value = ReadValue();
                Add( key, value );
            } while( token != Token.None );
            return index;
        }


        string ReadString() {
            stringParserBuffer.Length = 0;
            index++;

            for( int start = -1; index < str.Length - 1; index++ ) {
                char c = str[index];

                if( c == '"' ) {
                    if( start != -1 && start != index ) {
                        if( stringParserBuffer.Length == 0 ) {
                            index++;
                            return str.Substring( start, index - start - 1 );
                        } else {
                            stringParserBuffer.Append( str, start, index - start );
                        }
                    }
                    index++;
                    return stringParserBuffer.ToString();
                }

                if( c == '\\' ) {
                    if( start != -1 && start != index ) {
                        stringParserBuffer.Append( str, start, index - start );
                        start = -1;
                    }
                    index++;
                    if( index >= str.Length - 1 ) break;
                    switch( str[index] ) {
                        case '"':
                        case '/':
                        case '\\':
                            start = index;
                            continue;
                        case 'b':
                            stringParserBuffer.Append( '\b' );
                            continue;
                        case 'f':
                            stringParserBuffer.Append( '\f' );
                            continue;
                        case 'n':
                            stringParserBuffer.Append( '\n' );
                            continue;
                        case 'r':
                            stringParserBuffer.Append( '\r' );
                            continue;
                        case 't':
                            stringParserBuffer.Append( '\t' );
                            continue;
                        case 'u':
                            if( index >= str.Length - 5 ) break;
                            uint c0 = ReadHexChar( str[index + 1], 0x1000 );
                            uint c1 = ReadHexChar( str[index + 2], 0x0100 );
                            uint c2 = ReadHexChar( str[index + 3], 0x0010 );
                            uint c3 = ReadHexChar( str[index + 4], 0x0001 );
                            stringParserBuffer.Append( (char)( c0 + c1 + c2 + c3 ) );
                            index += 4;
                            continue;
                    }
                }

                if( c < ' ' ) {
                    throw new SerializationException( "JSONObject: Unexpected character: " +
                                                      ( (int)c ).ToString( "X4", NumberFormatInfo.InvariantInfo ) + "." );
                }

                if( start == -1 ) start = index;
            }
            throw new SerializationException( "JSONObject: Unexpected end of string." );
        }


        static uint ReadHexChar( char ch, uint multiplier ) {
            if( ch >= '0' && ch <= '9' )
                return (uint)( ch - '0' ) * multiplier;
            else if( ch >= 'A' && ch <= 'F' )
                return (uint)( ( ch - 'A' ) + 10 ) * multiplier;
            else if( ch >= 'a' && ch <= 'f' )
                return (uint)( ( ch - 'a' ) + 10 ) * multiplier;
            throw new SerializationException( "JSONObject: Incorrectly specified Unicode entity." );
        }


        object ReadValue() {
            switch( FindNextToken() ) {
                case Token.BeginObject:
                    JsonObject newObj = new JsonObject();
                    index = newObj.ReadJSONObject( str, index );
                    return newObj;

                case Token.String:
                    return ReadString();

                case Token.Number:
                    return ReadNumber();

                case Token.Null:
                    if( index >= str.Length - 4 ||
                        str[index + 1] != 'u' || str[index + 2] != 'l' || str[index + 3] != 'l' ) {
                        throw new SerializationException( "JSONObject: Expected 'null'." );
                    }
                    index += 4;
                    return null;

                case Token.True:
                    if( index >= str.Length - 4 ||
                        str[index + 1] != 'r' || str[index + 2] != 'u' || str[index + 3] != 'e' ) {
                        throw new SerializationException( "JSONObject: Expected 'true'." );
                    }
                    index += 4;
                    return true;

                case Token.False:
                    if( index >= str.Length - 5 ||
                        str[index + 1] != 'a' || str[index + 2] != 'l' || str[index + 3] != 's' || str[index + 4] != 'e' ) {
                        throw new SerializationException( "JSONObject: Expected 'false'." );
                    }
                    index += 5;
                    return false;

                case Token.BeginArray:
                    arrayParserBuffer.Clear();
                    index++;
                    bool first = true;
                    while( true ) {
                        Token token = FindNextToken();
                        if( token == Token.EndArray ) break;
                        if( first ) {
                            first = false;
                        } else if( token == Token.ValueSeparator ) {
                            index++;
                            token = FindNextToken();
                        } else {
                            ThrowUnexpected( token, "ValueSeparator" );
                        }
                        arrayParserBuffer.Add( ReadValue() );
                    }
                    index++;
                    return arrayParserBuffer.ToArray();
            }
            throw new SerializationException( "JSONObject: Unexpected token; expected a value." );
        }


        object ReadNumber() {
            long val = 1;
            double doubleVal = Double.NaN;
            bool first = true;
            bool negate = false;

            // Parse sign
            char c = str[index];
            if( str[index] == '-' ) {
                c = str[++index];
                negate = true;
            }

            // Parse integer part
            while( index < str.Length ) {
                if(first) {
                    if( c == '0' ) {
                        val = 0;
                        c = str[++index];
                        break;
                    } else if( c >= '1' && c <= '9' ) {
                        val = ( c - '0' );
                    }
                } else if( c >= '0' && c <= '9' ) {
                    val *= 10;
                    val += ( c - '0' );
                } else {
                    break;
                }
                first = false;
                c = str[++index];
            }
            if( index >= str.Length ) {
                throw new SerializationException( "JSONObject: Unexpected end of a number (before decimal point)." );
            }

            // Parse fractional part (if present)
            if( c == '.' ) {
                c = str[++index];
                double fraction = 0;
                int multiplier = 10;
                first = true;
                while( index < str.Length ) {
                    if( c >= '0' && c <= '9' ) {
                        fraction += ( c - '0' ) / (double)multiplier;
                        multiplier *= 10;
                    } else if( first ) {
                        throw new SerializationException( "JSONObject: Expected at least one digit after the decimal point." );
                    } else {
                        break;
                    }
                    c = str[++index];
                    first = false;
                }
                if( index >= str.Length ) {
                    throw new SerializationException( "JSONObject: Unexpected end of a number (after decimal point)." );
                }
                doubleVal = val + fraction;
                // Negate (if needed)
                if( negate ) doubleVal = -doubleVal;

            } else {
                if( negate ) val = -val;
            }

            // Parse exponent (if present)
            if( c == 'e' || c == 'E' ) {
                int exponent = 1;
                negate = false;

                // Exponent sign
                c = str[++index];
                if( c == '-' ) {
                    negate = true;
                    c = str[++index];
                } else if( c == '+' ) {
                    c = str[++index];
                }

                // Exponent value
                first = true;
                while( index < str.Length ) {
                    if(first){
                        if( c == '0' ) {
                            exponent = 0;
                            index++;
                            break;
                        } else if( c >= '1' && c <= '9' ) {
                            exponent = ( c - '0' );
                        } else {
                            throw new SerializationException( "JSONObject: Unexpected character in exponent." );
                        }
                    } else if( c >= '0' && c <= '9' ) {
                        exponent *= 10;
                        exponent += ( c - '0' );
                    }else{
                        break;
                    }
                    first = false;
                    c = str[++index];
                }
                if( index >= str.Length ) {
                    throw new SerializationException( "JSONObject: Unexpected end of a number (exponent)." );
                }

                // Apply the exponent
                if( negate ) {
                    exponent = -exponent;
                }
                if( Double.IsNaN( doubleVal ) ) {
                    doubleVal = val;
                }
                doubleVal *= Math.Pow( 10, exponent );
            }

            // Return value in appropriate format
            if( !Double.IsNaN( doubleVal ) ) {
                return doubleVal;
            } else if( val >= Int32.MinValue && val <= Int32.MaxValue ) {
                return (int)val;
            } else {
                return val;
            }
        }


        enum Token {
            None,
            Error,

            BeginObject,
            EndObject,
            BeginArray,
            EndArray,
            NameSeparator,
            ValueSeparator,
            Null,
            True,
            False,
            String,
            Number
        }


        Token FindNextToken() {
            if( index >= str.Length ) return Token.None;
            char c = str[index];
            while( c == ' ' || c == '\t' || c == '\r' || c == '\n' ) {
                index++;
                if( index >= str.Length ) return Token.None;
                c = str[index];
            }
            switch( c ) {
                case '{':
                    return Token.BeginObject;
                case '}':
                    return Token.EndObject;
                case '[':
                    return Token.BeginArray;
                case ']':
                    return Token.EndArray;
                case 'n':
                    return Token.Null;
                case 't':
                    return Token.True;
                case 'f':
                    return Token.False;
                case ':':
                    return Token.NameSeparator;
                case ',':
                    return Token.ValueSeparator;
                case '"':
                    return Token.String;
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return Token.Number;
                default:
                    return Token.Error;
            }
        }


        [TerminatesProgram]
        static void ThrowUnexpected( Token given, string expected ) {
            throw new SerializationException( "JSONObject: Unexpected token " + given + ", expecting " + expected + "." );
        }

        #endregion


        #region Serialization

        sealed class JSONSerializer {
            readonly int indent = 2;
            readonly bool compact;
            int indentLevel;
            readonly StringBuilder sb = new StringBuilder();

            public JSONSerializer() {}


            public JSONSerializer( int indent ) {
                this.indent = indent;
                compact = ( indent < 0 );
            }


            public string Serialize( [NotNull] JsonObject obj ) {
                sb.Clear();
                indentLevel = 0;
                SerializeInternal( obj );
                return sb.ToString();
            }


            void Indent() {
                sb.Append( '\r' ).Append( '\n' ).Append( ' ', indentLevel * indent );
            }


            void SerializeInternal( [NotNull] JsonObject obj ) {
                sb.Append( '{' );
                if( obj.data.Count > 0 ) {
                    indentLevel++;
                    bool first = true;
                    foreach( var kvp in obj.data ) {
                        if( first ) {
                            first = false;
                        } else {
                            sb.Append( ',' );
                        }
                        if( !compact ) Indent();
                        WriteString( kvp.Key );
                        sb.Append( ':' );
                        if( !compact ) sb.Append( ' ' );
                        WriteValue( kvp.Value );
                    }
                    indentLevel--;
                    if( !compact ) {
                        Indent();
                    }
                }
                sb.Append( '}' );
            }


            void WriteValue( [CanBeNull] object obj ) {
                JsonObject asObject;
                string asString;
                Array asArray;
                if( obj == null ) {
                    sb.Append( "null" );
                } else if( (asObject = obj as JsonObject) != null ) {
                    SerializeInternal( asObject );
                } else if( obj is int ) {
                    sb.Append( ( (int)obj ).ToString( NumberFormatInfo.InvariantInfo ) );
                } else if( obj is double ) {
                    sb.Append( ( (double)obj ).ToString( NumberFormatInfo.InvariantInfo ) );
                } else if( ( asString = obj as string ) != null ) {
                    WriteString( asString );
                } else if( ( asArray = obj as Array ) != null ) {
                    WriteArray( asArray );
                } else if( obj is long ) {
                    sb.Append( ( (long)obj ).ToString( NumberFormatInfo.InvariantInfo ) );
                } else if( obj is bool ) {
                    if( (bool)obj ) {
                        sb.Append( "true" );
                    } else {
                        sb.Append( "false" );
                    }
                } else {
                    throw new InvalidOperationException( "JSONObject: Non-serializable object found in the collection." );
                }
            }


            void WriteString( [NotNull] string str ) {
                sb.Append( '"' );
                int runIndex = -1;

                for( var i = 0; i < str.Length; i++ ) {
                    var c = str[i];

                    if( c >= ' ' && c < 128 && c != '\"' && c != '\\' ) {
                        if( runIndex == -1 ) {
                            runIndex = i;
                        }
                        continue;
                    }

                    if( runIndex != -1 ) {
                        sb.Append( str, runIndex, i - runIndex );
                        runIndex = -1;
                    }

                    sb.Append( '\\' );
                    switch( c ) {
                        case '\b':
                            sb.Append( 'b' );
                            break;
                        case '\f':
                            sb.Append( 'f' );
                            break;
                        case '\n':
                            sb.Append( 'n' );
                            break;
                        case '\r':
                            sb.Append( 'r' );
                            break;
                        case '\t':
                            sb.Append( 't' );
                            break;
                        case '"':
                        case '\\':
                            sb.Append( c );
                            break;
                        default:
                            sb.Append( 'u' );
                            sb.Append( ( (int)c ).ToString( "X4", NumberFormatInfo.InvariantInfo ) );
                            break;
                    }
                }

                if( runIndex != -1 ) {
                    sb.Append( str, runIndex, str.Length - runIndex );
                }

                sb.Append( '\"' );
            }


            void WriteArray( [NotNull] Array array ) {
                sb.Append( '[' );
                bool first = true;
                for( int i = 0; i < array.Length; i++ ) {
                    if( first ) {
                        first = false;
                    } else {
                        sb.Append( ',' );
                        if( !compact ) sb.Append( ' ' );
                    }
                    WriteValue( array.GetValue( i ) );
                }
                sb.Append( ']' );
            }
        }


        /// <summary> Serializes this JSONObject with default settings. </summary>
        [NotNull]
        public string Serialize() {
            return new JSONSerializer().Serialize( this );
        }


        /// <summary> Serializes this JSONObject with custom indentation. </summary>
        /// <param name="indent"> Number of spaces to use for indentation.
        /// If zero or positive, padding and line breaks are added.
        /// If negative, serialization is done as compactly as possible. </param>
        [NotNull]
        public string Serialize( int indent ) {
            return new JSONSerializer( indent ).Serialize( this );
        }

        #endregion


        #region Has/Get/TryGet shortcuts

        // ==== non-cast ====
        public bool Has( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return data.ContainsKey( key );
        }


        public bool HasNull( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null );
        }


        public object Get( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return data[key];
        }


        public bool TryGet( [NotNull] string key, [CanBeNull] out object val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return data.TryGetValue( key, out val );
        }


        // ==== strings ====
        public string GetString( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (string)data[key];
        }


        public bool TryGetString( [NotNull] string key, [NotNull] out string val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as string );
            return ( val != null );
        }


        public bool TryGetStringOrNull( [NotNull] string key, [CanBeNull] out string val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = null;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( boxedVal == null ) {
                return true;
            }
            val = ( boxedVal as string );
            return ( val != null );
        }


        public bool HasString( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as string != null );
        }


        public bool HasStringOrNull( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as string != null );
        }


        // ==== integers ====
        public int GetInt( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (int)data[key];
        }


        public bool TryGetInt( [NotNull] string key, out int val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is int ) ) return false;
            val = (int)boxedVal;
            return true;
        }


        public bool HasInt( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is int );
        }


        // ==== longs ====
        public long GetLong( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (long)data[key];
        }


        public bool TryGetLong( [NotNull] string key, out long val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is long ) ) return false;
            val = (long)boxedVal;
            return true;
        }


        public bool HasLong( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is long );
        }


        // ==== double ====
        public double GetDouble( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (double)data[key];
        }


        public bool TryGetDouble( [NotNull] string key, out double val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is double ) ) return false;
            val = (double)boxedVal;
            return true;
        }


        public bool HasDouble( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is double );
        }


        // ==== boolean ====
        public bool GetBool( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (bool)data[key];
        }


        public bool TryGetBool( [NotNull] string key, out bool val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = false;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is bool ) ) return false;
            val = (bool)boxedVal;
            return true;
        }


        public bool HasBool( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is bool );
        }


        // ==== JSONObject ====
        [CanBeNull]
        public JsonObject GetObject( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return (JsonObject)data[key];
        }


        public bool TryGetObject( [NotNull] string key, [NotNull] out JsonObject val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as JsonObject );
            return ( val != null );
        }


        public bool TryGetObjectOrNull( [NotNull] string key, [CanBeNull] out JsonObject val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = null;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( boxedVal == null ) {
                return true;
            }
            val = ( boxedVal as JsonObject );
            return ( val != null );
        }


        public bool HasObject( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as JsonObject != null );
        }


        public bool HasObjectOrNull( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as JsonObject != null );
        }


        // ==== Array ====
        [CanBeNull]
        public T[] GetArray<T>( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            if( data[key] is T[] ) {
                return (T[])data[key];
            } else {
                return ( (object[])data[key] ).Cast<T>().ToArray();
            }
        }


        public bool TryGetArray<T>( [NotNull] string key, [NotNull] out T[] val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            try {
                val = GetArray<T>( key );
                return true;
            } catch( InvalidCastException ) {
                val = null;
                return false;
            }
        }


        public bool TryGetArrayOrNull<T>( [NotNull] string key, [CanBeNull] out T[] val ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            val = null;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( boxedVal == null ) {
                return true;
            }
            try {
                val = GetArray<T>( key );
                return true;
            } catch( InvalidCastException ) {
                val = null;
                return false;
            }
        }


        public bool HasArray( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as object[] != null );
        }


        public bool HasArrayOrNull( [NotNull] string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as object[] != null );
        }

        #endregion


        #region IDictionary / ICollection / ICloneable members


        /// <summary> Creates a JsonObject from an existing JsonObject or string-object dictionary. </summary>
        public JsonObject( IEnumerable<KeyValuePair<string, object>> other ) {
            foreach( var kvp in other ) {
                Add( kvp );
            }
        }


        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return data.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return data.GetEnumerator();
        }


        public void Add( KeyValuePair<string, object> item ) {
            Add( item.Key, item.Value );
        }


        public void Clear() {
            data.Clear();
        }


        bool ICollection<KeyValuePair<string, object>>.Contains( KeyValuePair<string, object> item ) {
            return ( data as ICollection<KeyValuePair<string, object>> ).Contains( item );
        }


        void ICollection<KeyValuePair<string, object>>.CopyTo( KeyValuePair<string, object>[] array, int arrayIndex ) {
            ( data as ICollection<KeyValuePair<string, object>> ).CopyTo( array, arrayIndex );
        }


        bool ICollection<KeyValuePair<string, object>>.Remove( KeyValuePair<string, object> item ) {
            return ( data as ICollection<KeyValuePair<string, object>> ).Remove( item );
        }


        public int Count {
            get { return data.Count; }
        }


        bool ICollection<KeyValuePair<string, object>>.IsReadOnly {
            get { return false; }
        }


        public bool ContainsKey( string key ) {
            return data.ContainsKey( key );
        }


        public void Add( [NotNull] string key, JsonObject obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, int obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, long obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, double obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, bool obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, string obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, Array obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            data.Add( key, obj );
        }


        public void Add( [NotNull] string key, object obj ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            if( obj == null || obj is JsonObject ||
                obj is string || obj is int ||
                obj is long || obj is double || obj is bool || obj is Array ) {
                data.Add( key, obj );
            } else if( obj is sbyte ) {
                data.Add( key, (int)(sbyte)obj );
            } else if( obj is byte ) {
                data.Add( key, (int)(byte)obj );
            } else if( obj is short ) {
                data.Add( key, (int)(short)obj );
            } else if( obj is ushort ) {
                data.Add( key, (int)(ushort)obj );
            } else if( obj is uint ) {
                data.Add( key, (long)(uint)obj );
            } else if( obj is float ) {
                data.Add( key, (double)(float)obj );
            } else if( obj is decimal ) {
                data.Add( key, (double)(decimal)obj );
            } else {
                throw new ArgumentException( "JSONObject: Unacceptable value type." );
            }
        }


        public bool Remove( string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return data.Remove( key );
        }


        bool IDictionary<string, object>.TryGetValue( string key, out object value ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            return data.TryGetValue( key, out value );
        }


        public object this[ string key ] {
            get {
                if( key == null ) throw new ArgumentNullException( "key" );
                return data[key];
            }
            set {
                if( key == null ) throw new ArgumentNullException( "key" );
                data[key] = value;
            }
        }


        public ICollection<string> Keys {
            get { return data.Keys; }
        }


        public ICollection<object> Values {
            get { return data.Values; }
        }


        public object Clone() {
            return new JsonObject( this );
        }

        #endregion
    }
}