using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace fCraft.Utils {
    class JSONObject : IDictionary<string,object> {
        readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public string Serialize() {
            StringBuilder sb = new StringBuilder();
            SerializeInternal( sb );
            return sb.ToString();
        }


        void SerializeInternal( StringBuilder sb ) {
            sb.Append( '{' );
            foreach( var kvp in data ) {
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
            } else if( obj is sbyte || obj is byte ||
                       obj is short || obj is ushort ||
                       obj is int || obj is uint ||
                       obj is long ) {
                sb.Append( (long)obj );
            } else if( obj is ulong ) {
                sb.Append( (ulong)obj );
            } else if( obj is float || obj is double || obj is decimal ) {
                sb.Append( (double)obj );
            } else if( obj is string ) {
                WriteString( sb, obj as string );
            } else if( obj is Array ) {
                WriteArray( sb, obj as Array );
            } else {
                WriteString( sb, obj.ToString() );
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


        #region IDictionary members etc

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
            return data.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return data.GetEnumerator();
        }


        public void Add( KeyValuePair<string, object> item ) {
            throw new NotImplementedException();
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


        public void Add( string key, object value ) {

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