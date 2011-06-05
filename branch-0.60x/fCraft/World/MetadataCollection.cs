// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    /// <summary> A string metadata entry. </summary>
    public struct MetadataEntry {
        string group;
        public string Group {
            get { return group; }
            set {
                if( value == null ) throw new ArgumentNullException();
                group = value;
            }
        }

        string key;
        public string Key {
            get { return key; }
            set {
                if( value == null ) throw new ArgumentNullException();
                key = value;
            }
        }

        string value;
        public string Value {
            get { return value; }
            set {
                if( value == this.value ) throw new ArgumentNullException();
                this.value = value;
            }
        }
    }


    /// <summary> A collection of string metadata entries. </summary>
    public class MetadataCollection : ICollection<MetadataEntry>, ICollection, ICloneable {
        public const string EmptyGroup = "";

        readonly Dictionary<string, Dictionary<string, string>> store = new Dictionary<string, Dictionary<string, string>>();

        public MetadataCollection() { }

        public MetadataCollection( MetadataCollection other )
            : this() {
            lock( other.syncRoot ) {
                foreach( var group in store ) {
                    foreach( var key in group.Value ) {
                        Add( group.Key, key.Key, key.Value );
                    }
                }
            }
        }


        public int Count {
            get { return store.Count; }
        }


        public void Add( string key, string value ) {
            Add( EmptyGroup, key, value );
        }


        public void Add( string group, string key, string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                if( !store.ContainsKey( group ) ) {
                    store.Add( group, new Dictionary<string, string>() );
                }
                store[group].Add( key, value );
            }
        }


        public bool Remove( string key ) {
            return Remove( EmptyGroup, key );
        }


        public bool Remove( string group, string key ) {
            Dictionary<string, string> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) return false;
                return pair.Remove( key );
            }
        }


        #region Index / Get / Set

        public string this[string group,string key] {
            get {
                return GetValue( group, key );
            }
            set {
                SetValue( group, key, value );
            }
        }


        public string this[string key] {
            get {
                return GetValue( EmptyGroup, key );
            }
            set {
                SetValue( EmptyGroup, key, value );
            }
        }


        public string GetValue( string key ) {
            return GetValue( EmptyGroup, key );
        }


        public string GetValue( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store[group][key];
            }
        }


        public void SetValue( string key, string value ) {
            SetValue( EmptyGroup, key, value );
        }
        

        public void SetValue( string group, string key, string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                if( !store.ContainsKey( group ) ) {
                    store.Add( group, new Dictionary<string, string>() );
                }
                store[group][key] = value;
            }
        }


        public MetadataEntry Get( string key ) {
            return Get( EmptyGroup, key );
        }


        public MetadataEntry Get( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return new MetadataEntry {
                    Group = group,
                    Key = key,
                    Value = store[group][key]
                };
            }
        }


        public void Set( MetadataEntry entry ) {
            SetValue( entry.Group, entry.Key, entry.Value );
        }

        #endregion


        #region Contains Group/Key

        public bool ContainsGroup( string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            lock( syncRoot ) {
                return store.ContainsKey( group );
            }
        }


        public bool ContainsKey( string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store[EmptyGroup].ContainsKey( key );
            }
        }


        public bool ContainsKey( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store.ContainsKey( group ) &&
                       store[group].ContainsKey( key );
            }
        }

        #endregion


        #region TryGetValue

        public bool TryGetValue( string key, out string value ) {
            return TryGetValue( EmptyGroup, key, out value );
        }


        public bool TryGetValue( string group, string key, out string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            Dictionary<string,string> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) {
                    value = null;
                    return false;
                }
                return pair.TryGetValue( key, out value );
            }
        }

        #endregion


        /// <summary> Enumerates a group of keys. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        public IEnumerator<MetadataEntry> GetGroup( string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            Dictionary<string, string> groupDic;
            if( store.TryGetValue( group, out groupDic ) ) {
                foreach( var key in groupDic ) {
                    yield return new MetadataEntry {
                        Group = group,
                        Key = key.Key,
                        Value = key.Value
                    };
                }
            }
        }


        #region ICollection<MetadataEntry> Members

        public void Add( MetadataEntry item ) {
            Add( item.Group, item.Key, item.Value );
        }


        public void Clear() {
            lock( syncRoot ) {
                store.Clear();
            }
        }


        public bool Contains( MetadataEntry item ) {
            return ContainsKey( item.Group, item.Key );
        }


        public void CopyTo( MetadataEntry[] array, int arrayIndex ) {
            if( array == null ) throw new ArgumentNullException( "array" );

            if( arrayIndex < 0 || arrayIndex >= array.Length ) {
                throw new ArgumentException( "arrayIndex" );
            }

            int total = 0;
            foreach( var group in store.Values ) {
                total += group.Count;
            }

            if( array.Length < arrayIndex + total ) {
                throw new ArgumentException( "array" );
            }

            int i = 0;
            lock( syncRoot ) {
                foreach( var group in store ) {
                    foreach( var pair in group.Value ) {
                        array[i] = new MetadataEntry {
                            Group = group.Key,
                            Key = pair.Key,
                            Value = pair.Value
                        };
                        i++;
                    }
                }
            }
        }


        bool ICollection<MetadataEntry>.IsReadOnly {
            get { return false; }
        }


        public bool Remove( MetadataEntry item ) {
            return Remove( item.Group, item.Key );
        }

        #endregion


        #region IEnumerable<MetadataEntry> Members

        /// <summary> Enumerates all keys in this collection. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        public IEnumerator<MetadataEntry> GetEnumerator() {
            foreach( var group in store ) {
                foreach( var key in group.Value ) {
                    yield return new MetadataEntry {
                        Group = group.Key,
                        Key = key.Key,
                        Value = key.Value
                    };
                }
            }
        }

        #endregion


        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion


        #region ICloneable Members

        public object Clone() {
            return new MetadataCollection( this );
        }

        #endregion


        #region ICollection Members


        public void CopyTo( Array array, int index ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            MetadataEntry[] castArray = array as MetadataEntry[];
            if( castArray == null ) {
                throw new ArgumentException( "Array must be of type MetadataEntry[]", "array" );
            }
            CopyTo( castArray, index );
        }


        public bool IsSynchronized {
            get { return true; }
        }


        object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }

        #endregion
    }
}
