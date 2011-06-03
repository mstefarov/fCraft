using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    struct MetadataEntry {
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


    class MetadataCollection : ICollection<MetadataEntry> {
        const string EmptyGroup = "";

        readonly Dictionary<string, Dictionary<string, string>> store = new Dictionary<string, Dictionary<string, string>>();


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
            group = group.ToLower();
            key = key.ToLower();    
            if( !store.ContainsKey( group ) ) {
                store.Add( group, new Dictionary<string, string>() );
            }
            store[group].Add( key, value );
        }


        public bool Remove( string key ) {
            return Remove( EmptyGroup, key );
        }


        public bool Remove( string group, string key ) {
            Dictionary<string, string> pair;
            if( !store.TryGetValue( group, out pair ) ) return false;
            return pair.Remove( key );
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
            return store[group][key];
        }


        public void SetValue( string key, string value ) {
            SetValue( EmptyGroup, key, value );
        }
        

        public void SetValue( string group, string key, string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            if( value == null ) throw new ArgumentNullException( "value" );
            group = group.ToLower();
            key = key.ToLower();
            if( !store.ContainsKey( group ) ) {
                store.Add( group, new Dictionary<string, string>() );
            }
            store[group][key] = value;
        }

        #endregion


        #region Contains Group/Key

        public bool ContainsGroup( string group ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            group = group.ToLower();
            return store.ContainsKey( group );
        }


        public bool ContainsKey( string key ) {
            if( key == null ) throw new ArgumentNullException( "key" );
            key = key.ToLower();
            return store[EmptyGroup].ContainsKey( key );
        }


        public bool ContainsKey( string group, string key ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            group = group.ToLower();
            key = key.ToLower();
            return store.ContainsKey( group ) &&
                   store[group].ContainsKey( key );
        }

        #endregion


        #region TryGetValue

        public bool TryGetValue( string key, out string value ) {
            return TryGetValue( EmptyGroup, key, out value );
        }


        public bool TryGetValue( string group, string key, out string value ) {
            if( group == null ) throw new ArgumentNullException( "group" );
            if( key == null ) throw new ArgumentNullException( "key" );
            group = group.ToLower();
            key = key.ToLower();
            Dictionary<string,string> pair;
            if( !store.TryGetValue( group, out pair ) ) {
                value = null;
                return false;
            }
            return pair.TryGetValue( key, out value );
        }

        #endregion


        #region ICollection<MetadataEntry> Members

        public void Add( MetadataEntry item ) {
            Add( item.Group, item.Key, item.Value );
        }


        public void Clear() {
            store.Clear();
        }


        public bool Contains( MetadataEntry item ) {
            return store.ContainsKey( item.Group ) &&
                   store[item.Group].ContainsKey( item.Key );
        }


        public void CopyTo( MetadataEntry[] array, int arrayIndex ) {
            if( array == null ) {
                throw new ArgumentNullException( "array" );
            }

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


        bool ICollection<MetadataEntry>.IsReadOnly {
            get { return false; }
        }


        public bool Remove( MetadataEntry item ) {
            return Remove( item.Group, item.Key );
        }

        #endregion


        #region IEnumerable<MetadataEntry> Members

        public IEnumerator<MetadataEntry> GetEnumerator() {
            throw new NotImplementedException();
        }

        #endregion


        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion
    }
}
