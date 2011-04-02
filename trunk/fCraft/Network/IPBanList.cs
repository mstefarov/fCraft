// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using fCraft.Events;

namespace fCraft {
    public static class IPBanList {

        static readonly SortedDictionary<string, IPBanInfo> Bans = new SortedDictionary<string, IPBanInfo>();
        const string Header = "IP,bannedBy,banDate,banReason,playerName,attempts,lastAttemptName,lastAttemptDate ";
        static readonly object BanListLock = new object();
        public static bool IsLoaded { get; private set; }

        internal static void Load() {
            if( File.Exists( Paths.IPBanListFileName ) ) {
                string headerText;
                using( StreamReader reader = File.OpenText( Paths.IPBanListFileName ) ) {
                    headerText = reader.ReadLine(); // header
                    while( !reader.EndOfStream ) {
                        string[] fields = reader.ReadLine().Split( ',' );
                        if( fields.Length == IPBanInfo.FieldCount ) {
                            try {
                                IPBanInfo ban = new IPBanInfo( fields );
                                if( ban.Address == IPAddress.Any || ban.Address == IPAddress.None ) {
                                    Logger.Log( "IPBanList.Load: Invalid IP address skipped.", LogType.Warning );
                                } else {
                                    Bans.Add( ban.Address.ToString(), ban );
                                }
                            } catch( IOException ex ) {
                                Logger.Log( "IPBanList.Load: Error while trying to read from file: {0}", LogType.Error, ex.Message );
                            } catch( Exception ex ) {
                                Logger.Log( "IPBanList.Load: Could not parse a record: {0}", LogType.Error, ex.Message );
                            }
                        } else {
                            Logger.Log( "IPBanList.Load: Corrupt record skipped ({0} fields instead of {1}): {2}", LogType.Error,
                                        fields.Length, IPBanInfo.FieldCount, String.Join( ",", fields ) );
                        }
                    }
                }
                if( !headerText.EndsWith( " " ) ) {
                    Logger.Log( "IPBanList.Load: Attempting to recover IP bans...", LogType.SystemActivity );
                    int oldBanCount = Bans.Count;
                    PlayerDB.RecoverIPBans();
                    Logger.Log( "IPBanList.Load: {0} IP bans recovered.", LogType.SystemActivity, Bans.Count - oldBanCount );
                }
                Logger.Log( "IPBanList.Load: Done loading IP ban list ({0} records).", LogType.Debug, Bans.Count );
            } else {
                Logger.Log( "IPBanList.Load: No IP ban file found.", LogType.Warning );
            }
            IsLoaded = true;
        }


        internal static void Save() {
            if( !IsLoaded ) return;
            Logger.Log( "IPBanList.Save: Saving IP ban list ({0} records).", LogType.Debug, Bans.Count );
            string tempFile = Paths.IPBanListFileName + ".temp";

            lock( BanListLock ) {
                using( StreamWriter writer = File.CreateText( tempFile ) ) {
                    writer.WriteLine( Header );
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


        /// <summary>Adds a new IP Ban.</summary>
        /// <param name="ban">Ban information</param>
        /// <returns>true if ban was added, false if it was already on the list</returns>
        public static bool Add( IPBanInfo ban ) {
            lock( BanListLock ) {
                if( Bans.ContainsKey( ban.Address.ToString() ) ) return false;
                if( RaiseAddingIPBanEvent( ban ) ) return false;
                Bans.Add( ban.Address.ToString(), ban );
                RaiseAddedIPBanEvent( ban );
                Save();
                return true;
            }
        }


        public static IPBanInfo Get( IPAddress address ) {
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
            lock( BanListLock ) {
                if( address == null || !Bans.ContainsKey( address.ToString() ) ) {
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

        public static int CountBans() {
            return Bans.Count;
        }



        #region Events

        public static event EventHandler<IPBanCancellableEventArgs> AddingIPBan;


        public static event EventHandler<IPBanEventArgs> AddedIPBan;


        public static event EventHandler<IPBanCancellableEventArgs> RemovingIPBan;


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

        public readonly IPAddress Address;
        public string BannedBy;
        public DateTime BanDate;
        public string BanReason;
        public string PlayerName = "";

        public short Attempts;
        public string LastAttemptName;
        public DateTime LastAttemptDate;


        public IPBanInfo( string[] fields ) {
            Address = IPAddress.Parse( fields[0] );
            BannedBy = fields[1];
            BanDate = DateTime.Parse( fields[2] );
            BanReason = PlayerInfo.Unescape( fields[3] );
            if( fields[4].Length < 2 ) {
                PlayerName = fields[4];
            }

            Attempts = Int16.Parse( fields[5] );
            LastAttemptName = fields[6];
            if( fields[7].Length > 2 ) {
                LastAttemptDate = DateTime.Parse( fields[7] );
            }
        }


        public IPBanInfo( IPAddress address, string playerName, string bannedBy, string banReason ) {
            Address = address;
            BannedBy = bannedBy;
            BanDate = DateTime.Now;
            if( banReason != null ) {
                BanReason = banReason;
            }
            PlayerName = playerName;
            LastAttemptName = playerName;
            LastAttemptDate = DateTime.MinValue;
        }


        public string Serialize() {
            string[] fields = new string[FieldCount];

            fields[0] = Address.ToString();
            fields[1] = BannedBy;
            fields[2] = BanDate.ToCompactString();
            fields[3] = PlayerInfo.Escape( BanReason );
            fields[4] = PlayerName;
            fields[5] = Attempts.ToString();
            fields[6] = LastAttemptName;
            if( LastAttemptDate == DateTime.MinValue ) {
                fields[7] = "";
            } else {
                fields[7] = LastAttemptDate.ToCompactString();
            }

            return String.Join( ",", fields );
        }


        public void ProcessAttempt( Player player ) {
            Attempts++;
            LastAttemptDate = DateTime.Now;
            LastAttemptName = player.Name;
        }
    }
}


#region EventArgs
namespace fCraft.Events {

    public class IPBanEventArgs : EventArgs {
        internal IPBanEventArgs( IPBanInfo info ) {
            BanInfo = info;
        }
        public IPBanInfo BanInfo { get; private set; }
    }


    public sealed class IPBanCancellableEventArgs : IPBanEventArgs {
        internal IPBanCancellableEventArgs( IPBanInfo info ) :
            base( info ) {
        }
        public bool Cancel { get; set; }
    }

}
#endregion