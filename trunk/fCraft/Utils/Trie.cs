// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft {
    /// <summary> Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB. </summary>
    public sealed class Trie<T> where T : class {
        readonly TrieNode root = new TrieNode();
        public int Count { get; private set; }

        const byte Multi = 254,
                   Empty = 255;


        /// <summary> Get payload for an exact name (no autocompletion) </summary>
        /// <param name="name">Full name</param>
        /// <returns>Payload object, if found. Null if not found.</returns>
        public T Get( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length == 0 ) return root.Payload;

            TrieNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.Children == null || temp.Children[code] == null )
                    return null;
                temp = temp.Children[code];
            }
            return temp.Payload;
        }


        /// <summary> Searches for players starting with namePart, up to a specified limit. Autocompletes. </summary>
        /// <param name="namePart">Partial or full player name</param>
        /// <param name="limit">Limit on the number of player names to return</param>
        /// <returns>List of matches (if there are no matches, length is zero)</returns>
        public List<T> GetMultiple( string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            List<T> results = new List<T>();
            TrieNode temp = root;
            for( int i = 0; i < namePart.Length; i++ ) {
                int code = CharCode( namePart[i] );
                if( temp.Children == null || temp.Children[code] == null ) {
                    return results;
                }
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
            if( namePart == null ) throw new ArgumentNullException( "name" );
            if( namePart.Length == 0 ) {
                info = root.Payload;
                return (root.Children == null);
            }

            TrieNode temp = root;
            for( int i = 0; i < namePart.Length; i++ ) {
                int code = CharCode( namePart[i] );
                if( temp.Children == null || temp.Children[code] == null ) {
                    info = null;
                    return true;
                }
                temp = temp.Children[code];
            }

            if( temp.Payload != null ) {
                info = temp.Payload;
                return true; // exact match

            } else if( temp.Tag == Multi ) {
                info = null;
                return false; // multiple matches
            }

            // todo: check for multiple matches in this loop
            while( true ) {
                if( temp.Children == null ) {
                    // found a singular match
                    info = temp.Payload;
                    return true;

                } else if( temp.Tag == Multi ) {
                    // ran into multiple matches
                    info = null;
                    return false;

                } else {
                    // or go deeper down the trie
                    temp = temp.Children[temp.Tag];
                }
            }
        }


        /// <summary> Adds a new object to the trie by name. </summary>
        /// <param name="name"> Full name (used as a key) </param>
        /// <param name="payload"> Object associated with the name. </param>
        /// <returns> Returns false if an entry for this name already exists. </returns>
        public bool Add( string name, T payload ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( payload == null ) throw new ArgumentNullException( "payload" );

            if( name.Length == 0 ) {
                if( root.Payload != null ) return false;
                root.Payload = payload;
                return true;
            }

            TrieNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.Children == null ) {
                    temp.InitChildren();
                }
                if( temp.Children[code] == null ) {
                    temp.Children[code] = new TrieNode();
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
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length == 0 ) {
                if( root.Payload == null ) return false;
                root.Payload = null;
                return true;
            }

            TrieNode temp = root;
            for( int i = 0; i < name.Length; i++ ) {
                int code = CharCode( name[i] );
                if( temp.Children == null ) {
                    temp.Children = new TrieNode[37];
                }
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

        sealed class TrieNode {
            const int ChildCount = 37;

            public TrieNode() { }

            public TrieNode( T payload ) {
                Payload = payload;
            }

            public byte Tag = Empty;
            public TrieNode[] Children;
            public T Payload;

            public void InitChildren() {
                Children = new TrieNode[ChildCount];
            }

            public bool GetAllChildren( ICollection<T> list, int limit ) {
                if( list.Count >= limit ) return false;
                if( Payload != null ) {
                    list.Add( Payload );
                }
                if( Children == null ) return true;

                if( Tag == Multi ) {
                    for( int i = 0; i < Children.Length; i++ ) {
                        if( Children[i] != null ) {
                            if( !Children[i].GetAllChildren( list, limit ) ) return false;
                        }
                    }
                } else if( Tag < ChildCount ) {
                    if( !Children[Tag].GetAllChildren( list, limit ) ) return false;
                }
                return true;
            }
        }
    }
}