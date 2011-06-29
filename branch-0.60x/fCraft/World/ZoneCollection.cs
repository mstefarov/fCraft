using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
    /// <summary> A collection of zones within a map. </summary>
    public sealed class ZoneCollection : ICollection<Zone>, ICollection, ICloneable, INotifiesOnChange {
        readonly Dictionary<string, Zone> store = new Dictionary<string, Zone>();

        public Zone[] Cache { get; private set; }

        public ZoneCollection() {
            UpdateCache();
        }

        public ZoneCollection( ZoneCollection other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( other.syncRoot ) {
                foreach( Zone zone in other.store.Values ) {
                    Add( zone );
                }
            }
        }

        void UpdateCache() {
            lock( syncRoot ) {
                Cache = store.Values.ToArray();
            }
        }


        /// <summary> Adds a new zone to the collection.
        /// The name of the zone cannot match existing names. </summary>
        public void Add( Zone item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            lock( syncRoot ) {
                string zoneName = item.Name.ToLower();
                if( store.ContainsValue( item ) ) {
                    throw new ArgumentException( "Duplicate zone.", "item" );
                }
                store.Add( zoneName, item );
                item.Changed += OnZoneChanged;
                UpdateCache();
                RaiseChangedEvent();
            }
        }


        /// <summary> Removes all zones from the collection. </summary>
        public void Clear() {
            lock( syncRoot ) {
                if( store.Count > 0 ) {
                    foreach( Zone zone in store.Values ) {
                        zone.Changed -= OnZoneChanged;
                    }
                    store.Clear();
                    UpdateCache();
                    RaiseChangedEvent();
                }
            }
        }


        /// <summary> Checks whether a given zone is in the collection. </summary>
        public bool Contains( Zone item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            Zone[] cache = Cache;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i] == item ) return true;
            }
            return false;
        }


        public int Count {
            get { return store.Count; }
        }


        /// <summary> Removes a zone from the collection. </summary>
        /// <returns> True if the given zone was found & removed.
        /// False if this collection did not contain the given zone. </returns>
        public bool Remove( Zone item ) {
            if( item == null ) throw new ArgumentNullException( "item" );
            lock( syncRoot ) {
                if( store.ContainsValue( item ) ) {
                    store.Remove( item.Name.ToLower() );
                    UpdateCache();
                    RaiseChangedEvent();
                    item.Changed -= OnZoneChanged;
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Removes a zone from the collection, by name. </summary>
        /// <returns> True if the given zone was found & removed.
        /// False if this collection did not contain the given zone. </returns>
        public bool Remove( string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            lock( syncRoot ) {
                string zoneNameLower = zoneName.ToLower();
                Zone item;
                if( store.TryGetValue( zoneNameLower, out item ) ) {
                    item.Changed -= OnZoneChanged;
                    store.Remove( zoneNameLower );
                    UpdateCache();
                    RaiseChangedEvent();
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Checks how zones affect the given player's ability to affect
        /// a block at given coordinates. </summary>
        /// <param name="x"> Block's X coordinate. </param>
        /// <param name="y"> Block's Y coordinate. </param>
        /// <param name="h"> Block's H coordinate. </param>
        /// <param name="player"> Player to check. </param>
        /// <returns> None if no zones affect the coordinate.
        /// Allow if ALL affecting zones allow the player.
        /// Deny if ANY affecting zone denies the player. </returns>
        public PermissionOverride Check( int x, int y, int h, Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            PermissionOverride result = PermissionOverride.None;
            if( Cache.Length == 0 ) return result;

            Zone[] zoneListCache = Cache;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) ) {
                    if( zoneListCache[i].Controller.Check( player.Info ) ) {
                        result = PermissionOverride.Allow;
                    } else {
                        return PermissionOverride.Deny;
                    }
                }
            }
            return result;
        }


        /// <summary> Checks how zones affect the given player's ability to affect
        /// a block at given coordinates, in detail. </summary>
        /// <param name="x"> Block's X coordinate. </param>
        /// <param name="y"> Block's Y coordinate. </param>
        /// <param name="h"> Block's H coordinate. </param>
        /// <param name="player"> Player to check. </param>
        /// <param name="allowedZones"> Array of zones that allow the player to build. </param>
        /// <param name="deniedZones"> Array of zones that deny the player from building. </param>
        /// <returns> True if any zones were found. False if none affect the given coordinate. </returns>
        public bool CheckDetailed( short x, short y, short h, Player player, out Zone[] allowedZones, out Zone[] deniedZones ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            var allowedList = new List<Zone>();
            var deniedList = new List<Zone>();
            bool found = false;

            Zone[] zoneListCache = Cache;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) ) {
                    found = true;
                    if( zoneListCache[i].Controller.Check( player.Info ) ) {
                        allowedList.Add( zoneListCache[i] );
                    } else {
                        deniedList.Add( zoneListCache[i] );
                    }
                }
            }
            allowedZones = allowedList.ToArray();
            deniedZones = deniedList.ToArray();
            return found;
        }


        /// <summary> Finds which zone denied player's ability to affect
        /// a block at given coordinates. Used in conjunction with CheckZones(). </summary>
        /// <param name="x"> Block's X coordinate. </param>
        /// <param name="y"> Block's Y coordinate. </param>
        /// <param name="h"> Block's H coordinate. </param>
        /// <param name="player"> Player to check. </param>
        /// <returns> First zone to deny the player.
        /// null if none of the zones deny the player. </returns>
        public Zone FindDenied( int x, int y, int h, Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Zone[] zoneListCache = Cache;
            for( int i = 0; i < zoneListCache.Length; i++ ) {
                if( zoneListCache[i].Bounds.Contains( x, y, h ) &&
                    !zoneListCache[i].Controller.Check( player.Info ) ) {
                    return zoneListCache[i];
                }
            }
            return null;
        }


        /// <summary> Finds a zone by name, without using autocompletion.
        /// Zone names are case-insensitive. </summary>
        /// <param name="name"> Full zone name. </param>
        /// <returns> Zone object if it was found.
        /// null if no Zone with the given name could be found. </returns>
        public Zone FindExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( syncRoot ) {
                Zone result;
                if( store.TryGetValue( name.ToLower(), out result ) ) {
                    return result;
                }
            }
            return null;
        }


        /// <summary> Finds a zone by name, with autocompletion.
        /// Zone names are case-insensitive. </summary>
        /// <remarks> Note that this method is a lot slower than FindZoneExact. </remarks>
        /// <param name="name"> Full zone name. </param>
        /// <returns> Zone object if it was found.
        /// null if no Zone with the given name could be found. </returns>
        public Zone Find( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            // try to find exact match
            lock( syncRoot ) {
                Zone result;
                if( store.TryGetValue( name.ToLower(), out result ) ) {
                    return result;
                }
            }
            // try to autocomplete
            Zone match = null;
            Zone[] cache = Cache;
            foreach( Zone zone in cache ) {
                if( zone.Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    if( match == null ) {
                        // first (and hopefully only) match found
                        match = zone;
                    } else {
                        // more than one match found
                        return null;
                    }
                }
            }
            return match;
        }


        /// <summary> Changes the name of a given zone. </summary>
        /// <param name="zone"> Zone to rename. </param>
        /// <param name="newName"> New name to give to the zone. </param>
        /// <exception cref="System.ArgumentException"> Thrown if a zone with a given name already exists. </exception>
        public void Rename( Zone zone, string newName ) {
            if( zone == null ) throw new ArgumentNullException( "zone" );
            if( newName == null ) throw new ArgumentNullException( "newName" );
            lock( syncRoot ) {
                if( !store.Remove( zone.Name.ToLower() ) ) {
                    throw new ArgumentException( "Trying to rename a zone that does not exist.", "zone" );
                }
                zone.Name = newName;
                store.Add( newName.ToLower(), zone );
                UpdateCache();
                RaiseChangedEvent();
            }
        }


        public IEnumerator<Zone> GetEnumerator() {
            return store.Values.GetEnumerator();
        }


        #region ICollection Boilerplate

        IEnumerator IEnumerable.GetEnumerator() {
            return store.Values.GetEnumerator();
        }


        public void CopyTo( Zone[] array, int arrayIndex ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( arrayIndex < 0 || arrayIndex > array.Length ) throw new ArgumentOutOfRangeException( "arrayIndex" );
            Zone[] cache = Cache;
            Array.Copy( cache, 0, array, arrayIndex, cache.Length );
        }


        public void CopyTo( Array array, int index ) {
            if( array == null ) throw new ArgumentNullException( "array" );
            if( index < 0 || index > array.Length ) throw new ArgumentOutOfRangeException( "index" );
            Zone[] cache = Cache;
            Array.Copy( cache, 0, array, index, cache.Length );
        }


        public bool IsReadOnly {
            get { return false; }
        }


        public bool IsSynchronized {
            get { return true; }
        }


        object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }

        #endregion


        public object Clone() {
            lock( syncRoot ) {
                return new ZoneCollection( this );
            }
        }


        public event EventHandler Changed;

        void OnZoneChanged( object sender, EventArgs e ) {
            RaiseChangedEvent();
        }

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }
}
