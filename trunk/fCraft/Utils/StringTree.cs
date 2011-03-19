// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;

namespace fCraft {
    /// <summary>
    /// Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB.
    /// </summary>
    public sealed class StringTree<T> where T : class {
        readonly StringNode root = new StringNode();
        public int Count { get; private set; }

        const byte Multi = 37,
                   Empty = 38;


        /// <summary> Get payload for an exact name (no autocompletion) </summary>
        /// <param name="name">Full name</param>
        /// <returns>Payload object, if found. Null (or default) if not found.</returns>
        public T Get( string name ) {
            StringNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.Children[code] == null )
                    return default(T);
                temp = temp.Children[code];
            }
            return temp.Payload;
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
                if( temp.Children[code] == null )
                    return results;
                temp = temp.Children[code];
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
                if( temp.Children[code] == null ) {
                    info = default(T);
                    return true; // early detection of no matches
                }
                temp = temp.Children[code];
            }

            if( temp.Payload != null ) {
                info = temp.Payload;
                return true; // exact match
            } else if( temp.Tag == Multi ) {
                info = default(T);
                return false; // multiple matches
            }
            for( ; temp.Tag < Multi; temp = temp.Children[temp.Tag] ) ;
            info = temp.Payload;
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
                if( temp.Children[code] == null ) {
                    temp.Children[code] = new StringNode();
                }
                if( temp.Tag == Empty ) {
                    temp.Tag = (byte)code;
                } else {
                    temp.Tag = Multi;
                }
                temp = temp.Children[code];
            }
            if( temp.Payload != null )
                return false;
            temp.Payload = payload;
            Count++;
            return true;
        }


        public bool Remove( string name ) {
            StringNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.Children[code] == null )
                    return false;
                temp = temp.Children[code];
            }
            temp.Payload = default( T );
            Count--;
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
            public byte Tag = Empty;
            public readonly StringNode[] Children = new StringNode[37];
            public T Payload;

            public bool GetAllChildren( ICollection<T> list, int limit ) {
                if( list.Count >= limit ) return false;
                if( Payload != null ) {
                    list.Add( Payload );
                }
                if( Tag < Multi ) {
                    if( !Children[Tag].GetAllChildren( list, limit ) ) return false;
                } else if( Tag == Multi ) {
                    for( int i = 0; i < Children.Length; i++ ) {
                        if( Children[i] != null ) {
                            if( !Children[i].GetAllChildren( list, limit ) ) return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
