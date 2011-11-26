using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    public sealed partial class FlatfilePlayerDBProvider : IPlayerDBProvider {
        const int BufferSize = 64 * 1024;
        int maxID = 255;

        public int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        [NotNull]
        readonly object syncRoot = new object();

        [NotNull]
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
        [NotNull]
        readonly object saveLoadLocker = new object();


        [NotNull]
        readonly Trie<PlayerInfo> trie = new Trie<PlayerInfo>();


        public void Remove( [NotNull] PlayerInfo playerInfo ) {
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


        [NotNull]
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


        #region Loading

        Dictionary<int, Rank> rankMapping;

        [CanBeNull]
        public IEnumerable<PlayerInfo> Load() {
            //LoadBinary();
            //return;
            lock( saveLoadLocker ) {
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
                                    if( fields.Length >= MinFieldCount ) {
#if !DEBUG
                                        try {
#endif
                                            PlayerInfo info;
                                            switch( version ) {
                                                case 0:
                                                    info = LoadFormat0( fields );
                                                    break;
                                                case 1:
                                                    info = LoadFormat1( fields );
                                                    break;
                                                default:
                                                    // Versions 2-5 differ in semantics only, not in actual serialization format.
                                                    info = LoadFormat2( fields );
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
                                                    fields.Length, MinFieldCount );
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
            lock( saveLoadLocker ) {
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
                                    PlayerInfo info = LoadBinaryFormat0( reader );

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



        internal PlayerInfo LoadBinaryFormat0( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int id = Read7BitEncodedInt( reader );
            // ReSharper disable UseObjectOrCollectionInitializer
            PlayerInfo info = new PlayerInfo( id );
            // ReSharper restore UseObjectOrCollectionInitializer

            // General
            info.Name = reader.ReadString();
            info.DisplayedName = ReadString( reader );
            info.LastSeen = DateTimeUtil.ToDateTime( reader.ReadUInt32() );

            // Rank
            int rankIndex = Read7BitEncodedInt( reader );
            info.Rank = GetRankByIndex( rankIndex );
            {
                bool hasPrevRank = reader.ReadBoolean();
                if( hasPrevRank ) {
                    int prevRankIndex = Read7BitEncodedInt( reader );
                    info.Rank = GetRankByIndex( prevRankIndex );
                }
            }
            info.RankChangeType = (RankChangeType)reader.ReadByte();
            if( info.RankChangeType != RankChangeType.Default ) {
                info.RankChangeDate = ReadDate( reader );
                info.RankChangedBy = ReadString( reader );
                info.RankChangeReason = ReadString( reader );
            }

            // Bans
            info.BanStatus = (BanStatus)reader.ReadByte();
            info.BanDate = ReadDate( reader );
            info.BannedBy = ReadString( reader );
            info.BanReason = ReadString( reader );
            if( info.BanStatus == BanStatus.Banned ) {
                info.BannedUntil = ReadDate( reader );
                info.LastFailedLoginDate = ReadDate( reader );
                info.LastFailedLoginIP = new IPAddress( reader.ReadBytes( 4 ) );
            } else {
                info.UnbanDate = ReadDate( reader );
                info.UnbannedBy = ReadString( reader );
                info.UnbanReason = ReadString( reader );
            }

            // Stats
            info.FirstLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.LastLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.TotalTime = new TimeSpan( reader.ReadUInt32() * TimeSpan.TicksPerSecond );
            info.BlocksBuilt = Read7BitEncodedInt( reader );
            info.BlocksDeleted = Read7BitEncodedInt( reader );
            if( reader.ReadBoolean() ) {
                info.BlocksDrawn = reader.ReadInt64();
            }
            info.TimesVisited = Read7BitEncodedInt( reader );
            info.MessagesWritten = Read7BitEncodedInt( reader );
            info.TimesKickedOthers = Read7BitEncodedInt( reader );
            info.TimesBannedOthers = Read7BitEncodedInt( reader );

            // Kicks
            info.TimesKicked = Read7BitEncodedInt( reader );
            if( info.TimesKicked > 0 ) {
                info.LastKickDate = ReadDate( reader );
                info.LastKickBy = ReadString( reader );
                info.LastKickReason = ReadString( reader );
            }

            // Freeze/Mute
            info.IsFrozen = reader.ReadBoolean();
            if( info.IsFrozen ) {
                info.FrozenOn = ReadDate( reader );
                info.FrozenBy = ReadString( reader );
            }
            info.MutedUntil = ReadDate( reader );
            if( info.MutedUntil != DateTime.MinValue ) {
                info.MutedBy = ReadString( reader );
            }

            // Misc
            info.Password = ReadString( reader );
            info.LastModified = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            reader.ReadBoolean(); // info.IsOnline - skip
            info.IsHidden = reader.ReadBoolean();
            info.LastIP = new IPAddress( reader.ReadBytes( 4 ) );
            info.LeaveReason = (LeaveReason)reader.ReadByte();
            info.BandwidthUseMode = (BandwidthUseMode)reader.ReadByte();

            return info;
        }


        static DateTime ReadDate( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            } else {
                return DateTime.MinValue;
            }
        }


        static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return reader.ReadString();
            } else {
                return null;
            }
        }


        static int Read7BitEncodedInt( [NotNull] BinaryReader reader ) {
            byte num3;
            int num = 0;
            int num2 = 0;
            do {
                if( num2 == 0x23 ) {
                    throw new FormatException( "Invalid 7bit encoded integer." );
                }
                num3 = reader.ReadByte();
                num |= (num3 & 0x7f) << num2;
                num2 += 7;
            }
            while( (num3 & 0x80) != 0 );
            return num;
        }

        [NotNull]
        static FileStream OpenRead( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan );
        }

        #endregion


        #region Saving

        public void Save() {
            const string tempFileName = Paths.PlayerDBFileName + ".bin.temp";

            lock( saveLoadLocker ) {
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
                        SaveBinaryFormat0( listCopy[i], writer );
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
        static FileStream OpenWrite( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return new FileStream( fileName, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize );
        }


        static void SaveBinaryFormat0( PlayerInfo info, [NotNull] BinaryWriter writer ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            // General
            writer.Write( info.Name ); // 0
            WriteString( writer, info.DisplayedName ); // 1
            Write7BitEncodedInt( writer, info.ID ); // 2
            if( info.IsOnline ) {
                writer.Write( (uint)DateTime.UtcNow.ToUnixTime() ); // 5
            } else {
                writer.Write( (uint)info.LastSeen.ToUnixTime() ); // 5
            }

            // Rank
            Write7BitEncodedInt( writer, info.Rank.Index ); // 7
            {
                bool hasPrevRank = (info.PreviousRank != null); // 8 prefix
                writer.Write( hasPrevRank );
                if( hasPrevRank ) {
                    Write7BitEncodedInt( writer, info.PreviousRank.Index ); // 8
                }
            }
            writer.Write( (byte)info.RankChangeType ); // 12
            if( info.RankChangeType != RankChangeType.Default ) {
                WriteDate( writer, info.RankChangeDate ); // 9
                WriteString( writer, info.RankChangedBy ); // 10
                WriteString( writer, info.RankChangeReason ); // 11
            }

            // Bans
            writer.Write( (byte)info.BanStatus ); // 13
            WriteDate( writer, info.BanDate ); // 14
            WriteString( writer, info.BannedBy ); // 15
            WriteString( writer, info.BanReason ); // 16
            if( info.BanStatus == BanStatus.Banned ) {
                WriteDate( writer, info.BannedUntil ); // 14
                WriteDate( writer, info.LastFailedLoginDate ); // 20
                writer.Write( info.LastFailedLoginIP.GetAddressBytes() ); // 21
            } else {
                WriteDate( writer, info.UnbanDate ); // 17
                WriteString( writer, info.UnbannedBy ); // 18
                WriteString( writer, info.UnbanReason ); // 18
            }

            // Stats
            writer.Write( (uint)info.FirstLoginDate.ToUnixTime() ); // 3
            writer.Write( (uint)info.LastLoginDate.ToUnixTime() ); // 4
            if( info.IsOnline ) {
                writer.Write( (uint)info.TotalTime.Add( info.TimeSinceLastLogin ).ToSeconds() ); // 22
            } else {
                writer.Write( (uint)info.TotalTime.ToSeconds() ); // 22
            }
            Write7BitEncodedInt( writer, info.BlocksBuilt ); // 23
            Write7BitEncodedInt( writer, info.BlocksDeleted ); // 24
            {
                bool hasBlocksDrawn = (info.BlocksDrawn > 0); // 25 prefix
                writer.Write( hasBlocksDrawn );
                if( hasBlocksDrawn ) {
                    writer.Write( info.BlocksDrawn ); // 25
                }
            }
            Write7BitEncodedInt( writer, info.TimesVisited ); // 26
            Write7BitEncodedInt( writer, info.MessagesWritten ); // 27
            Write7BitEncodedInt( writer, info.TimesKickedOthers ); // 28
            Write7BitEncodedInt( writer, info.TimesBannedOthers ); // 29

            // Kicks
            Write7BitEncodedInt( writer, info.TimesKicked ); // 30
            if( info.TimesKicked > 0 ) {
                WriteDate( writer, info.LastKickDate ); // 31
                WriteString( writer, info.LastKickBy ); // 32
                WriteString( writer, info.LastKickReason ); // 33
            }

            // Freeze/Mute
            writer.Write( info.IsFrozen ); // 34
            if( info.IsFrozen ) {
                WriteDate( writer, info.FrozenOn ); // 35
                WriteString( writer, info.FrozenBy ); // 36
            }
            WriteDate( writer, info.MutedUntil ); // 37
            if( info.MutedUntil != DateTime.MinValue ) {
                WriteString( writer, info.MutedBy ); // 38
            }

            // Misc
            WriteString( writer, info.Password ); // 39
            writer.Write( (uint)info.LastModified.ToUnixTime() ); // 40
            writer.Write( info.IsOnline ); // 41
            writer.Write( info.IsHidden ); // 42
            writer.Write( info.LastIP.GetAddressBytes() ); // 43
            writer.Write( (byte)info.LeaveReason ); // 44
            writer.Write( (byte)info.BandwidthUseMode ); // 45
        }


        static void WriteDate( [NotNull] BinaryWriter writer, DateTime dateTime ) {
            bool hasDate = (dateTime != DateTime.MinValue);
            writer.Write( hasDate );
            if( hasDate ) {
                writer.Write( (uint)dateTime.ToUnixTime() );
            }
        }


        static void WriteString( [NotNull] BinaryWriter writer, [CanBeNull] string str ) {
            bool hasString = (str != null);
            writer.Write( hasString );
            if( hasString ) {
                writer.Write( str );
            }
        }


        static void Write7BitEncodedInt( [NotNull] BinaryWriter writer, int value ) {
            uint num = (uint)value;
            while( num >= 0x80 ) {
                writer.Write( (byte)(num | 0x80) );
                num = num >> 7;
            }
            writer.Write( (byte)num );
        }
        #endregion


        [NotNull]
        public PlayerInfo AddPlayer( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            int id = GetNextID();
            PlayerInfo info = new PlayerInfo( id, name, lastIP, startingRank, rankChangeType );
            trie.Add( name, info );
            return info;
        }


        [NotNull]
        public PlayerInfo AddUnrecognizedPlayer( [NotNull] string name, [NotNull] Rank startingRank, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            int id = GetNextID();
            PlayerInfo info = new PlayerInfo( id, name, IPAddress.None, startingRank, rankChangeType );
            trie.Add( name, info );
            return info;
        }


        [NotNull]
        public PlayerInfo AddSuperPlayer( ReservedPlayerID id, [NotNull] string name, [NotNull] Rank rank ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            PlayerInfo info = new PlayerInfo( (int)id, name, IPAddress.None, rank );
            trie.Add( name, info );
            return info;
        }
    }
}