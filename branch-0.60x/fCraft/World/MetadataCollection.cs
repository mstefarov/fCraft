// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace fCraft {
    /// <summary> A string metadata entry. </summary>
    [DebuggerDisplay( "Count = {Count}" )]
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
    public sealed class MetadataCollection : ICollection<MetadataEntry>, ICollection, ICloneable, INotifiesOnChange {

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


        /// <summary> The total number of entries in this collection. </summary>
        public int Count {
            get {
                lock( syncRoot ) {
                    return store.Sum( group => group.Value.Count );
                }
            }
        }


        /// <summary> Number of groups in this collection. </summary>
        public int GroupCount {
            get { return store.Count; }
        }


        /// <summary> Number of keys within a given group. </summary>
        public int GetKeyCount( string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            return store[group].Count;
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
                RaiseChangedEvent();
            }
        }


        public bool Remove( string group, string key ) {
            Dictionary<string, string> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) return false;
                if( pair.Remove( key ) ) {
                    RaiseChangedEvent();
                    return true;
                } else {
                    return false;
                }
            }
        }


        #region Index / Get / Set

        public string this[string group, string key] {
            get {
                return GetValue( group, key );
            }
            set {
                SetValue( group, key, value );
            }
        }


        public string GetValue( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store[group][key];
            }
        }


        public void SetValue( string group, string key, string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                bool raiseChangedEvent = false;
                if( !store.ContainsKey( group ) ) {
                    store.Add( group, new Dictionary<string, string>() );
                    raiseChangedEvent = true;
                }
                if( !store[group].ContainsKey( key ) || store[group][key] != value ) {
                    raiseChangedEvent = true;
                }
                store[group][key] = value;
                if( raiseChangedEvent ) RaiseChangedEvent();
            }
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


        public bool ContainsGroup( string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            lock( syncRoot ) {
                return store.ContainsKey( group );
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


        public bool TryGetValue( string group, string key, out string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            Dictionary<string, string> pair;
            lock( syncRoot ) {
                if( !store.TryGetValue( group, out pair ) ) {
                    value = null;
                    return false;
                }
                return pair.TryGetValue( key, out value );
            }
        }



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
                bool raiseEvent = (store.Count > 0);
                store.Clear();
                if( raiseEvent ) RaiseChangedEvent();
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

            lock( syncRoot ) {
                if( array.Length < arrayIndex + Count ) {
                    throw new ArgumentException( "array" );
                }

                int i = 0;
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
            // ReSharper disable LoopCanBeConvertedToQuery
            foreach( var group in store ) {
                foreach( var key in group.Value ) {
                    yield return new MetadataEntry {
                        Group = group.Key,
                        Key = key.Key,
                        Value = key.Value
                    };
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
        }

        #endregion


        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
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


        readonly object syncRoot = new object();
        /// <summary> Internal lock object used by this collection to ensure thread safety. </summary>
        public object SyncRoot {
            get { return syncRoot; }
        }

        #endregion



        public object Clone() {
            return new MetadataCollection( this );
        }

        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }
}