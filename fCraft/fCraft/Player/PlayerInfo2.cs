using System;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {

    class PlayerInfo2 {

        public PlayerInfo2() { }
        public PlayerInfo2( string _Name, int _ID, PlayerClass _PlayerClass ) {
            Name = _Name;
            ID = _ID;
            _playerClass = _PlayerClass;
        }


        #region Properties

        public Player PlayerObject { get; private set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public PlayerState State { get; set; }

        IPAddress _lastIP;
        public IPAddress LastIP {
            get { return _lastIP; }
            set { _lastIP = value; NeedsFlushing = true; }
        }

        PlayerClass _playerClass;
        public PlayerClass PlayerClass {
            get { return _playerClass; }
            set { _playerClass = value; NeedsFlushing = true; }
        }

        bool _banned;
        public bool Banned {
            get { return _banned; }
            set { _banned = value; NeedsFlushing = true; }
        }

        DateTime _firstLoginDate;
        public DateTime FirstLoginDate {
            get { return _firstLoginDate; }
            set { _firstLoginDate = value; NeedsFlushing = true; }
        }

        DateTime _lastLoginDate;
        public DateTime LastLoginDate {
            get { return _lastLoginDate; }
            set { _lastLoginDate = value; NeedsFlushing = true; }
        }

        DateTime _lastSeen;
        public DateTime LastSeen {
            get { return _lastSeen; }
            set { _lastSeen = value; NeedsFlushing = true; }
        }

        TimeSpan _timeOnServer;
        public TimeSpan TimeOnServer {
            get { return _timeOnServer; }
            set { _timeOnServer = value; NeedsFlushing = true; }
        }




        int _timesVisited;
        public int TimesVisited {
            get { return _timesVisited; }
            set { TimesVisited = value; NeedsFlushing = true; }
        }

        int _messagesWritten;
        public int MessagesWritten {
            get { return _messagesWritten; }
            set { _messagesWritten = value; NeedsFlushing = true; }
        }

        int _blocksPlaced;
        public int BlocksPlaced {
            get { return _blocksPlaced; }
            set { _blocksPlaced = value; NeedsFlushing = true; }
        }

        int _blocksDeleted;
        public int BlocksDeleted {
            get { return _blocksDeleted; }
            set { _blocksDeleted = value; NeedsFlushing = true; }
        }

        int _blocksDrawn;
        public int BlocksDrawn {
            get { return _blocksDrawn; }
            set { _blocksDrawn = value; NeedsFlushing = true; }
        }

        int _timesKicked;
        public int TimesKicked {
            get { return _timesKicked; }
            set { _timesKicked = value; NeedsFlushing = true; }
        }

        int _timesKickedOthers;
        public int TimesKickedOthers {
            get { return _timesKickedOthers; }
            set { _timesKickedOthers = value; NeedsFlushing = true; }
        }

        int _timesBannedOthers;
        public int TimesBannedOthers {
            get { return _timesBannedOthers; }
            set { _timesBannedOthers = value; NeedsFlushing = true; }
        }


        int _messagesWrittenLastSession;
        public int MessagesWrittenLastSession {
            get { return _messagesWrittenLastSession; }
            set { _messagesWrittenLastSession = value; NeedsFlushing = true; }
        }

        int _blocksPlacedLastSession;
        public int BlocksPlacedLastSession {
            get { return _messagesWrittenLastSession; }
            set { _messagesWrittenLastSession = value; NeedsFlushing = true; }
        }

        int _blocksDeletedLastSession;
        public int BlocksDeletedLastSession {
            get { return _blocksDeletedLastSession; }
            set { _blocksDeletedLastSession = value; NeedsFlushing = true; }
        }

        int _blocksDrawnLastSession;
        public int BlocksDrawnLastSession {
            get { return _blocksDrawnLastSession; }
            set { _blocksDrawnLastSession = value; NeedsFlushing = true; }
        }


        #endregion

        public bool NeedsFlushing { get; private set; }



        #region Processors

        object modificationLock = new object();


        public void IncrementMessagesWritten() {
            _messagesWritten++;
            _messagesWrittenLastSession++;
            NeedsFlushing = true;
        }

        public void IncrementBlocksPlaced() {
            _blocksPlaced++;
            _blocksPlacedLastSession++;
            NeedsFlushing = true;
        }

        public void IncrementBlocksDeleted() {
            _blocksDeleted++;
            _blocksDeletedLastSession++;
            NeedsFlushing = true;
        }

        public void AddBlocksDrawn( int amount ) {
            _blocksDrawn += amount;
            _blocksDrawnLastSession += amount;
            NeedsFlushing = true;
        }


        internal void ProcessLogin( Player player ) {
            lock( modificationLock ) {
                State = PlayerState.Online;
                PlayerObject = player;
                Name = player.name;
                _lastIP = player.session.GetIP();
                _lastLoginDate = DateTime.Now;
                _lastSeen = DateTime.Now;
                _timesVisited++;
            }
        }

        internal void ProcessLogout( LeaveReason leaveReason ) {
            lock( modificationLock ) {
                State = PlayerState.Offline;
                PlayerObject = null;
                TimeOnServer += DateTime.Now.Subtract( _lastLoginDate );
                _lastSeen = DateTime.Now;
            }
        }

        internal bool ProcessBan( Player banner, string reason, BanMethod method ) {
            lock( modificationLock ) {
                if( !Banned ) {
                    _banned = true;
                    return true;
                } else {
                    return false;
                }
            }
        }


        public void ProcessBanOther() {
            _timesBannedOthers++;
        }

        public void ProcessKickOther() {
            _timesKickedOthers++;
        }


        internal bool ProcessUnban( Player unbanner, string reason, UnbanMethod method ) {
            lock( modificationLock ) {
                if( Banned ) {
                    _banned = false;
                    return true;
                } else {
                    return false;
                }
            }
        }

        internal PlayerClass ProcessClassChange( PlayerClass newClass ) {
            return Interlocked.Exchange<PlayerClass>( ref _playerClass, newClass );
        }

        internal void ProcessKick( Player kicker, string reason ) {
            lock( modificationLock ) {
                _timesKicked++;
            }
        }

        #endregion

        #region Stats

        public TimeSpan GetTotalTimeOnServer() {
            if( State == PlayerState.Online ) {
                return TimeOnServer + DateTime.Now.Subtract( LastLoginDate );
            } else {
                return TimeOnServer;
            }
        }

        #endregion
    }
}