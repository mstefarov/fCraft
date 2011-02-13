// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;


namespace fCraft {
    /// <summary>
    /// Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB.
    /// </summary>
    public sealed class StringTree<T> {
        StringNode root = new StringNode();
        public int Count { private set; get; }

        public const byte MULTI = 37, EMPTY=38;


        /// <summary> Get payload for an exact name (no autocompletion) </summary>
        /// <param name="name">Full name</param>
        /// <returns>Payload object, if found. Null (or default) if not found.</returns>
        public T Get( string name ) {
            StringNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.children[code] == null )
                    return default(T);
                temp = temp.children[code];
            }
            return temp.payload;
        }


        /// <summary>
        /// Searches for players starting with namePart. Autocompletes.
        /// </summary>
        /// <param name="namePart">Partial or full player name</param>
        /// <param name="limit">Limit on the number of player names to return</param>
        /// <returns>List of matches (if there are no matches, length is zero)</returns>
        public List<T> GetMultiple( string namePart, int limit ) {
            List<T> results = new List<T>();
            StringNode temp = root;
            for( int i = 0; i < namePart.Length; i++ ) {
                int code = CharCode( namePart[i] );
                if( temp.children[code] == null )
                    return results;
                temp = temp.children[code];
            }
            temp.GetAllChildren( results, limit );
            return results;
        }


        /// <summary>Searches for payloads with names that start with namePart, returning just one or none of the matches.</summary>
        /// <param name="namePart">Partial or full name</param>
        /// <param name="info">Payload object to output (will be set to null/default(T) if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public bool Get( string namePart, out T info ) {
            StringNode temp = root;
            for( int i = 0; i < namePart.Length; i++ ) {
                int code = CharCode( namePart[i] );
                if( temp.children[code] == null ) {
                    info = default(T);
                    return true; // early detection of no matches
                }
                temp = temp.children[code];
            }

            if( temp.payload != null ) {
                info = temp.payload;
                return true; // exact match
            } else if( temp.tag == MULTI ) {
                info = default(T);
                return false; // multiple matches
            }
            for( ; temp.tag < MULTI; temp = temp.children[temp.tag] ) ;
            info = temp.payload;
            return true; // one autocompleted match
        }


        /// <summary>
        /// Adds a new object to the trie by name.
        /// </summary>
        /// <param name="name">Full name (used as a key)</param>
        /// <param name="payload">Object associated with the name.</param>
        /// <returns>Returns false if an entry for this name already exists.</returns>
        public bool Add( string name, T payload ) {
            StringNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.children[code] == null ) {
                    temp.children[code] = new StringNode();
                }
                if( temp.tag == EMPTY ) {
                    temp.tag = (byte)code;
                } else {
                    temp.tag = MULTI;
                }
                temp = temp.children[code];
            }
            if( temp.payload != null )
                return false;
            temp.payload = payload;
            Count++;
            return true;
        }


        public bool Remove( string name ) {
            StringNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.children[code] == null )
                    return false;
                temp = temp.children[code];
            }
            temp.payload = default( T );
            return true;
        }


        // Decodes ASCII into internal letter code.
        static int CharCode( char ch ) {
            if( ch >= 'a' && ch <= 'z' )
                return ch - 'a';
            if( ch >= 'A' && ch <= 'Z' )
                return ch - 'A';
            if( ch >= '0' && ch <= '9' )
                return ch - '0' + 26;
            return 36;
        }

        sealed class StringNode {
            public byte tag = EMPTY;
            public StringNode[] children = new StringNode[37];
            public T payload;

            public bool GetAllChildren( List<T> list, int limit ) {
                if( list.Count >= limit ) return false;
                if( payload != null ) {
                    list.Add( payload );
                }
                if( tag < MULTI ) {
                    if( !children[tag].GetAllChildren( list, limit ) ) return false;
                } else if( tag == MULTI ) {
                    for( int i = 0; i < children.Length; i++ ) {
                        if( children[i] != null ) {
                            if( !children[i].GetAllChildren( list, limit ) ) return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
