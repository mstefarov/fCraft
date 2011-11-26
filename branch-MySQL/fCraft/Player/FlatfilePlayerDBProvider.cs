using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class FlatfilePlayerDBProvider : IPlayerDBProvider {
        const int BufferSize = 64 * 1024;
        int maxID = 255;

        public int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        readonly object syncRoot = new object();
        public object SyncRoot {
            get { return syncRoot; }
        }

        /* 
         * Version 0 - before 0.530 - all dates/times are local
         * Version 1 - 0.530-0.536 - all dates and times are stored as UTC unix timestamps (milliseconds)
         * Version 2 - 0.600 dev - all dates and times are stored as UTC unix timestamps (seconds)
         * Version 3 - 0.600 dev - same as v2, but sorting by ID is enforced
         * Version 4 - 0.600 dev - added LastModified column, forced banned players to be unfrozen/unmuted/unhidden.
         * Version 5 - 0.600+ - removed FailedLoginCount column
         */
        const int FormatVersion = 5;


        // used to prevent concurrent access to the PlayerDB file
        readonly object SaveLoadLocker = new object();


        readonly Trie<PlayerInfo> trie = new Trie<PlayerInfo>();


        public void Remove( [NotNull] PlayerInfo playerInfo ) {
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            throw new NotImplementedException();
        }


        public void PullChanges( [NotNull] params PlayerInfo[] playerInfo ) {
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            throw new NotImplementedException();
        }


        public void PushChanges( [NotNull] params PlayerInfo[] playerInfo ) {
            if( playerInfo == null ) throw new ArgumentNullException( "playerInfo" );
            throw new NotImplementedException();
        }


        [CanBeNull]
        public PlayerInfo FindExact( [NotNull] string fullName ) {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            lock( trie.SyncRoot ) {
                return trie.Get( fullName );
            }
        }


        public bool FindOneByPartialName( [NotNull] string partialName, [CanBeNull] out PlayerInfo result ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            lock( trie.SyncRoot ) {
                return trie.GetOneMatch( partialName, out result );
            }
        }


        [NotNull]
        public IEnumerable<PlayerInfo> FindByIP( [NotNull] IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            List<PlayerInfo> result = new List<PlayerInfo>();
            PlayerInfo[] cache = PlayerDB.PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Equals( address ) ) {
                    result.Add( cache[i] );
                    if( result.Count >= limit ) break;
                }
            }
            return result.ToArray();
        }


        [NotNull]
        public IEnumerable<PlayerInfo> FindByPartialName( [NotNull] string partialName, int limit ) {
            if( partialName == null ) throw new ArgumentNullException( "partialName" );
            lock( syncRoot ) {
                return trie.GetList( partialName, limit );
            }
        }


        static readonly Regex RegexNonNameChars = new Regex( @"[^a-zA-Z0-9_\*\?]", RegexOptions.Compiled );

        [NotNull]
        public IEnumerable<PlayerInfo> FindByPattern( [NotNull] string pattern, int limit ) {
            if( pattern == null ) throw new ArgumentNullException( "pattern" );
            string regexString = "^" + RegexNonNameChars.Replace( pattern, "" ).Replace( "*", ".*" ).Replace( "?", "." ) + "$";
            Regex regex = new Regex( regexString, RegexOptions.IgnoreCase );
            List<PlayerInfo> result = new List<PlayerInfo>();
            PlayerInfo[] cache = PlayerDB.PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( regex.IsMatch( cache[i].Name ) ) {
                    result.Add( cache[i] );
                    if( result.Count >= limit ) break;
                }
            }
            return result.ToArray();
        }


        public void MassRankChange( [NotNull] Player player, [NotNull] Rank from, [NotNull] Rank to, [NotNull] string reason ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( reason == null ) throw new ArgumentNullException( "reason" );
            throw new NotImplementedException();
        }


        public void SwapInfo( [NotNull] PlayerInfo player1, [NotNull] PlayerInfo player2 ) {
            if( player1 == null ) throw new ArgumentNullException( "player1" );
            if( player2 == null ) throw new ArgumentNullException( "player2" );
            throw new NotImplementedException();
        }


        #region Saving/Loading

        [CanBeNull]
        public IEnumerable<PlayerInfo> Load() {
            //LoadBinary();
            //return;
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    using( FileStream fs = OpenRead( Paths.PlayerDBFileName ) ) {
                        using( StreamReader reader = new StreamReader( fs, Encoding.UTF8, true, BufferSize ) ) {

                            string header = reader.ReadLine();

                            if( header == null ) return null; // if PlayerDB is an empty file

                            lock( syncRoot ) {
                                int version = IdentifyFormatVersion( header );
                                if( version > FormatVersion ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                                version );
                                } else if( version < FormatVersion ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Converting PlayerDB to a newer format (version {0} to {1}).",
                                                version, FormatVersion );
                                }

                                int emptyRecords = 0;
                                while( true ) {
                                    string line = reader.ReadLine();
                                    if( line == null ) break;
                                    string[] fields = line.Split( ',' );
                                    if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                                        try {
#endif
                                        PlayerInfo info;
                                        switch( version ) {
                                            case 0:
                                                info = FlatfilePlayerInfo.LoadFormat0( this, fields );
                                                break;
                                            case 1:
                                                info = FlatfilePlayerInfo.LoadFormat1( this, fields );
                                                break;
                                            default:
                                                // Versions 2-5 differ in semantics only, not in actual serialization format.
                                                info = FlatfilePlayerInfo.LoadFormat2( this, fields );
                                                break;
                                        }

                                        if( info.ID > maxID ) {
                                            maxID = info.ID;
                                            Logger.Log( LogType.Warning, "PlayerDB.Load: Adjusting wrongly saved MaxID ({0} to {1})." );
                                        }

                                        // A record is considered "empty" if the player has never logged in.
                                        // Empty records may be created by /Import, /Ban, and /Rank commands on typos.
                                        // Deleting such records should have no negative impact on DB completeness.
                                        if( (info.LastIP.Equals( IPAddress.None ) || info.LastIP.Equals( IPAddress.Any ) || info.TimesVisited == 0) &&
                                            !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                                            Logger.Log( LogType.SystemActivity,
                                                        "PlayerDB.Load: Skipping an empty record for player \"{0}\"",
                                                        info.Name );
                                            emptyRecords++;
                                            continue;
                                        }

                                        // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                                        if( trie.ContainsKey( info.Name ) ) {
                                            Logger.Log( LogType.Error,
                                                        "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.",
                                                        info.Name );
                                        } else {
                                            trie.Add( info.Name, info );
                                        }
#if !DEBUG
                                        } catch( Exception ex ) {
                                            Logger.LogAndReportCrash( "Error while parsing PlayerInfo record",
                                                                      "fCraft",
                                                                      ex,
                                                                      false );
                                        }
#endif
                                    } else {
                                        Logger.Log( LogType.Error,
                                                    "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.",
                                                    fields.Length, PlayerInfo.MinFieldCount );
                                    }
                                }

                                if( emptyRecords > 0 ) {
                                    Logger.Log( LogType.Warning,
                                                "PlayerDB.Load: Skipped {0} empty records.", emptyRecords );
                                }
                            }
                        }
                    }
                } else {
                    Logger.Log( LogType.Warning, "PlayerDB.Load: No player DB file found." );
                }
            }
            return trie.Values;
        }


        Dictionary<int, Rank> rankMapping;


        [NotNull]
        public Rank GetRankByIndex( int index ) {
            Rank rank;
            if( rankMapping.TryGetValue( index, out rank ) ) {
                return rank;
            } else {
                Logger.Log( LogType.Error,
                            "Unknown rank index ({0}). Assigning rank {1} instead.",
                            index, RankManager.DefaultRank );
                return RankManager.DefaultRank;
            }
        }


        internal void LoadBinary() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName + ".bin" ) ) {
                    using( FileStream fs = OpenRead( Paths.PlayerDBFileName + ".bin" ) ) {
                        BinaryReader reader = new BinaryReader( fs );
                        int version = reader.ReadInt32();

                        if( version > FormatVersion ) {
                            Logger.Log( LogType.Warning,
                                        "PlayerDB.LoadBinary: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                        version );
                        } else if( version < FormatVersion ) {
                            Logger.Log( LogType.Warning,
                                        "PlayerDB.LoadBinary: Converting PlayerDB to a newer format (version {0} to {1}).",
                                        version, FormatVersion );
                        }

                        maxID = reader.ReadInt32();

                        lock( syncRoot ) {
                            int rankCount = reader.ReadInt32();
                            rankMapping = new Dictionary<int, Rank>( rankCount );
                            for( int i = 0; i < rankCount; i++ ) {
                                byte rankIndex = reader.ReadByte();
                                string rankName = reader.ReadString();
                                Rank rank = Rank.Parse( rankName );
                                if( rank == null ) {
                                    Logger.Log( LogType.Error,
                                                "PlayerDB.LoadBinary: Could not parse rank: \"{0}\". Assigning rank {1} instead.",
                                                rankName, RankManager.DefaultRank );
                                    rank = RankManager.DefaultRank;
                                }
                                rankMapping.Add( rankIndex, rank );
                            }
                            int records = reader.ReadInt32();

                            int emptyRecords = 0;
                            for( int i = 0; i < records; i++ ) {
#if !DEBUG
                                try {
#endif
                                PlayerInfo info = FlatfilePlayerInfo.LoadBinaryFormat0( this, reader );

                                if( info.ID > maxID ) {
                                    maxID = info.ID;
                                    Logger.Log( LogType.Warning, "PlayerDB.LoadBinary: Adjusting wrongly saved MaxID ({0} to {1})." );
                                }

                                // A record is considered "empty" if the player has never logged in.
                                // Empty records may be created by /Import, /Ban, and /Rank commands on typos.
                                // Deleting such records should have no negative impact on DB completeness.
                                if( (info.LastIP.Equals( IPAddress.None ) || info.LastIP.Equals( IPAddress.Any ) || info.TimesVisited == 0) &&
                                    !info.IsBanned && info.Rank == RankManager.DefaultRank ) {

                                    Logger.Log( LogType.SystemActivity,
                                                "PlayerDB.LoadBinary: Skipping an empty record for player \"{0}\"",
                                                info.Name );
                                    emptyRecords++;
                                    continue;
                                }

                                // Check for duplicates. Unless PlayerDB.txt was altered externally, this does not happen.
                                if( trie.ContainsKey( info.Name ) ) {
                                    Logger.Log( LogType.Error,
                                                "PlayerDB.LoadBinary: Duplicate record for player \"{0}\" skipped.",
                                                info.Name );
                                } else {
                                    trie.Add( info.Name, info );
                                }
#if !DEBUG
                                } catch( Exception ex ) {
                                    Logger.LogAndReportCrash( "Error while parsing PlayerInfo record",
                                                              "fCraft",
                                                              ex,
                                                              false );
                                }
#endif
                            }

                            if( emptyRecords > 0 ) {
                                Logger.Log( LogType.Warning,
                                            "PlayerDB.LoadBinary: Skipped {0} empty records.", emptyRecords );
                            }
                        }
                    }
                } else {
                    Logger.Log( LogType.Warning, "PlayerDB.Load: No player DB file found." );
                }
            }
        }


        int IdentifyFormatVersion( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new FormatException( "Invalid PlayerDB header format: " + header );
            }
            int maxIDField;
            if( Int32.TryParse( headerParts[0], out maxIDField ) ) {
                if( maxIDField >= 255 ) {// IDs start at 256
                    maxID = maxIDField;
                }
            }
            int version;
            if( Int32.TryParse( headerParts[1], out version ) ) {
                return version;
            } else {
                return 0;
            }
        }


        public void Save() {
            const string tempFileName = Paths.PlayerDBFileName + ".bin.temp";

            lock( SaveLoadLocker ) {
                PlayerInfo[] listCopy = PlayerDB.PlayerInfoList;
                using( FileStream fs = OpenWrite( tempFileName ) ) {
                    BinaryWriter writer = new BinaryWriter( fs );
                    writer.Write( FormatVersion );
                    writer.Write( maxID );
                    writer.Write( RankManager.Ranks.Count );
                    foreach( Rank rank in RankManager.Ranks ) {
                        writer.Write( (byte)rank.Index );
                        writer.Write( rank.FullName );
                    }
                    writer.Write( listCopy.Length );
                    for( int i = 0; i < listCopy.Length; i++ ) {
                        ((FlatfilePlayerInfo)listCopy[i]).SaveBinaryFormat0( writer );
                    }
                }

                try {
                    Paths.MoveOrReplace( tempFileName, Paths.PlayerDBFileName + ".bin" );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "PlayerDB.SaveBinary: An error occured while trying to save PlayerDB: {0}", ex );
                }
            }
        }


        [NotNull]
        static FileStream OpenRead( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan );
        }


        [NotNull]
        static FileStream OpenWrite( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return new FileStream( fileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize );
        }

        #endregion


        [NotNull]
        public PlayerInfo AddPlayer( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            int id = GetNextID();
            FlatfilePlayerInfo info = new FlatfilePlayerInfo( id, name, lastIP, startingRank, rankChangeType );
            trie.Add( name, info );
            return info;
        }


        [NotNull]
        public PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            int id = GetNextID();
            FlatfilePlayerInfo info = new FlatfilePlayerInfo( id, name, IPAddress.None, startingRank, rankChangeType );
            trie.Add( name, info );
            return info;
        }


        [NotNull]
        public PlayerInfo AddSuperPlayer( ReservedPlayerIDs id, [NotNull] string name, [NotNull] Rank rank ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            FlatfilePlayerInfo info = new FlatfilePlayerInfo( (int)id, name, IPAddress.None, rank );
            trie.Add( name, info );
            return info;
        }
    }
}
