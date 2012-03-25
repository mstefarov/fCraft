using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    public class JSONObject : IDictionary<string, object> {
        readonly Dictionary<string, object> data = new Dictionary<string, object>();


        #region Parsing

        int index;
        Token token;
        string str;

        public JSONObject() { }


        public JSONObject( string inputString ) {
            ReadJSONObject( inputString, 0 );
            token = FindNextToken();
            if( token != Token.None ) {
                ThrowUnexpected( token, "None" );
            }
        }


        int ReadJSONObject( string inputString, int offset ) {
            str = inputString;
            index = offset;
            token = FindNextToken();
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
                token = FindNextToken();
                object value = ReadValue();
                Add( key, value );
            } while( token != Token.None );
            return index;
        }


        string ReadString() {
            StringBuilder sb = new StringBuilder();
            index++;

            for( int start = -1; index < str.Length - 1; index++ ) {
                char c = str[index];

                if( c == '"' ) {
                    if( start != -1 && start != index ) {
                        sb.Append( str, start, index - start );
                        index++;
                        return sb.ToString();
                    }
                }

                if( c == '\\' ) {
                    if( start != -1 && start != index ) {
                        sb.Append( str, start, index - start );
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
                            sb.Append( '\b' );
                            continue;
                        case 'f':
                            sb.Append( '\f' );
                            continue;
                        case 'n':
                            sb.Append( '\n' );
                            continue;
                        case 'r':
                            sb.Append( '\r' );
                            continue;
                        case 't':
                            sb.Append( '\t' );
                            continue;
                        case 'u':
                            if( index >= str.Length - 5 ) break;
                            uint c0 = ReadHexChar( str[index + 1], 0x1000 );
                            uint c1 = ReadHexChar( str[index + 2], 0x0100 );
                            uint c2 = ReadHexChar( str[index + 3], 0x0010 );
                            uint c3 = ReadHexChar( str[index + 4], 0x0001 );
                            sb.Append( (char)( c0 + c1 + c2 + c3 ) );
                            index += 3;
                            continue;
                    }
                }

                if( c >= ' ' ) {
                    if( start == -1 ) start = index;
                    continue;
                }

                throw new SerializationException( "Unexpected character: " + ( (int)c ).ToString( "X4", NumberFormatInfo.InvariantInfo ) );
            }
            throw new SerializationException( "Unexpected end of string" );
        }


        uint ReadHexChar( char ch, uint multiplier ) {
            if( ch >= '0' && ch <= '9' )
                return (uint)( ch - '0' ) * multiplier;
            else if( ch >= 'A' && ch <= 'F' )
                return (uint)( ( ch - 'A' ) + 10 ) * multiplier;
            else if( ch >= 'a' && ch <= 'f' )
                return (uint)( ( ch - 'a' ) + 10 ) * multiplier;
            throw new SerializationException( "Unexpected Unicode entity" );
        }


        object ReadValue() {
            switch( token ) {
                case Token.BeginObject:
                    JSONObject newObj = new JSONObject();
                    index = newObj.ReadJSONObject( str, index );
                    return newObj;

                case Token.String:
                    return ReadString();

                case Token.Number:
                    return ReadNumber();

                case Token.Null:
                    if( index >= str.Length - 4 ||
                        str[index + 1] != 'u' || str[index + 2] != 'l' || str[index + 3] != 'l' ) {
                        throw new SerializationException( "Unexpected token; expected 'null'" );
                    }
                    index += 4;
                    return null;

                case Token.True:
                    if( index >= str.Length - 4 ||
                        str[index + 1] != 'r' || str[index + 2] != 'u' || str[index + 3] != 'e' ) {
                        throw new SerializationException( "Unexpected token; expected 'true'" );
                    }
                    index += 4;
                    return true;

                case Token.False:
                    if( index >= str.Length - 5 ||
                        str[index + 1] != 'a' || str[index + 2] != 'l' || str[index + 3] != 's' || str[index + 4] != 'e' ) {
                        throw new SerializationException( "Unexpected token; expected 'false'" );
                    }
                    index += 5;
                    return false;

                case Token.BeginArray:
                    index++;
                    List<object> list = new List<object>();
                    bool first = true;
                    while( true ) {
                        token = FindNextToken();
                        if( token == Token.EndArray ) break;
                        if( first ) {
                            first = false;
                        } else if( token == Token.ValueSeparator ) {
                            index++;
                            token = FindNextToken();
                        } else {
                            ThrowUnexpected( token, "ValueSeparator" );
                        }
                        list.Add( ReadValue() );
                    }
                    index++;
                    return list.ToArray();
            }
            throw new SerializationException( "Unexpected token " + token + "; expected value" );
        }


        object ReadNumber() {
            int start = index;
            while( FindNextToken() == Token.Number ) index++;
            string numberString = str.Substring( start, index - start );
            int tryInt;
            if( Int32.TryParse( numberString, out tryInt ) ) {
                return tryInt;
            }
            long tryLong;
            if( Int64.TryParse( numberString, out tryLong ) ) {
                return tryLong;
            }
            double tryDouble;
            if( Double.TryParse( numberString, out tryDouble ) ) {
                return tryDouble;
            }
            throw new SerializationException( "Invalid number format" );
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
                case '+':
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
                case '.':
                case 'e':
                case 'E':
                    return Token.Number;
                default:
                    return Token.Error;
            }
        }


        [TerminatesProgram]
        static void ThrowUnexpected( Token given, string expected ) {
            throw new SerializationException( "JSON: Unexpected token " + given +
                                              ", expecting " + expected );
        }

        #endregion


        #region Serialization

        public string Serialize() {
            StringBuilder sb = new StringBuilder();
            SerializeInternal( sb );
            return sb.ToString();
        }


        void SerializeInternal( StringBuilder sb ) {
            sb.Append( '{' );
            bool first = true;
            foreach( var kvp in data ) {
                if( first ) {
                    first = false;
                } else {
                    sb.Append( ',' );
                }
                WriteString( sb, kvp.Key );
                sb.Append( ':' );
                WriteValue( sb, kvp.Value );
            }
            sb.Append( '}' );
        }


        void WriteValue( StringBuilder sb, object obj ) {
            if( obj == null ) {
                sb.Append( "null" );
            } else if( obj is JSONObject ) {
                ( obj as JSONObject ).SerializeInternal( sb );
            } else if( obj is bool ) {
                if( (bool)obj ) {
                    sb.Append( "true" );
                } else {
                    sb.Append( "false" );
                }
            } else if( obj is int ) {
                sb.Append( (int)obj );
            } else if( obj is long ) {
                sb.Append( (long)obj );
            } else if( obj is double ) {
                sb.Append( (double)obj );
            } else if( obj is string ) {
                WriteString( sb, obj as string );
            } else if( obj is IList ) {
                WriteArray( sb, obj as IList );
            } else {
                throw new InvalidOperationException( "Non-serializable object found in collection" );
            }
        }


        void WriteString( StringBuilder sb, string str ) {
            sb.Append( '\"' );
            int runIndex = -1;

            for( var index = 0; index < str.Length; index++ ) {
                var c = str[index];

                if( c >= ' ' && c < 128 && c != '\"' && c != '\\' ) {
                    if( runIndex == -1 ) {
                        runIndex = index;
                    }
                    continue;
                }

                if( runIndex != -1 ) {
                    sb.Append( str, runIndex, index - runIndex );
                    runIndex = -1;
                }

                switch( c ) {
                    case '\t': sb.Append( "\\t" ); break;
                    case '\r': sb.Append( "\\r" ); break;
                    case '\n': sb.Append( "\\n" ); break;
                    case '"':
                    case '\\': sb.Append( '\\' ); sb.Append( c ); break;
                    default:
                        sb.Append( "\\u" );
                        sb.Append( ( (int)c ).ToString( "X4", NumberFormatInfo.InvariantInfo ) );
                        break;
                }
            }

            if( runIndex != -1 ) {
                sb.Append( str, runIndex, str.Length - runIndex );
            }

            sb.Append( '\"' );
        }


        void WriteArray( StringBuilder sb, IList array ) {
            sb.Append( '[' );
            bool first = true;
            foreach( var element in array ) {
                if( first ) {
                    first = false;
                } else {
                    sb.Append( ',' );
                }
                WriteValue( sb, element );
            }
            sb.Append( ']' );
        }

        #endregion


        #region Has/Get/TryGet shortcuts

        // ==== non-cast ====
        public bool Has( string key ) {
            return data.ContainsKey( key );
        }


        public bool HasNull( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null );
        }


        public object Get( string key ) {
            return data[key];
        }


        public bool TryGet( string key, out object val ) {
            return data.TryGetValue( key, out val );
        }


        // ==== strings ====
        public string GetString( string key ) {
            return (string)data[key];
        }


        public bool TryGetString( string key, out string val ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as string );
            return ( val != null );
        }


        public bool TryGetStringOrNull( string key, out string val ) {
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


        public bool HasString( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as string != null );
        }


        public bool HasStringOrNull( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as string != null );
        }


        // ==== integers ====
        public int GetInt( string key ) {
            return (int)data[key];
        }


        public bool TryGetInt( string key, out int val ) {
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is int ) ) return false;
            val = (int)boxedVal;
            return true;
        }


        public bool HasInt( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is int );
        }


        // ==== longs ====
        public long GetLong( string key ) {
            return (long)data[key];
        }


        public bool TryGetLong( string key, out long val ) {
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is long ) ) return false;
            val = (long)boxedVal;
            return true;
        }


        public bool HasLong( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is long );
        }


        // ==== boolean ====
        public bool GetBool( string key ) {
            return (bool)data[key];
        }


        public bool TryGetBool( string key, out bool val ) {
            val = false;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is bool ) ) return false;
            val = (bool)boxedVal;
            return true;
        }


        public bool HasBool( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is bool );
        }


        // ==== double ====
        public double GetDouble( string key ) {
            return (double)data[key];
        }


        public bool TryGetDouble( string key, out double val ) {
            val = 0;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is double ) ) return false;
            val = (double)boxedVal;
            return true;
        }


        public bool HasDouble( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is double );
        }


        // ==== JSONObject ====
        public JSONObject GetObject( string key ) {
            return (JSONObject)data[key];
        }


        public bool TryGetObject( string key, out JSONObject val ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as JSONObject );
            return ( val != null );
        }


        public bool TryGetObjectOrNull( string key, out JSONObject val ) {
            val = null;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( boxedVal == null ) {
                return true;
            }
            val = ( boxedVal as JSONObject );
            return ( val != null );
        }


        public bool HasObject( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as JSONObject != null );
        }


        public bool HasObjectOrNull( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as JSONObject != null );
        }


        // ==== Array ====
        public Array GetArray( string key ) {
            return (Array)data[key];
        }


        public bool TryGetArray( string key, out Array val ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as Array );
            return ( val != null );
        }


        public bool TryGetArrayOrNull( string key, out Array val ) {
            val = null;
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( boxedVal == null ) {
                return true;
            }
            val = ( boxedVal as Array );
            return ( val != null );
        }


        public bool HasArray( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as Array != null );
        }


        public bool HasArrayOrNull( string key ) {
            object boxedVal;
            if( !data.TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as Array != null );
        }

        #endregion


        #region IDictionary members etc

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


        public void Add( string key, object obj ) {
            if( obj == null || obj is JSONObject ||
                obj is int || obj is long || obj is double ||
                obj is bool || obj is string || obj is IList ) {
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
                throw new ArgumentException( "Unacceptable value type." );
            }
        }


        public bool Remove( string key ) {
            return data.Remove( key );
        }


        bool IDictionary<string,object>.TryGetValue( string key, out object value ) {
            return data.TryGetValue( key, out value );
        }


        public object this[ string key ] {
            get { return data[key]; }
            set { data[key] = value; }
        }


        public ICollection<string> Keys {
            get { return data.Keys; }
        }


        public ICollection<object> Values {
            get { return data.Values; }
        }

        #endregion
    }
}