// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Collections.Generic;

namespace fCraft {
    /// <summary>
    /// Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB.
    /// </summary>
    sealed class StringTree {
        StringNode root;
        public int Count { private set; get; }

        public const byte MULTI = 37, EMPTY=38;


        public StringTree() {
            root = new StringNode();
        }


        /// <summary>
        /// Get PlayerInfo for an exact name (no autocompletion)
        /// </summary>
        /// <param name="name">Full player name</param>
        /// <returns>PlayerInfo object, if found. Null if not found.</returns>
        public PlayerInfo Get( string name ) {
            StringNode temp = root;
            int code;
            for( int i = 0; i < name.Length; i++ ) {
                code = CharCode( name[i] );
                if( temp.children[code] == null )
                    return null;
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
        public List<PlayerInfo> GetMultiple( string namePart, int limit ) {
            List<PlayerInfo> results = new List<PlayerInfo>();
            StringNode temp = root;
            int code;
            for( int i = 0; i < namePart.Length; i++ ) {
                code = CharCode( namePart[i] );
                if( temp.children[code] == null )
                    return results;
                temp = temp.children[code];
            }
            GetAllChildren( temp, results, limit );
            return results;
        }

        bool GetAllChildren( StringNode node, List<PlayerInfo> list, int limit ) {
            if( list.Count >= limit ) return false;
            if(node.payload!=null){
                list.Add(node.payload);
            }
            if( node.tag < MULTI ) {
                if( !GetAllChildren( node.children[node.tag], list, limit ) ) return false;
            } else if( node.tag == MULTI ) {
                for( int i = 0; i < node.children.Length; i++ ) {
                    if( node.children[i] != null ) {
                        if( !GetAllChildren( node.children[i], list, limit ) ) return false;
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Searches for player names starting with namePart, returning just one or none of the matches.
        /// </summary>
        /// <param name="namePart">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public bool Get( string namePart, out PlayerInfo info ) {
            StringNode temp = root;
            int code;
            for( int i = 0; i < namePart.Length; i++ ) {
                code = CharCode( namePart[i] );
                if( temp.children[code] == null ) {
                    info = null;
                    return true; // early detection of no matches
                }
                temp = temp.children[code];
            }

            if( temp.payload != null ) {
                info = temp.payload;
                return true; // exact match
            } else if( temp.tag == MULTI ) {
                info = null;
                return false; // multiple matches
            }
            for( ; temp.tag < MULTI; temp = temp.children[temp.tag] ) ;
            info = temp.payload;
            return true; // one autocompleted match
        }


        /// <summary>
        /// Adds a new player name to the trie.
        /// </summary>
        /// <param name="name">Full name (used as a key)</param>
        /// <param name="payload">PlayerInfo associated with the name</param>
        /// <returns>Returns false if an entry for this player already exists.</returns>
        public bool Add( string name, PlayerInfo payload ) {
            StringNode temp = root;
            int code;
            for( int i = 0; i < name.Length; i++ ) {
                code = CharCode( name[i] );
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
            public PlayerInfo payload;
        }
    }
}
