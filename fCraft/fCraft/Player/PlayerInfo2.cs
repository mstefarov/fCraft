using System;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {

    enum LeaveReason {
        Normal = 0,
        Error = 1,
        Kick = 2,
        AFKKick = 3,
        AntiGriefKick = 4,
        InvalidMessageKick = 5,
        InvalidSetTileKick = 6,
        InvalidOpcodeKick = 7,
        AntiBlockSpamKick = 8,
        AntiMessageSpamKick = 9,
        AntiMovementSpamKick = 10,
        LeavingMapKick = 11,
        Ban = 12,
        BanIP = 13,
        BanAll = 14,
        ServerShutdown = 15
    }

    class PlayerInfo2 {

        public Player PlayerObject { get; private set; }

        public int ID { get; private set; }
        public string Name { get; private set; }

        public bool Online {
            get { return PlayerObject != null; }
        }

        IPAddress _lastIP;
        public IPAddress LastIP {
            get { return _lastIP; }
            set { DB.QueuePlayerInfoUpdate( this, "LastIP", DB.IPAddressToInt32( _lastIP = value ) ); }
        }


        #region Class

        PlayerClass _playerClass;
        public PlayerClass PlayerClass {
            get { return _playerClass; }
            set { DB.QueuePlayerInfoUpdate( this, "PlayerClass", (_playerClass = value).index ); }
        }

        DateTime _classChangeDate;
        public DateTime ClassChangeDate {
            get { return _classChangeDate; }
            set { DB.QueuePlayerInfoUpdate( this, "ClassChangeDate", DB.DateTimeToTimestamp( _classChangeDate = value ) ); }
        }

        string _classChangedBy;
        public string ClassChangedBy {
            get { return _classChangedBy; }
            set { DB.QueuePlayerInfoUpdate( this, "ClassChangedBy", _classChangedBy = value ); }
        }


        PlayerClass _previousClass;
        public PlayerClass PreviousClass {
            get { return _previousClass; }
            set { DB.QueuePlayerInfoUpdate( this, "PreviousClass", (_previousClass = value).index ); }
        }

        string _classChangeReason;
        public string ClassChangeReason {
            get { return _classChangeReason; }
            set { DB.QueuePlayerInfoUpdate( this, "ClassChangeReason", _classChangeReason = value ); }
        }

        #endregion


        #region Bans

        bool _banned;
        public bool Banned {
            get { return _banned; }
            set { DB.QueuePlayerInfoUpdate( this, "Banned", (_banned = value) ? 1 : 0 ); }
        }



        DateTime _banDate;
        public DateTime BanDate {
            get { return _banDate; }
            set { DB.QueuePlayerInfoUpdate( this, "BanDate", DB.DateTimeToTimestamp( _banDate = value ) ); }
        }

        string _bannedBy;
        public string BannedBy {
            get { return _bannedBy; }
            set { DB.QueuePlayerInfoUpdate( this, "BannedBy", _bannedBy = value ); }
        }

        string _banReason;
        public string BanReason {
            get { return _banReason; }
            set { DB.QueuePlayerInfoUpdate( this, "BanReason", _banReason = value ); }
        }



        DateTime _unbanDate;
        public DateTime UnbanDate {
            get { return _unbanDate; }
            set { DB.QueuePlayerInfoUpdate( this, "UnbanDate", DB.DateTimeToTimestamp( _unbanDate = value ) ); }
        }

        string _unbannedBy;
        public string UnbannedBy {
            get { return _unbannedBy; }
            set { DB.QueuePlayerInfoUpdate( this, "UnbannedBy", _unbannedBy = value ); }
        }

        string _unbanReason;
        public string UnbanReason {
            get { return _unbanReason; }
            set { DB.QueuePlayerInfoUpdate( this, "UnbanReason", _unbanReason = value ); }
        }

        #endregion


        #region Dates

        DateTime _firstLoginDate;
        public DateTime FirstLoginDate {
            get { return _firstLoginDate; }
            set { DB.QueuePlayerInfoUpdate( this, "FirstLoginDate", DB.DateTimeToTimestamp( _firstLoginDate = value ) ); }
        }

        DateTime _lastLoginDate;
        public DateTime LastLoginDate {
            get { return _lastLoginDate; }
            set { DB.QueuePlayerInfoUpdate( this, "LastLoginDate", DB.DateTimeToTimestamp( _lastLoginDate = value ) ); }
        }

        DateTime _lastSeen;
        public DateTime LastSeen {
            get { return _lastSeen; }
            set { DB.QueuePlayerInfoUpdate( this, "LastSeen", DB.DateTimeToTimestamp( _lastSeen = value ) ); }
        }

        TimeSpan _previousTimeOnServer;
        public TimeSpan TotalTimeOnServer {
            get{
                if( Online ) {
                    return _previousTimeOnServer + DateTime.Now.Subtract( LastLoginDate );
                } else {
                    return _previousTimeOnServer;
                }
            }
        }


        public TimeSpan LastSessionDuration { get; private set; }
        public LeaveReason LastLeaveReason { get; private set; }

        #endregion


        #region Stats

        int _timesVisited;
        public int TimesVisited {
            get { return _timesVisited; }
            set { DB.QueuePlayerInfoUpdate( this, "TimesVisited", _timesVisited = value ); }
        }

        int _messagesWritten;
        public int MessagesWritten {
            get { return _messagesWritten; }
            set { DB.QueuePlayerInfoUpdate( this, "MessagesWritten", _messagesWritten = value ); }
        }

        int _blocksPlaced;
        public int BlocksPlaced {
            get { return _blocksPlaced; }
            set { DB.QueuePlayerInfoUpdate( this, "BlocksPlaced", _blocksPlaced = value ); }
        }

        int _blocksDeleted;
        public int BlocksDeleted {
            get { return _blocksDeleted; }
            set { DB.QueuePlayerInfoUpdate( this, "BlocksDeleted", _blocksDeleted = value ); }
        }

        int _blocksDrawn;
        public int BlocksDrawn {
            get { return _blocksDrawn; }
            set { DB.QueuePlayerInfoUpdate( this, "BlocksDrawn", _blocksDrawn = value ); }
        }

        int _timesKicked;
        public int TimesKicked {
            get { return _timesKicked; }
            set { DB.QueuePlayerInfoUpdate( this, "TimesKicked", _timesKicked = value ); }
        }

        int _timesKickedOthers;
        public int TimesKickedOthers {
            get { return _timesKickedOthers; }
            set { DB.QueuePlayerInfoUpdate( this, "TimesKickedOthers", _timesKickedOthers = value ); }
        }

        int _timesBannedOthers;
        public int TimesBannedOthers {
            get { return _timesBannedOthers; }
            set { DB.QueuePlayerInfoUpdate( this, "TimesBannedOthers", _timesBannedOthers = value ); }
        }

        #endregion


        #region Session Stats

        public int MessagesWrittenLastSession { get; private set; }
        public int BlocksPlacedLastSession { get; private set; }
        public int BlocksDeletedLastSession { get; private set; }
        public int BlocksDrawnLastSession { get; private set; }

        #endregion


        bool _needsFlushing;
        public bool NeedsFlushing{
            get { return _needsFlushing; }
        }

        public void IncrementBlocksPlaced() {
            Interlocked.Increment( ref _blocksPlaced );
            _needsFlushing = true;
        }

        public void IncrementBlocksDeleted() {
            Interlocked.Increment( ref _blocksDeleted );
            _needsFlushing = true;
        }

        public void AddBlocksDrawn( int amount ) {
            Interlocked.Add( ref _blocksDrawn, amount );
            _needsFlushing = true;
        }

        #region Processors

        public void ProcessLogin( Player player ) {
            PlayerObject = player;
            Name = player.name;
            _lastIP = player.session.GetIP();
            _lastLoginDate = DateTime.Now;
            _lastSeen = DateTime.Now;
            _timesVisited++;
            DB.ProcessLogin( this );
        }

        public void ProcessLogout( LeaveReason leaveReason ) {
            LastLeaveReason = leaveReason;
            LastSessionDuration = DateTime.Now.Subtract( _lastLoginDate );
            PlayerObject = null;
            _previousTimeOnServer += LastSessionDuration;
            _lastSeen = DateTime.Now;
            DB.ProcessLogout( this );
        }

        #endregion
    }
}