using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Utils {
    class JSONObject : Dictionary<string, object> {
        // ==== non-cast ====
        public bool Has( string key ) {
            return ContainsKey( key );
        }


        public bool HasNull( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null );
        }


        public object Get( string key ) {
            return this[key];
        }


        public bool TryGet( string key, out object val ) {
            return TryGetValue( key, out val );
        }


        // ==== strings ====
        public string GetString( string key ) {
            return (string)this[key];
        }


        public bool TryGetString( string key, out string val ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as string );
            return ( val != null );
        }


        public bool TryGetStringOrNull( string key, out string val ) {
            val = null;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
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
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as string != null );
        }


        public bool HasStringOrNull( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as string != null );
        }


        // ==== integers ====
        public int GetInt( string key ) {
            return (int)this[key];
        }


        public bool TryGetInt( string key, out int val ) {
            val = 0;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is int ) ) return false;
            val = (int)boxedVal;
            return true;
        }


        public bool HasInt( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is int );
        }


        // ==== boolean ====
        public bool GetBool( string key ) {
            return (bool)this[key];
        }


        public bool TryGetBool( string key, out bool val ) {
            val = false;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is bool ) ) return false;
            val = (bool)boxedVal;
            return true;
        }


        public bool HasBool( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is bool );
        }


        // ==== double ====
        public double GetDouble( string key ) {
            return (double)this[key];
        }


        public bool TryGetDouble( string key, out double val ) {
            val = 0;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            if( !( boxedVal is double ) ) return false;
            val = (double)boxedVal;
            return true;
        }


        public bool HasDouble( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal is double );
        }


        // ==== JSONObject ====
        public JSONObject GetObject( string key ) {
            return (JSONObject)this[key];
        }


        public bool TryGetObject( string key, out JSONObject val ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as JSONObject );
            return ( val != null );
        }


        public bool TryGetObjectOrNull( string key, out JSONObject val ) {
            val = null;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
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
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as JSONObject != null );
        }


        public bool HasObjectOrNull( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as JSONObject != null );
        }


        // ==== Array ====
        public Array GetArray( string key ) {
            return (Array)this[key];
        }


        public bool TryGetArray( string key, out Array val ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                val = null;
                return false;
            }
            val = ( boxedVal as Array );
            return ( val != null );
        }


        public bool TryGetArrayOrNull( string key, out Array val ) {
            val = null;
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
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
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal as Array != null );
        }


        public bool HasArrayOrNull( string key ) {
            object boxedVal;
            if( !TryGetValue( key, out boxedVal ) ) {
                return false;
            }
            return ( boxedVal == null ) || ( boxedVal as Array != null );
        }
    }
}