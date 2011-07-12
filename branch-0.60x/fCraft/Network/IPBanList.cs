// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using fCraft.Events;

namespace fCraft {
    public static class IPBanList {

        static readonly SortedDictionary<string, IPBanInfo> Bans = new SortedDictionary<string, IPBanInfo>();
        const string Header = "IP,bannedBy,banDate,banReason,playerName,attempts,lastAttemptName,lastAttemptDate";
        const int ForamtVersion = 1;
        static readonly object BanListLock = new object();
        public static bool IsLoaded { get; private set; }


        internal static void Load() {
            if( File.Exists( Paths.IPBanListFileName ) ) {
                using( StreamReader reader = File.OpenText( Paths.IPBanListFileName ) ) {

                    string headerText = reader.ReadLine();
                    if( headerText == null ) {
                        Logger.Log( "IPBanList.Load: IP ban file is empty.", LogType.Warning );
                    }

                    int version = ParseHeader( headerText );

                    while( !reader.EndOfStream ) {
                        string line = reader.ReadLine();
                        if( line == null ) break;
                        string[] fields = line.Split( ',' );
                        if( fields.Length == IPBanInfo.FieldCount ) {
                            try {
                                IPBanInfo ban;
                                if( version == 0 ) {
                                    ban = IPBanInfo.LoadOldFormat( fields, true );
                                } else {
                                    ban = IPBanInfo.Load( fields );
                                }

                                if( ban.Address.Equals( IPAddress.Any ) || ban.Address.Equals( IPAddress.None ) ) {
                                    Logger.Log( "IPBanList.Load: Invalid IP address skipped.", LogType.Warning );
                                } else {
                                    Bans.Add( ban.Address.ToString(), ban );
                                }
                            } catch( IOException ex ) {
                                Logger.Log( "IPBanList.Load: Error while trying to read from file: {0}", LogType.Error,
                                            ex.Message );
                            } catch( Exception ex ) {
                                Logger.Log( "IPBanList.Load: Could not parse a record: {0}", LogType.Error, ex.Message );
                            }
                        } else {
                            Logger.Log( "IPBanList.Load: Corrupt record skipped ({0} fields instead of {1}): {2}",
                                        LogType.Error,
                                        fields.Length, IPBanInfo.FieldCount, String.Join( ",", fields ) );
                        }
                    }

                    if( version == 0 ) {
                        Logger.Log( "IPBanList.Load: Attempting to recover IP bans...", LogType.SystemActivity );
                        int oldBanCount = Bans.Count;
                        PlayerDB.RecoverIPBans();
                        Logger.Log( "IPBanList.Load: {0} IP bans recovered.", LogType.SystemActivity,
                                    Bans.Count - oldBanCount );
                    }
                }

                Logger.Log( "IPBanList.Load: Done loading IP ban list ({0} records).", LogType.Debug, Bans.Count );
            } else {
                Logger.Log( "IPBanList.Load: No IP ban file found.", LogType.Warning );
            }
            IsLoaded = true;
        }


        static int ParseHeader( string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            if( header.IndexOf( ' ' ) > 0 ) {
                string firstPart = header.Substring( 0, header.IndexOf( ' ' ) );
                int version;
                if( Int32.TryParse( firstPart, out version ) ) {
                    return version;
                } else {
                    return 0;
                }
            } else {
                return 0;
            }
        }


        internal static void Save() {
            if( !IsLoaded ) return;
            Logger.Log( "IPBanList.Save: Saving IP ban list ({0} records).", LogType.Debug, Bans.Count );
            const string tempFile = Paths.IPBanListFileName + ".temp";

            lock( BanListLock ) {
                using( StreamWriter writer = File.CreateText( tempFile ) ) {
                    writer.WriteLine( "{0} {1}", ForamtVersion, Header );
                    foreach( IPBanInfo entry in Bans.Values ) {
                        writer.WriteLine( entry.Serialize() );
                    }
                }
            }
            try {
                Paths.MoveOrReplace( tempFile, Paths.IPBanListFileName );
            } catch( Exception ex ) {
                Logger.Log( "IPBanList.Save: An error occured while trying to save ban list file: " + ex, LogType.Error );
            }
        }


