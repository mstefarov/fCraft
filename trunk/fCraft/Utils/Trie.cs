// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
    /// <summary> Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB. </summary>
    public sealed class Trie<T> where T : class {
        public const byte LeafNode = 254,
                          MultiNode = 255;
        readonly TrieNode root = new TrieNode();
        public int Count { get; private set; }


        // Find a node that exactly matches the given key
        TrieNode GetNode( string key ) {
            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharCode( key[i] );
                switch( temp.Tag ) {
                    case LeafNode:
                        return null;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            return null;
                        } else {
                            temp = temp.Children[code];
                            break;
                        }

                    default:
                        if( temp.Tag != code ) {
                            return null;
                        } else {
                            temp = temp.Children[0];
                            break;
                        }
                }
            }
            return temp;
        }


        public bool Contains( string key ) {
            return (GetNode( key ) != null);
        }


        /// <summary> Get payload for an exact key (no autocompletion). </summary>
        /// <param name="name"> Full key. </param>
        /// <returns> Payload object, if found. Null if not found. </returns>
        public T Get( string key ) {
            if( key == null ) throw new ArgumentNullException( "name" );
            TrieNode node = GetNode( key );
            if( node != null ) {
                return node.Payload;
            } else {
                return null;
            }
        }


        /// <summary> Searches for payloads with keys that start with keyPart, returning just one or none of the matches. </summary>
        /// <param name="namePart"> Partial or full key. </param>
        /// <param name="info"> Payload object to output (will be set to null if no single match was found). </param>
        /// <returns> True if one or zero matches were found, false if multiple matches were found. </returns>
        public bool Get( string keyPart, out T payload ) {
            if( keyPart == null ) throw new ArgumentNullException( "name" );
            TrieNode node = GetNode( keyPart );

            if( node.Payload != null ) {
                payload = node.Payload;
                return true; // exact match

            } else if( node.Tag == MultiNode ) {
                payload = null;
                return false; // multiple matches
            }

            // todo: check for multiple matches in this loop
            while( true ) {
                switch( node.Tag ) {
                    case LeafNode:
                        // found a singular match
                        payload = node.Payload;
                        return true;

                    case MultiNode:
                        // ran into multiple matches
                        payload = null;
                        return false;

                    default:
                        // go deeper
                        node = node.Children[0];
                        break;
                }
            }
        }


        /// <summary> Finds a list of payloads with keys that start with keyPart, up to a specified limit. Autocompletes. </summary>
        /// <param name="namePart"> Partial or full key. </param>
        /// <param name="limit"> Limit on the number of payloads to find/return. </param>
        /// <returns> List of matches (if there are no matches, length is zero). </returns>
        public List<T> GetList( string keyPart, int limit ) {
            if( keyPart == null ) throw new ArgumentNullException( "keyPart" );
            List<T> results = new List<T>();

            TrieNode startingNode = GetNode( keyPart );
            if( startingNode != null ) {
                startingNode.GetAllChildren( results, limit );
            }

            return results;
        }


        /// <summary> Adds a new object by key. If an entry for this key already exists, it is NOT overwritten. </summary>
        /// <param name="name"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public bool Add( string key, T payload ) {
            return Add( key, payload, false );
        }


        /// <summary> Adds a new object by key. </summary>
        /// <param name="name"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public bool Add( string key, T payload, bool overwriteOnDuplicate ) {
            if( key == null ) throw new ArgumentNullException( "name" );
            if( payload == null ) throw new ArgumentNullException( "payload" );

            if( key.Length == 0 ) {
                if( root.Payload != null ) {
                    if( overwriteOnDuplicate ) root.Payload = payload;
                    return false;
                }
                root.Payload = payload;
                return true;
            }

            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharCode( key[i] );

                switch( temp.Tag ) {
                    case LeafNode:
                        temp.LeafToSingle( (byte)code );
                        temp.Children[0] = new TrieNode();
                        temp = temp.Children[0];
                        break;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            temp.Children[code] = new TrieNode();
                        }
                        temp = temp.Children[code];
                        break;

                    default:
                        if( temp.Tag != code ) {
                            temp.SingleToMulti();
                            temp.Children[code] = new TrieNode();
                            temp = temp.Children[code];
                        } else {
                            temp = temp.Children[0];
                        }
                        break;
                }
            }

            if( temp.Payload != null ) {
                if( overwriteOnDuplicate ) temp.Payload = payload;
                return false;
            } else {
                temp.Payload = payload;
                Count++;
                return true;
            }
        }


        public T this[string key] {
            get {
                return Get( key );
            }
            set {
                Add( key, value, true );
            }
        }


        /// <summary> Removes an entry by key. </summary>
        /// <param name="name"> Key for the entry to remove. </param>
        /// <returns> True if the entry was removed, false if no entry was found for this key. </returns>
        public bool Remove( string key ) {
            if( key == null ) throw new ArgumentNullException( "name" );
            if( key.Length == 0 ) {
                if( root.Payload == null ) return false;
                root.Payload = null;
                return true;
            }

            // find parents
            TrieNode temp = root;
            Stack<TrieNode> parents = new Stack<TrieNode>();
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharCode( key[i] );
                switch( temp.Tag ) {
                    case LeafNode:
                        return false;

                    case MultiNode:
                        if( temp.Children[code] == null ) {
                            return false;
                        } else {
                            parents.Push( temp );
                            temp = temp.Children[code];
                            break;
                        }

                    default:
                        if( temp.Tag != code ) {
                            return false;
                        } else {
                            parents.Push( temp );
                            temp = temp.Children[0];
                            break;
                        }
                }
            }

            // reduce parents
            temp.Payload = null;
            Count--;
            while( parents.Count > 0 ) {
                TrieNode parent = parents.Pop();
                switch( parent.Tag ) {
                    case LeafNode:
                        ThrowStateException();
                        break;

                    case MultiNode:
                        parent.MultiToSingle();
                        return true;

                    default:
                        parent.SingleToLeaf();
                        break;
                }
                if( parent.Payload != null ) {
                    break;
                }
            }
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


        static void ThrowStateException() {
            throw new Exception( "Inconsistent state" );
        }


        sealed class TrieNode {
            const int ChildCount = 37;


            // Tag identifies TrieNode as being either a LeafNode,
            // a MultiNode, or a single-child node.
            public byte Tag = LeafNode;

            // Children. May be null (if LeafNode),
            // TrieNode[ChildCount] (if MultiNode),
            // or TrieNode[1] (if single-child node)
            public TrieNode[] Children;

            // May be null (if MultiNode or single-child node)
            public T Payload;


            public TrieNode() { }


            public TrieNode( T payload ) {
                Payload = payload;
            }


            public void LeafToSingle( byte charCode ) {
                if( Children != null || Tag != LeafNode ) {
                    ThrowStateException();
                }
                Children = new TrieNode[1];
                Tag = charCode;
            }


            public void SingleToLeaf() {
                if( Children == null || Children.Length != 1 || Tag >= ChildCount ) {
                    ThrowStateException();
                }
                if( Children[0].Tag == LeafNode ) {
                    Children = null;
                    Tag = LeafNode;
                }
            }


            public void SingleToMulti() {
                if( Children == null || Children.Length != 1 || Tag >= ChildCount ) {
                    ThrowStateException();
                }
                TrieNode oldNode = Children[0];
                Children = new TrieNode[ChildCount];
                Children[Tag] = oldNode;
                Tag = MultiNode;
            }


            public void MultiToSingle() {
                if( Children == null || Children.Length != ChildCount || Tag != MultiNode ) {
                    ThrowStateException();
                }
                int index = -1;

                // remove empty children
                for( int i = 0; i < Children.Length; i++ ) {
                    if( Children[i] != null &&
                        Children[i].Tag == LeafNode &&
                        Children[i].Payload == null ) {

                        Children[i] = null;

                    } else if( index != -1 ) {
                        index = i;

                    } else {
                        return;
                    }
                }

                if( index == -1 ) {
                    ThrowStateException();
                } else {
                    // if there's just one, convert to single
                    Children = new TrieNode[] { Children[index] };
                    Tag = (byte)index;
                }
            }


            public bool GetAllChildren( ICollection<T> list, int limit ) {
                if( list.Count >= limit ) return false;
                if( Payload != null ) {
                    list.Add( Payload );
                }
                if( Children == null ) return true;

                switch( Tag ) {
                    case MultiNode:
                        for( int i = 0; i < Children.Length; i++ ) {
                            if( Children[i] != null ) {
                                if( !Children[i].GetAllChildren( list, limit ) ) return false;
                            }
                        }
                        return true;

                    case LeafNode:
                        return true;

                    default:
                        return Children[0].GetAllChildren( list, limit );
                }
            }
        }


        /* Self-test
        const int ItemsToAdd = 1000000;
        const int DupeCheckCount = ItemsToAdd / 10000;

        public static void RunSelfTest() {
            Trie<string> test = new Trie<string>();
            Dictionary<string, string> reference = new Dictionary<string, string>();

            Random rand = new Random();
            {
                Console.WriteLine( "Inserting {0} nodes...", ItemsToAdd );
                // test insertion
                for( int i = 0; i < ItemsToAdd; i++ ) {
                    string key = RandString( rand );
                    if( reference.ContainsKey( key ) ) continue;
                    reference.Add( key, key );
                    if( !test.Add( key, key ) ) Console.WriteLine( " {0}: dupe", key );
                }

                // verify that trie contains all elements that are in the reference
                foreach( var pair in reference ) {
                    if( test.Get( pair.Key ) != pair.Value ) {
                        Console.WriteLine( " {0}: got: {1}", pair.Key, test.Get( pair.Key ) ?? "null", pair.Value );
                    }
                    string partialTest;
                    if( !test.Get( pair.Key, out partialTest ) )
                        Console.WriteLine( " {0}: get with completion: failed (multi)" );
                    if( partialTest != pair.Value )
                        Console.WriteLine( " {0}: got with completion: {1}", pair.Key, test.Get( pair.Key ) ?? "null", pair.Value );
                }

                // verify that counts match
                if( test.Count != reference.Count ) {
                    Console.WriteLine( "Count: {0} test vs. {1} reference", test.Count, reference.Count );
                }
                Console.WriteLine( "Insertion test done." );
                Console.WriteLine();
            }


            {
                IEnumerable<string> dupeKeys = reference.Where( pair => pair.Key.Length < 4 ).Select( pair => pair.Key ).Distinct().Take( DupeCheckCount );
                Console.WriteLine( "Testing autocompletion on {0} nodes...", dupeKeys.Count() );

                int k = 0;
                foreach( string dupeKey in dupeKeys ) {
                    IOrderedEnumerable<string> matches = test.GetMultiple( dupeKey, Int32.MaxValue ).OrderBy( s => s );
                    IOrderedEnumerable<string> refMatches = reference.Where( pair => pair.Key.StartsWith( dupeKey ) ).Select( pair => pair.Key ).OrderBy( s => s );
                    if( !matches.SequenceEqual( refMatches ) )
                        Console.WriteLine( "{0}: Autocompletion failed ({1} vs ref {2})", dupeKey, matches.Count(), refMatches.Count() );
                    k++;
                }
                Console.WriteLine( "Autocompletion test done." );
                Console.WriteLine();
            }


            {
                Console.WriteLine( "Removing {0} nodes...", reference.Count/2 );
                int r = 0;
                List<string> stuffToRemove = new List<string>();
                // make a list of items to remove
                foreach( var pair in reference ) {
                    stuffToRemove.Add( pair.Key );
                    r++;
                    if( r > reference.Count / 2 ) break;
                }

                // test removal
                for( int i = 0; i < stuffToRemove.Count; i++ ) {
                    if( !test.Remove( stuffToRemove[i] ) ) {
                        Console.WriteLine( " {0}: missing (#{1})", stuffToRemove[i], i );
                    }
                    reference.Remove( stuffToRemove[i] );
                }

                // verify that all remaining items are intact
                foreach( var pair in reference ) {
                    if( test.Get( pair.Key ) != pair.Value ) {
                        Console.WriteLine( " {0}: got: {1}", pair.Key, test.Get( pair.Key ) ?? "null", pair.Value );
                    }
                    string partialTest;
                    if( !test.Get( pair.Key, out partialTest ) )
                        Console.WriteLine( " {0}: get with completion: failed (multi)" );
                    if( partialTest != pair.Value )
                        Console.WriteLine( " {0}: got with completion: {1}", pair.Key, test.Get( pair.Key ) ?? "null", pair.Value );
                }

                // verify that count still matches
                if( test.Count != reference.Count ) {
                    Console.WriteLine( "Count: {0} test vs. {1} reference", test.Count, reference.Count );
                }

                Console.WriteLine( "Removal test done." );
                Console.WriteLine();
            }


            {
                IEnumerable<string> dupeKeys = reference.Where( pair => pair.Key.Length < 4 ).Select( pair => pair.Key ).Distinct().Take( DupeCheckCount );
                Console.WriteLine( "Testing autocompletion on {0} nodes...", dupeKeys.Count() );

                int k = 0;
                foreach( string dupeKey in dupeKeys ) {
                    IOrderedEnumerable<string> matches = test.GetMultiple( dupeKey, Int32.MaxValue ).OrderBy( s => s );
                    IOrderedEnumerable<string> refMatches = reference.Where( pair => pair.Key.StartsWith( dupeKey ) ).Select( pair => pair.Key ).OrderBy( s => s );
                    if( !matches.SequenceEqual( refMatches ) )
                        Console.WriteLine( "{0}: Autocompletion failed ({1} vs ref {2})", dupeKey, matches.Count(), refMatches.Count() );
                    k++;
                }
                Console.WriteLine( "Autocompletion test done." );
                Console.WriteLine();
            }
        }


        public static string RandString( Random rand ) {
            int len = rand.Next( 1, 8 );
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for( int i = 0; i < len; i++ ) {
                sb.Append( (char)rand.Next( 'a', 'z' + 1 ) );
            }
            return sb.ToString();
        }
        */
    }
}