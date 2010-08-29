using System;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {
    class PlayerInfo2 {

        public int ID { get; private set; }
        public string Name { get; private set; }


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
            set { DB.QueuePlayerInfoUpdate( this, "ClassChangeDate", DB.ToUnixTimestamp( _classChangeDate = value ) ); }
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
            set { DB.QueuePlayerInfoUpdate( this, "BanDate", DB.ToUnixTimestamp( _banDate = value ) ); }
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
            set { DB.QueuePlayerInfoUpdate( this, "UnbanDate", DB.ToUnixTimestamp( _unbanDate = value ) ); }
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
            set { DB.QueuePlayerInfoUpdate( this, "FirstLoginDate", DB.ToUnixTimestamp( _firstLoginDate = value ) ); }
        }

        DateTime _lastLoginDate;
        public DateTime LastLoginDate {
            get { return _lastLoginDate; }
            set { DB.QueuePlayerInfoUpdate( this, "LastLoginDate", DB.ToUnixTimestamp( _lastLoginDate = value ) ); }
        }

        DateTime _firstLogoffDate;
        public DateTime FirstLogoffDate {
            get { return _firstLogoffDate; }
            set { DB.QueuePlayerInfoUpdate( this, "FirstLogoffDate", DB.ToUnixTimestamp( _firstLogoffDate = value ) ); }
        }

        #endregion


        #region Stats

        TimeSpan _totalTimeOnServer;
        public TimeSpan TotalTimeOnServer {
            get { return _totalTimeOnServer; }
            set { DB.QueuePlayerInfoUpdate( this, "TotalTimeOnServer", (int)(_totalTimeOnServer = value ).TotalSeconds ); }
        }

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
    }
}