        /// <summary> Adds a new IP Ban. </summary>
        /// <param name="ban"> Ban information </param>
        /// <returns> True if ban was added, false if it was already on the list </returns>
        public static bool Add( IPBanInfo ban ) {
            if( ban == null ) throw new ArgumentNullException( "ban" );
            lock( BanListLock ) {
                if( Bans.ContainsKey( ban.Address.ToString() ) ) return false;
                if( RaiseAddingIPBanEvent( ban ) ) return false;
                Bans.Add( ban.Address.ToString(), ban );
                RaiseAddedIPBanEvent( ban );
                Save();
                return true;
            }
        }


        /// <summary> Retrieves ban information for a given IP address. </summary>
        /// <param name="address"> IP address to check. </param>
        /// <returns> IPBanInfo object if found, otherwise null. </returns>
        public static IPBanInfo Get( IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( BanListLock ) {
                if( !Bans.ContainsKey( address.ToString() ) ) {
                    return null;
                }
                return Bans[address.ToString()];
            }
        }


        // Returns true if address was banned (and was unbanned)
        // Returns false if address was not banned (and is still not banned) or if address is null
        public static bool Remove( IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( BanListLock ) {
                if( !Bans.ContainsKey( address.ToString() ) ) {
                    return false;
                }
                IPBanInfo info = Bans[address.ToString()];
                if( RaiseRemovingIPBanEvent( info ) ) return false;
                if( Bans.Remove( address.ToString() ) ) {
                    RaiseRemovedIPBanEvent( info );
                    Save();
                    return true;
                } else {
                    return false;
                }
            }
        }


        public static int Count {
            get {
                return Bans.Count;
            }
        }


        #region Events

        /// <summary> Occurs when a new IP ban is about to be added (cancellable). </summary>
        public static event EventHandler<IPBanCancellableEventArgs> AddingIPBan;


        /// <summary> Occurs when a new IP ban has been added. </summary>
        public static event EventHandler<IPBanEventArgs> AddedIPBan;


        /// <summary> Occurs when an existing IP ban is about to be removed (cancellable). </summary>
        public static event EventHandler<IPBanCancellableEventArgs> RemovingIPBan;


        /// <summary> Occurs after an existing IP ban has been removed. </summary>
        public static event EventHandler<IPBanEventArgs> RemovedIPBan;


