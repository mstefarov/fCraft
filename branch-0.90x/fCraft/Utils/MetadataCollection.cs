// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A single entry in a MetadataCollection, identified by group and key names. Immutable. </summary>
    /// <typeparam name="TValue"> Value type. Must be a reference type. </typeparam>
    public struct MetadataEntry<TValue> where TValue : class {
        /// <summary> Creates a new MetadataEntry struct with the specified group, key, and value. </summary>
        /// <exception cref="ArgumentNullException"> group, key, or value is null </exception>
        public MetadataEntry( [NotNull] string @group, [NotNull] string key, [NotNull] TValue @value ) {
            if( @group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( @value == null ) throw new ArgumentNullException( "value" );
            Group = group;
            Key = key;
            Value = value;
        }

        public readonly string Group;
        public readonly string Key;
        public readonly TValue Value;
    }


    /// <summary> A collection of metadata entries, addressable by pairs of string group/key names.
    /// Group names, key names, and values may not be null.
    /// This collection is synchronized for cross-thread access. </summary>
    /// <typeparam name="TValue"> Value type. Must be a reference type. </typeparam>
    [DebuggerDisplay( "GroupCount = {GroupCount}, Count = {Count}" )]
    public sealed class MetadataCollection<TValue>
        : ICollection<MetadataEntry<TValue>>, ICollection, ICloneable, INotifiesOnChange
        where TValue : class {
        readonly Dictionary<string, Dictionary<string, TValue>> store =
            new Dictionary<string, Dictionary<string, TValue>>();

        /// <summary> Creates an empty MetadataCollection. </summary>
        public MetadataCollection() {}


        /// <summary> Creates a copy of the given MetadataCollection. Copies all entries within. </summary>
        public MetadataCollection( [NotNull] MetadataCollection<TValue> other )
            : this() {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( other.syncRoot ) {
                foreach( var group in store ) {
                    store.Add( group.Key, new Dictionary<string, TValue>( group.Value ) );
                }
            }
        }


        /// <summary> Adds a new entry to the collection.
        /// Throws ArgumentException if an entry with the same group/key already exists. </summary>
        /// <param name="groupName"> Group name. Cannot be null. </param>
        /// <param name="key"> Key name. Cannot be null. </param>
        /// <param name="value"> Value. Cannot be null. </param>
        public void Add( [NotNull] string groupName, [NotNull] string key, [NotNull] TValue value ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                Dictionary<string, TValue> group;
                if( !store.TryGetValue( groupName, out group ) ) {
                    group = new Dictionary<string, TValue>();
                    store.Add( groupName, group );
                }
                group.Add( key, value );
                RaiseChangedEvent();
            }
        }


        /// <summary> Removes entry with the specified group/key from the collection. </summary>
        /// <param name="groupName"> Group name. Cannot be null. </param>
        /// <param name="key"> Key name. Cannot be null. </param>
        /// <returns> True if the entry was located and removed. False if no entry was found. </returns>
        public bool Remove( [NotNull] string groupName, [NotNull] string key ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                Dictionary<string, TValue> pair;
                if( !store.TryGetValue( groupName, out pair ) ) return false;
                if( pair.Remove( key ) ) {
                    if( pair.Count == 0 ) {
                        store.Remove( groupName );
                    }
                    RaiseChangedEvent();
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Enumerates key-value pairs in a group. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        [NotNull]
        public IEnumerable<MetadataEntry<TValue>> GetGroup( [NotNull] string groupName ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            lock( syncRoot ) {
                Dictionary<string, TValue> group;
                if( store.TryGetValue( groupName, out group ) ) {
                    foreach( var key in group ) {
                        yield return new MetadataEntry<TValue>( groupName, key.Key, key.Value );
                    }
                } else {
                    throw new KeyNotFoundException( "No group found with the given name." );
                }
            }
        }


        #region Count / Group Count / Key Count

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


        /// <summary> Counts key/value pairs within a given group.
        /// Throws KeyNotFoundException if no such group exists. </summary>
        /// <param name="group"> Group name. Cannot be null. </param>
        /// <returns> Number of keys within the specified group. </returns>
        public int CountKeys( [NotNull] string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            return store[group].Count;
        }

        #endregion

        #region Index / Get / Set

        /// <summary> Gets or sets the value of a given entry.
        /// If the specified group/key pair is not found, a get operation throws a KeyNotFoundException,
        /// and a set operation creates a new element with the specified group/key. </summary>
        /// <param name="group"> The group of the value to get or set. </param>
        /// <param name="key"> The key of the value to get or set. </param>
        public TValue this[ [NotNull] string group, [NotNull] string key ] {
            get {
                return GetValue( group, key );
            }
            set {
                SetValue( group, key, value );
            }
        }


        [NotNull]
        TValue GetValue( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return store[group][key];
            }
        }


        void SetValue( [NotNull] string groupName, [NotNull] string key, [NotNull] TValue value ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            lock( syncRoot ) {
                Dictionary<string, TValue> group;
                if( !store.TryGetValue( groupName, out group ) ) {
                    group = new Dictionary<string, TValue>();
                    store.Add( groupName, group );
                }
                group[key] = value;
            }
            RaiseChangedEvent();
        }


        /// <summary> Sets the value of a given entry as a MetadataEntry struct. </summary>
        public MetadataEntry<TValue> Get( [NotNull] string group, [NotNull] string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                return new MetadataEntry<TValue>( group, key, store[group][key] );
            }
        }


        /// <summary> Sets the value of an entry corresponding to given MetadataEntry struct.
        /// If the specified group/key pair is not found, a new element is created. </summary>
        public void Set( MetadataEntry<TValue> entry ) {
            SetValue( entry.Group, entry.Key, entry.Value );
        }


        /// <summary> Gets the value associated with the specified group/key. </summary>
        /// <returns> true if this collection contains an element with the specified group/key; otherwise, false. </returns>
        /// <param name="groupName"> The group of the value to get. </param>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value"> When this method returns, contains the value associated with the specified
        /// key, if the key is found; otherwise, the default value for the type of the
        /// value parameter. This parameter is passed uninitialized. </param>
        /// <exception cref="ArgumentNullException"> group or key is null </exception>
        public bool TryGetValue( [NotNull] string groupName, [NotNull] string key, out TValue value ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                Dictionary<string, TValue> group;
                if( !store.TryGetValue( groupName, out group ) ) {
                    value = null;
                    return false;
                }
                return group.TryGetValue( key, out value );
            }
        }

        #endregion

        #region Contains Group / Key / Value

        /// <summary> Determines whether this collection contains a group with the specified name. </summary>
        public bool ContainsGroup( [NotNull] string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            lock( syncRoot ) {
                return store.ContainsKey( group );
            }
        }


        /// <summary> Determines whether this collection contains an entry with the specified group/key pair. </summary>
        public bool ContainsKey( [NotNull] string groupName, [NotNull] string key ) {
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            if( key == null ) throw new ArgumentNullException( "key" );
            lock( syncRoot ) {
                Dictionary<string, TValue> group;
                if( !store.TryGetValue( groupName, out group ) ) {
                    return false;
                }
                return group.ContainsKey( key );
            }
        }


        /// <summary> Determines whether this collection contains a specific value at least once. </summary>
        public bool ContainsValue( [CanBeNull] TValue value ) {
            if( value == null ) return false;
            lock( syncRoot ) {
                foreach( var group in store ) {
                    if( group.Value.ContainsValue( value ) ) {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region ICollection Implementation

        public void Add( MetadataEntry<TValue> item ) {
            Add( item.Group, item.Key, item.Value );
        }


        public void Clear() {
            lock( syncRoot ) {
                bool raiseEvent = (store.Count > 0);
                store.Clear();
                if( raiseEvent ) RaiseChangedEvent();
            }
        }


        public bool Contains( MetadataEntry<TValue> item ) {
            return ContainsKey( item.Group, item.Key );
        }


        public void CopyTo( MetadataEntry<TValue>[] array, int arrayIndex ) {
            if( array == null ) throw new ArgumentNullException( "array" );

            if( arrayIndex < 0 || arrayIndex >= array.Length ) {
                throw new ArgumentOutOfRangeException( "arrayIndex" );
            }

            lock( syncRoot ) {
                if( array.Length < arrayIndex + Count ) {
                    throw new ArgumentOutOfRangeException( "array" );
                }

                int i = 0;
                foreach( var group in store ) {
                    foreach( var pair in group.Value ) {
                        array[i] = new MetadataEntry<TValue>( group.Key, pair.Key, pair.Value );
                        i++;
                    }
                }
            }
        }


        bool ICollection<MetadataEntry<TValue>>.IsReadOnly {
            get { return false; }
        }


        public bool Remove( MetadataEntry<TValue> item ) {
            return Remove( item.Group, item.Key );
        }


        public void CopyTo( Array array, int index ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            var castArray = array as MetadataEntry<TValue>[];
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

        #region IEnumerable Implementation

        /// <summary> Enumerates all keys in this collection. </summary>
        /// <remarks> Lock SyncRoot if this is used in a loop. </remarks>
        public IEnumerator<MetadataEntry<TValue>> GetEnumerator() {
            foreach( var group in store ) {
                foreach( var pair in group.Value ) {
                    yield return new MetadataEntry<TValue>( group.Key, pair.Key, pair.Value );
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region ICloneable Implementation

        public object Clone() {
            return new MetadataCollection<TValue>( this );
        }

        #endregion

        #region INotifiesOnChange Implementation

        /// <summary> Fired when an element is added, removed, or changed. </summary>
        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }

        #endregion
    }
}
