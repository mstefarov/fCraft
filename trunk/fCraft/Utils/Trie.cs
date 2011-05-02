// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace fCraft {
    /// <summary> Specialized data structure for partial-matching of large sparse sets of words.
    /// Used as a searchable index of players for PlayerDB. </summary>
    [DebuggerDisplay( "Count = {Count}" )]
    public sealed class Trie<T> : IDictionary<string, T>, IEnumerable<KeyValuePair<string, T>>, IDictionary, ICollection, IEnumerable, ICloneable where T : class {
        public const byte LeafNode = 254,
                          MultiNode = 255;
        TrieNode root = new TrieNode();

        int count = 0;
        int version = 0;


        public Trie() {
            keys = new TrieKeyCollection( this );
            values = new TrieValueCollection( this );
        }

        public Trie( IDictionary<string, T> dictionary )
            : this() {
            if( dictionary == null ) throw new ArgumentNullException( "dictionary" );
            foreach( var pair in dictionary ) {
                Add( pair.Key, pair.Value );
            }
        }


        // Find a node that exactly matches the given key
        TrieNode GetNode( string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );

            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );
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


        public bool ContainsValue( T value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            return Values.Contains( value );
        }


        /// <summary> Searches for payloads with keys that start with keyPart, returning just one or none of the matches. </summary>
        /// <param name="namePart"> Partial or full key. </param>
        /// <param name="info"> Payload object to output (will be set to null if no single match was found). </param>
        /// <returns>
        /// If no matches were found, returns true and sets payload to null.
        /// If one match was found, returns true and sets payload to the value.
        /// If more than one match was found, returns false and sets payload to null.
        /// </returns>
        public bool GetOneMatch( string keyPart, out T payload ) {
            if( keyPart == null ) throw new ArgumentNullException( "keyPart" );
            TrieNode node = GetNode( keyPart );

            if( node == null ) {
                payload = null;
                return true; // no matches
            }

            if( node.Payload != null ) {
                payload = node.Payload;
                return true; // exact match

            } else if( node.Tag == MultiNode ) {
                payload = null;
                return false; // multiple matches
            }

            // either partial match, or multiple matches
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


        /// <summary> Adds a new object by key. </summary>
        /// <param name="name"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public bool Add( string key, T payload, bool overwriteOnDuplicate ) {
            if( key == null ) throw new ArgumentNullException( "name" );
            if( payload == null ) throw new ArgumentNullException( "payload" );

            if( key.Length == 0 ) {
                if( root.Payload != null ) {
                    if( overwriteOnDuplicate ) {
                        root.Payload = payload;
                        version++;
                    }
                    return false;
                }
                count++;
                root.Payload = payload;
                return true;
            }

            TrieNode temp = root;
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );

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
                if( overwriteOnDuplicate ) {
                    temp.Payload = payload;
                    version++;
                }
                return false;
            } else {
                temp.Payload = payload;
                version++;
                count++;
                return true;
            }
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


        #region Key Encoding / Decoding

        // Decodes ASCII into internal letter code.
        static int CharToCode( char ch ) {
            if( ch >= 'a' && ch <= 'z' )
                return ch - 'a';
            else if( ch >= 'A' && ch <= 'Z' )
                return ch - 'A';
            else if( ch >= '0' && ch <= '9' )
                return ch - '0' + 26;
            else
                return 36;
        }


        static char CodeToChar( int code ) {
            if( code < 26 )
                return (char)(code + 'a');
            if( code >= 26 && code < 36 )
                return (char)(code + '0');
            else
                return '_';
        }


        static char CanonicizeChar( char ch ) {
            if( ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9' || ch == '_' )
                return ch;
            else if( ch >= 'A' && ch <= 'Z' )
                return (char)(ch - ('A' - 'a'));
            else
                return '_';
        }


        static string CanonicizeKey( string key ) {
            StringBuilder sb = new StringBuilder( key );
            for( int i = 0; i < sb.Length; i++ ) {
                sb[i] = CanonicizeChar( sb[i] );
            }
            return sb.ToString();
        }


        static void ThrowStateException() {
            throw new Exception( "Inconsistent state" );
        }

        #endregion


        #region Subset Enumerators

        public IEnumerable<T> ValuesStartingWith( string prefix ) {
            return values.StartingWith( prefix );
        }


        public IEnumerable<string> KeysStartingWith( string prefix ) {
            return keys.StartingWith( prefix );
        }


        public IEnumerable<KeyValuePair<string, T>> StartingWith( string prefix ) {
            return new TrieSubset( this, prefix );
        }


        public class TrieSubset : IEnumerable<KeyValuePair<string, T>> {
            Trie<T> trie;
            string prefix;

            public TrieSubset( Trie<T> trie, string prefix ) {
                this.trie = trie;
                this.prefix = prefix;
            }


            public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
                TrieNode node = trie.GetNode( prefix );
                return new TrieEnumerator( node, trie, CanonicizeKey( prefix ) );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator() as IEnumerator;
            }
        }

        #endregion


        #region Enumerator Base

        class EnumeratorBase {

            // Starting node ("root" of the trie/subtrie)
            protected TrieNode startingNode;

            // Current node (presumably with payload)
            protected TrieNode currentNode;

            // Index of the child in the current node
            protected int currentIndex;

            // Version of collection when we started iterating. Used to keep track of collection changes.
            protected int startingVersion;

            // Trie from which our nodes originate (used in conjunction with startingVersion to check for modification).
            protected Trie<T> baseTrie;

            protected StringBuilder currentKeyName;

            protected string basePrefix;


            // A couple stacks to keep track of our position in the trie
            protected Stack<TrieNode> parents = new Stack<TrieNode>();
            protected Stack<int> parentIndices = new Stack<int>();

            public EnumeratorBase( TrieNode node, Trie<T> trie, string prefix ) {
                if( node == null ) throw new ArgumentNullException( "node" );
                if( trie == null ) throw new ArgumentNullException( "trie" );
                if( prefix == null ) throw new ArgumentNullException( "prefix" );
                startingNode = node;
                baseTrie = trie;
                basePrefix = prefix;
                currentKeyName = new StringBuilder( basePrefix );
                startingVersion = baseTrie.version;
            }


            protected bool MoveNextInternal() {
                if( startingNode == null ) return false;
                if( baseTrie.version != startingVersion ) {
                    ThrowCollectionModifiedException();
                }
                if( currentNode == null ) {
                    currentNode = startingNode;
                    if( currentNode.Payload != null ) {
                        return true;
                    }
                }
                return FindNextPayload();
            }


            protected void ResetInternal() {
                parents.Clear();
                parentIndices.Clear();
                currentNode = null;
                startingVersion = baseTrie.version;
                currentKeyName = new StringBuilder( basePrefix );
            }


            protected bool FindNextPayload() {
            continueLoop:
                switch( currentNode.Tag ) {
                    case MultiNode:
                        while( currentIndex < currentNode.Children.Length ) {
                            if( currentNode.Children[currentIndex] != null ) {
                                MoveDown( currentNode.Children[currentIndex], currentIndex );
                                if( currentNode.Payload != null ) {
                                    return true;
                                } else {
                                    goto continueLoop;
                                }
                            } else {
                                currentIndex++;
                            }
                        }
                        if( !MoveUp() ) return false;
                        goto continueLoop;

                    case LeafNode:
                        if( !MoveUp() ) return false;
                        goto continueLoop;

                    default:
                        if( currentIndex == 0 ) {
                            MoveDown( currentNode.Children[0], currentNode.Tag );
                            if( currentNode.Payload != null ) {
                                return true;
                            }
                        } else {
                            if( !MoveUp() ) return false;
                        }
                        goto continueLoop;

                }
            }


            // Pops the nearest parent from the stack (moving up the trie)
            protected bool MoveUp() {
                if( parents.Count == 0 ) {
                    return false;
                } else {
                    currentKeyName.Remove( currentKeyName.Length - 1, 1 );
                    currentNode = parents.Pop();
                    currentIndex = parentIndices.Pop();
                    return true;
                }
            }


            // Pushes current node onto the stack, and makes the given node current.
            protected void MoveDown( TrieNode node, int index ) {
                currentKeyName.Append( CodeToChar( index ) );
                parents.Push( currentNode );
                parentIndices.Push( currentIndex + 1 );
                currentNode = node;
                currentIndex = 0;
            }


            protected void ThrowCollectionModifiedException() {
                throw new InvalidOperationException( "Trie was modified since enumeration started." );
            }
        }

        #endregion


        #region IDictionary<string,T> Members

        TrieKeyCollection keys;
        public ICollection<string> Keys {
            get { return keys; }
        }


        TrieValueCollection values;
        public ICollection<T> Values {
            get { return values; }
        }


        /// <summary> Adds a new object by key. If an entry for this key already exists, it is NOT overwritten. </summary>
        /// <param name="name"> Full key. </param>
        /// <param name="payload"> Object associated with the key. </param>
        /// <returns> True if object was added, false if an entry for this key already exists. </returns>
        public void Add( string key, T payload ) {
            if( !Add( key, payload, false ) ) {
                throw new ArgumentException( "Duplicate key.", "key" );
            }
        }


        public bool TryGetValue( string key, out T result ) {
            TrieNode node = GetNode( key );
            result = node.Payload;
            return (node != null);
        }


        public T this[string key] {
            get {
                return Get( key );
            }
            set {
                Add( key, value, true );
            }
        }


        public bool ContainsKey( string key ) {
            TrieNode node = GetNode( key );
            return (node != null && node.Payload != null);
        }


        /// <summary> Removes an entry by key. </summary>
        /// <param name="name"> Key for the entry to remove. </param>
        /// <returns> True if the entry was removed, false if no entry was found for this key. </returns>
        public bool Remove( string key ) {
            if( key == null ) throw new ArgumentNullException( "name" );
            if( key.Length == 0 ) {
                if( root.Payload == null ) return false;
                root.Payload = null;
                count--;
                version++;
                return true;
            }

            // find parents
            TrieNode temp = root;
            Stack<TrieNode> parents = new Stack<TrieNode>();
            for( int i = 0; i < key.Length; i++ ) {
                int code = CharToCode( key[i] );
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
            count--;
            version++;
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


        public void Clear() {
            root = new TrieNode();
            count = 0;
            version = 0;
        }

        #endregion


        #region IDictionary Members

        public bool IsFixedSize { get { return false; } }


        ICollection IDictionary.Values {
            get {
                return Values as ICollection;
            }
        }


        ICollection IDictionary.Keys {
            get {
                return Keys as ICollection;
            }
        }


        object IDictionary.this[object key] {
            get {
                if( key == null ) {
                    throw new ArgumentNullException( "key" );
                }
                string castKey = key as string;
                if( castKey == null ) {
                    throw new ArgumentException( "Key must be of type String.", "key" );
                }
                return this[castKey] as object;
            }
            set {
                if( key == null ) {
                    throw new ArgumentNullException( "key" );
                }
                string castKey = key as string;
                if( castKey == null ) {
                    throw new ArgumentException( "Key must be of type String.", "key" );
                }
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                T castValue = value as T;
                if( castValue == null ) {
                    throw new ArgumentException( "Value must be of type " + typeof( T ).Name, "value" );
                }
                this[castKey] = castValue;
            }
        }


        void IDictionary.Remove( object key ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }
            Remove( castKey );
        }


        void IDictionary.Add( object key, object value ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }
            if( value == null ) {
                throw new ArgumentNullException( "value" );
            }
            T castValue = value as T;
            if( castValue == null ) {
                throw new ArgumentException( "Value must be of type " + typeof( T ).Name, "value" );
            }
            Add( castKey, castValue );
        }


        bool IDictionary.Contains( object key ) {
            if( key == null ) {
                throw new ArgumentNullException( "key" );
            }
            string castKey = key as string;
            if( castKey == null ) {
                throw new ArgumentException( "Key must be of type String.", "key" );
            }

            return ContainsKey( castKey );
        }


        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new TrieDictionaryEnumerator( root, this, "" );
        }


        class TrieDictionaryEnumerator : EnumeratorBase, IDictionaryEnumerator {

            public TrieDictionaryEnumerator( TrieNode node, Trie<T> trie, string prefix )
                : base( node, trie, prefix ) {
            }


            public object Key {
                get {
                    if( currentNode == null || currentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return currentKeyName.ToString() as object;
                }
            }


            public object Value {
                get {
                    if( currentNode == null || currentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return currentNode.Payload as object;
                }
            }


            public DictionaryEntry Entry {
                get {
                    if( currentNode == null || currentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return new DictionaryEntry( currentKeyName.ToString(), currentNode.Payload );
                }
            }


            object IEnumerator.Current {
                get {
                    return Entry as object;
                }
            }


            public bool MoveNext() {
                return MoveNextInternal();
            }


            public void Reset() {
                ResetInternal();
            }
        }

        #endregion


        #region ValueCollection

        [DebuggerDisplay( "Count = {Count}" )]
        public class TrieValueCollection : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable {
            public Trie<T> trie;


            public TrieValueCollection( Trie<T> trie ) {
                if( trie == null ) throw new ArgumentNullException( "trie" );
                this.trie = trie;
            }


            public int Count { get { return trie.count; } }


            public bool IsReadOnly { get { return true; } }


            public bool IsSynchronized { get { return false; } }


            public object SyncRoot { get { return trie.syncRoot; } }


            public void CopyTo( Array array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                T[] castArray = array as T[];
                if( castArray == null ) {
                    throw new ArgumentException( "Array must be of type " + typeof( T ).Name + "[]" );
                }

                int i = index;
                foreach( T element in this ) {
                    castArray[i] = element;
                    i++;
                }
            }


            public void CopyTo( T[] array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                int i = index;
                foreach( T element in this ) {
                    array[i] = element;
                    i++;
                }
            }


            public bool Contains( T value ) {
                return (this as IEnumerable<T>).Contains( value );
            }


            #region Unsupported members (Add/Remove/Clear)

            const string ReadOnlyMessage = "Trie value collection is read-only";


            public void Add( T value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public bool Remove( T value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public void Clear() {
                throw new NotSupportedException( ReadOnlyMessage );
            }

            #endregion


            public IEnumerable<T> StartingWith( string prefix ) {
                return new TrieValueSubset( trie, prefix );
            }


            public class TrieValueSubset : IEnumerable<T> {
                Trie<T> trie;
                string prefix;

                public TrieValueSubset( Trie<T> trie, string prefix ) {
                    if( trie == null ) throw new ArgumentNullException( "trie" );
                    if( prefix == null ) throw new ArgumentNullException( "prefix" );
                    this.trie = trie;
                    this.prefix = prefix;
                }


                public IEnumerator<T> GetEnumerator() {
                    TrieNode node = trie.GetNode( prefix );
                    return new TrieValueEnumerator( node, trie, CanonicizeKey( prefix ) );
                }


                IEnumerator IEnumerable.GetEnumerator() {
                    return GetEnumerator() as IEnumerator;
                }
            }


            #region TrieValueEnumerator

            public IEnumerator<T> GetEnumerator() {
                return new TrieValueEnumerator( trie.root, trie, "" );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return new TrieValueEnumerator( trie.root, trie, "" );
            }


            class TrieValueEnumerator : EnumeratorBase, IEnumerator<T> {

                public TrieValueEnumerator( TrieNode node, Trie<T> trie, string prefix )
                    : base( node, trie, prefix ) {
                }


                public T Current {
                    get {
                        if( currentNode == null || currentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return currentNode.Payload;
                    }
                }


                object IEnumerator.Current {
                    get {
                        if( currentNode == null || currentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return currentNode.Payload as object;
                    }
                }


                public bool MoveNext() {
                    return MoveNextInternal();
                }


                public void Reset() {
                    ResetInternal();
                }


                void IDisposable.Dispose() { }
            }

            #endregion
        }

        #endregion


        #region KeyCollection

        [DebuggerDisplay( "Count = {Count}" )]
        public class TrieKeyCollection : ICollection<string>, IEnumerable<string>, ICollection, IEnumerable {
            public Trie<T> trie;


            public TrieKeyCollection( Trie<T> trie ) {
                if( trie == null ) throw new ArgumentNullException( "trie" );
                this.trie = trie;
            }


            public int Count { get { return trie.count; } }


            public bool IsReadOnly { get { return true; } }


            public bool IsSynchronized { get { return false; } }


            public object SyncRoot { get { return trie.syncRoot; } }


            public void CopyTo( Array array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                string[] castArray = array as string[];
                if( castArray == null ) {
                    throw new ArgumentException( "Array must be of type String[]" );
                }

                int i = index;
                foreach( string element in this ) {
                    castArray[i] = element;
                    i++;
                }
            }


            public void CopyTo( string[] array, int index ) {
                if( array == null ) throw new ArgumentNullException( "array" );
                if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );

                int i = index;
                foreach( string element in this ) {
                    array[i] = element;
                    i++;
                }
            }


            public bool Contains( string value ) {
                return trie.ContainsKey( value );
            }


            #region Unsupported members (Add/Remove/Clear)

            const string ReadOnlyMessage = "Trie value collection is read-only";


            public void Add( string value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public bool Remove( string value ) {
                throw new NotSupportedException( ReadOnlyMessage );
            }


            public void Clear() {
                throw new NotSupportedException( ReadOnlyMessage );
            }

            #endregion


            public IEnumerable<string> StartingWith( string prefix ) {
                return new TrieKeySubset( trie, prefix );
            }


            public class TrieKeySubset : IEnumerable<string> {
                Trie<T> trie;
                string prefix;

                public TrieKeySubset( Trie<T> trie, string prefix ) {
                    if( trie == null ) throw new ArgumentNullException( "trie" );
                    if( prefix == null ) throw new ArgumentNullException( "prefix" );
                    this.trie = trie;
                    this.prefix = prefix;
                }


                public IEnumerator<string> GetEnumerator() {
                    TrieNode node = trie.GetNode( prefix );
                    return new TrieKeyEnumerator( node, trie, CanonicizeKey( prefix ) );
                }


                IEnumerator IEnumerable.GetEnumerator() {
                    return GetEnumerator() as IEnumerator;
                }
            }


            #region TrieKeyEnumerator

            public IEnumerator<string> GetEnumerator() {
                return new TrieKeyEnumerator( trie.root, trie, "" );
            }


            IEnumerator IEnumerable.GetEnumerator() {
                return new TrieKeyEnumerator( trie.root, trie, "" );
            }


            class TrieKeyEnumerator : EnumeratorBase, IEnumerator<string> {

                public TrieKeyEnumerator( TrieNode node, Trie<T> trie, string prefix )
                    : base( node, trie, prefix ) {
                }


                public string Current {
                    get {
                        if( currentNode == null || currentNode.Payload == null ) {
                            throw new InvalidOperationException();
                        }
                        return currentKeyName.ToString();
                    }
                }


                object IEnumerator.Current {
                    get {
                        return Current as object;
                    }
                }


                public bool MoveNext() {
                    return MoveNextInternal();
                }


                public void Reset() {
                    ResetInternal();
                }


                void IDisposable.Dispose() { }
            }

            #endregion
        }

        #endregion


        #region IEnumerable<KeyValuePair<string,T>> Members

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
            return new TrieEnumerator( root, this, "" );
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return new TrieEnumerator( root, this, "" );
        }


        class TrieEnumerator : EnumeratorBase, IEnumerator<KeyValuePair<string, T>> {

            public TrieEnumerator( TrieNode node, Trie<T> trie, string prefix )
                : base( node, trie, prefix ) {
            }


            public KeyValuePair<string, T> Current {
                get {
                    if( currentNode == null || currentNode.Payload == null ) {
                        throw new InvalidOperationException();
                    }
                    return new KeyValuePair<string, T>( currentKeyName.ToString(), currentNode.Payload );
                }
            }


            object IEnumerator.Current {
                get {
                    return Current as object;
                }
            }


            public bool MoveNext() {
                return MoveNextInternal();
            }


            public void Reset() {
                ResetInternal();
            }


            void IDisposable.Dispose() { }
        }

        #endregion


        #region ICollection<KeyValuePair<string,T>> Members

        public int Count { get { return count; } }


        public bool IsReadOnly { get { return false; } }


        public void Add( KeyValuePair<string, T> pair ) {
            Add( pair.Key, pair.Value );
        }


        public bool Contains( KeyValuePair<string, T> pair ) {
            TrieNode node = GetNode( pair.Key );
            if( node == null ) return false;
            if( node.Payload == null ) return false;
            return node.Payload.Equals( pair.Value );
        }


        public bool Remove( KeyValuePair<string, T> pair ) {
            if( Contains( pair ) ) {
                return Remove( pair.Key );
            } else {
                return false;
            }
        }


        public void CopyTo( KeyValuePair<string, T>[] pairArray, int index ) {
            if( pairArray == null ) throw new ArgumentNullException( "pairArray" );
            if( index < 0 || index > pairArray.Length ) throw new ArgumentOutOfRangeException( "index" );

            int i = index;
            foreach( var pair in this ) {
                pairArray[i] = pair;
                i++;
            }
        }

        #endregion


        #region ICollection Members

        public bool IsSynchronized { get { return false; } }


        object syncRoot = new object();
        public object SyncRoot { get { return syncRoot; } }


        public void CopyTo( Array pairArray, int index ) {
            if( pairArray == null ) throw new ArgumentNullException( "pairArray" );
            if( index < 0 || index > pairArray.Length ) throw new ArgumentOutOfRangeException( "index" );

            var castPairArray = pairArray as KeyValuePair<string, T>[];
            if( castPairArray == null ) {
                throw new ArgumentException( "Array must be of type KeyValuePair<string," + typeof( T ).Name + ">[]" );
            }

            int i = index;
            foreach( var pair in this ) {
                castPairArray[i] = pair;
                i++;
            }
        }


        #endregion


        #region ICloneable Members

        public object Clone() {
            return new Trie<T>( this );
        }

        #endregion


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


            public bool GetAllChildren( IList<T> list, int limit ) {
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


        #region Self-test
        /*
        // Self-test
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
                    if( !test.Add( key, key, true ) ) Console.WriteLine( " {0}: dupe", key );
                }

                // verify that trie contains all elements that are in the reference
                foreach( var pair in reference ) {
                    if( test.Get( pair.Key ) != pair.Value ) {
                        Console.WriteLine( " {0}: got: {1}", pair.Key, test.Get( pair.Key ) ?? "null", pair.Value );
                    }
                    string partialTest;
                    if( !test.TryGetValue( pair.Key, out partialTest ) )
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
                    IOrderedEnumerable<string> matches = test.ValuesStartingWith( dupeKey ).OrderBy( s => s );
                    IOrderedEnumerable<string> refMatches = reference.Where( pair => pair.Key.StartsWith( dupeKey ) ).Select( pair => pair.Key ).OrderBy( s => s );
                    if( !matches.SequenceEqual( refMatches ) )
                        Console.WriteLine( "{0}: Autocompletion failed ({1} vs ref {2})", dupeKey, matches.Count(), refMatches.Count() );
                    k++;
                }
                Console.WriteLine( "Autocompletion test done." );
                Console.WriteLine();
            }


            {
                Console.WriteLine( "Removing {0} nodes...", reference.Count / 2 );
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
                    if( !test.TryGetValue( pair.Key, out partialTest ) )
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
                    IOrderedEnumerable<string> matches = test.ValuesStartingWith( dupeKey ).OrderBy( s => s );
                    IOrderedEnumerable<string> refMatches = reference.Where( pair => pair.Key.StartsWith( dupeKey ) ).Select( pair => pair.Key ).OrderBy( s => s );
                    if( !matches.SequenceEqual( refMatches ) )
                        Console.WriteLine( "{0}: Autocompletion failed ({1} vs ref {2})", dupeKey, matches.Count(), refMatches.Count() );
                    k++;
                }
                Console.WriteLine( "Autocompletion test done." );
                Console.WriteLine();
            }


            {
                string[] dupeKeys = reference.Where( pair => pair.Key.Length < 4 ).Select( pair => pair.Key ).Distinct().Take(100).ToArray();
                Console.WriteLine( "Benchmarking {0} iterators...", dupeKeys.Count() );

                string[] randomKeys = reference.OrderBy( k => rand.Next() ).Select(pair=>pair.Key).ToArray();

                
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    int total = 0;
                    string temp;
                    foreach( string randKey in randomKeys ) {
                        temp = reference[randKey];
                        total++;
                    }
                    sw.Stop();
                    Console.WriteLine( "Dictionary.this[]: {0} nodes in {1} ms", total, sw.ElapsedMilliseconds );
                }

                {
                    Stopwatch sw = Stopwatch.StartNew();
                    int total = 0;
                    string temp;
                    foreach( string randKey in randomKeys ) {
                        temp = test.Get( randKey );
                        total++;
                    }
                    sw.Stop();
                    Console.WriteLine( "Trie.Get: {0} nodes in {1} ms", total, sw.ElapsedMilliseconds );
                }
                
                
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    int total = 0;
                    foreach( string dupeKey in dupeKeys ) {
                        total += reference.Where( pair => CanonicizeKey( pair.Key ).StartsWith( dupeKey ) ).Select( pair => pair.Value ).ToList().Count;
                        //Console.WriteLine( dupeKey );
                    }
                    sw.Stop();
                    Console.WriteLine( "Dictionary: {0} nodes in {1} ms", total, sw.ElapsedMilliseconds );
                }
                
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    int total = 0;
                    foreach( string dupeKey in dupeKeys ) {
                        total += test.ValuesStartingWith( dupeKey ).ToList().Count;
                    }
                    sw.Stop();
                    Console.WriteLine( "ValuesStartingWith: {0} nodes in {1} ms", total, sw.ElapsedMilliseconds );
                }

                {
                    Stopwatch sw = Stopwatch.StartNew();
                    int total = 0;
                    foreach( string dupeKey in dupeKeys ) {
                        total += test.GetList( dupeKey, Int32.MaxValue ).Count;
                    }
                    sw.Stop();
                    Console.WriteLine( "GetList: {0} nodes in {1} ms", total, sw.ElapsedMilliseconds );
                }
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
        #endregion
    }
}