        static bool RaiseAddingIPBanEvent( IPBanInfo info ) {
            var h = AddingIPBan;
            if( h == null ) return false;
            var e = new IPBanCancellableEventArgs( info );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseAddedIPBanEvent( IPBanInfo info ) {
            var h = AddedIPBan;
            if( h != null ) h( null, new IPBanEventArgs( info ) );
        }

        static bool RaiseRemovingIPBanEvent( IPBanInfo info ) {
            var h = RemovingIPBan;
            if( h == null ) return false;
            var e = new IPBanCancellableEventArgs( info );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseRemovedIPBanEvent( IPBanInfo info ) {
            var h = RemovedIPBan;
            if( h != null ) h( null, new IPBanEventArgs( info ) );
        }

        #endregion
    }


    public sealed class IPBanInfo {
        public const int FieldCount = 8;

        public IPAddress Address;
        public string BannedBy;
        public DateTime BanDate;
        public string BanReason;
        public string PlayerName = "";

        public int Attempts;
        public string LastAttemptName;
        public DateTime LastAttemptDate;


        IPBanInfo() { }


        public IPBanInfo( IPAddress address, string playerName, string bannedBy, string banReason ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( bannedBy == null ) throw new ArgumentNullException( "bannedBy" );
            Address = address;
            BannedBy = bannedBy;
            BanDate = DateTime.UtcNow;
            if( banReason != null ) {
                BanReason = banReason;
            }
            PlayerName = playerName;
            LastAttemptName = playerName;
            LastAttemptDate = DateTime.MinValue;
        }


        public static IPBanInfo Load( string[] fields ) {
            IPBanInfo info = new IPBanInfo();
            if( fields == null ) throw new ArgumentNullException( "fields" );
            if( fields.Length != 8 ) throw new ArgumentException( "Unexpected field count", "fields" );

            info.Address = IPAddress.Parse( fields[0] );
            info.BannedBy = PlayerInfo.Unescape( fields[1] );
            fields[2].ToDateTime( ref info.BanDate );
            if( fields[3].Length > 0 ) {
                info.BanReason = PlayerInfo.Unescape( fields[3] );
            }
            if( fields[4].Length > 0 ) {
                info.PlayerName = PlayerInfo.Unescape( fields[4] );
            }

            Int32.TryParse( fields[5], out info.Attempts );
            info.LastAttemptName = PlayerInfo.Unescape( fields[6] );
            fields[7].ToDateTime( ref info.LastAttemptDate );

            return info;
        }


        public static IPBanInfo LoadOldFormat( string[] fields, bool convertDatesToUtc ) {
            IPBanInfo info = new IPBanInfo();
            if( fields == null ) throw new ArgumentNullException( "fields" );
            if( fields.Length != 8 ) throw new ArgumentException( "Unexpected field count", "fields" );

            info.Address = IPAddress.Parse( fields[0] );
            info.BannedBy = PlayerInfo.UnescapeOldFormat( fields[1] );
            DateTimeUtil.TryParseLocalDate( fields[2], out info.BanDate );
            info.BanReason = PlayerInfo.UnescapeOldFormat( fields[3] );
            if( fields[4].Length > 1 ) {
                info.PlayerName = PlayerInfo.UnescapeOldFormat( fields[4] );
            }

            info.Attempts = Int32.Parse( fields[5] );
            info.LastAttemptName = PlayerInfo.UnescapeOldFormat( fields[6] );
            DateTimeUtil.TryParseLocalDate( fields[7], out info.LastAttemptDate );

            if( convertDatesToUtc ) {
                if( info.BanDate != DateTime.MinValue ) info.BanDate = info.BanDate.ToUniversalTime();
                if( info.LastAttemptDate != DateTime.MinValue ) info.LastAttemptDate = info.LastAttemptDate.ToUniversalTime();
            }

            return info;
        }


        public string Serialize() {
            string[] fields = new string[FieldCount];

            fields[0] = Address.ToString();
            fields[1] = PlayerInfo.Escape( BannedBy );
            fields[2] = BanDate.ToUnixTimeString();
            fields[3] = PlayerInfo.Escape( BanReason );
            fields[4] = PlayerInfo.Escape( PlayerName );
            fields[5] = Attempts.ToString();
            fields[6] = PlayerInfo.Escape( LastAttemptName );
            fields[7] = LastAttemptDate.ToUnixTimeString();

            return String.Join( ",", fields );
        }


        public string SerializeOldFormat() {
            string[] fields = new string[FieldCount];

            fields[0] = Address.ToString();
            fields[1] = PlayerInfo.EscapeOldFormat( BannedBy );
            fields[2] = BanDate.ToCompactString();
            fields[3] = PlayerInfo.EscapeOldFormat( BanReason );
            fields[4] = PlayerInfo.EscapeOldFormat( PlayerName );
            fields[5] = Attempts.ToString();
            fields[6] = PlayerInfo.EscapeOldFormat( LastAttemptName );
            if( LastAttemptDate == DateTime.MinValue ) {
                fields[7] = "";
            } else {
                fields[7] = LastAttemptDate.ToCompactString();
            }

            return String.Join( ",", fields );
        }


        public void ProcessAttempt( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Attempts++;
            LastAttemptDate = DateTime.UtcNow;
            LastAttemptName = player.Name;
        }
    }
}


namespace fCraft.Events {

    public class IPBanEventArgs : EventArgs {
        internal IPBanEventArgs( IPBanInfo info ) {
            BanInfo = info;
        }
        public IPBanInfo BanInfo { get; private set; }
    }


    public sealed class IPBanCancellableEventArgs : IPBanEventArgs, ICancellableEvent {
        internal IPBanCancellableEventArgs( IPBanInfo info ) :
            base( info ) {
        }
        public bool Cancel { get; set; }
    }

}