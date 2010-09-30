// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Net;


namespace fCraft {
    public sealed class PlayerInfo {

        public const int MinFieldCount = 24,
                         MaxFieldCount = 29;
        public const string DateFormat = "o";

        public int ID;

        public string name;
        public IPAddress lastIP;
        public Rank rank;
        public DateTime rankChangeDate;
        public string rankChangedBy;

        public bool banned;
        public DateTime banDate;
        public string bannedBy;
        public DateTime unbanDate;
        public string unbannedBy;
        public string banReason;
        public string unbanReason;

        public DateTime lastFailedLoginDate;
        public IPAddress lastFailedLoginIP;
        public short failedLoginCount;
        public DateTime firstLoginDate;
        public DateTime lastLoginDate;

        public TimeSpan totalTimeOnServer;
        public int blocksBuilt;
        public int blocksDeleted;
        public short timesVisited;
        public int linesWritten;
        public short thanksReceived;
        public short warningsReceived;

        public Rank previousRank;
        public string rankChangeReason;
        public int timesKicked;
        public int timesKickedOthers;
        public int timesBannedOthers;


        // === Serialization & Defaults =======================================
        // fabricate info for a player
        public PlayerInfo( string _name, Rank _rank ) {
            name = _name;
            lastIP = IPAddress.None;

            rank = _rank;
            rankChangeDate = DateTime.MinValue;
            rankChangedBy = "-";

            //banned = false;
            banDate = DateTime.MinValue;
            bannedBy = "-";
            unbanDate = DateTime.MinValue;
            unbannedBy = "-";
            banReason = "-";
            unbanReason = "-";

            firstLoginDate = DateTime.Now;
            lastLoginDate = DateTime.Now;

            lastFailedLoginDate = DateTime.MinValue;
            lastFailedLoginIP = IPAddress.None;
            //failedLoginCount = 0;

            totalTimeOnServer = new TimeSpan( 0 );
            //blocksBuilt = 0;
            //blocksDeleted = 0;
            timesVisited = 1;

            //linesWritten = 0;
            //thanksReceived = 0;
            //warningsReceived = 0;

            previousRank = null;
        }


        // generate info for a new player
        public PlayerInfo( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();

            rank = RankList.DefaultRank;
            rankChangeDate = DateTime.MinValue;
            rankChangedBy = "-";

            //banned = false;
            banDate = DateTime.MinValue;
            bannedBy = "-";
            unbanDate = DateTime.MinValue;
            unbannedBy = "-";
            banReason = "-";
            unbanReason = "-";

            firstLoginDate = DateTime.Now;
            lastLoginDate = firstLoginDate;

            lastFailedLoginDate = DateTime.MinValue;
            lastFailedLoginIP = IPAddress.None;
            //failedLoginCount = 0;

            totalTimeOnServer = new TimeSpan( 0 );
            //blocksBuilt = 0;
            //blocksDeleted = 0;
            timesVisited = 1;

            //linesWritten = 0;
            //thanksReceived = 0;
            //warningsReceived = 0;

            previousRank = null;
        }


        // load info from file
        public PlayerInfo( string[] fields ) {
            name = fields[0];
            lastIP = IPAddress.Parse( fields[1] );

            rank = RankList.ParseRank( fields[2] );
            if( rank == null ) {
                rank = RankList.DefaultRank;
                Logger.Log( "PlayerInfo: Could not parse class for player {0}. Setting to default ({1}).", LogType.Error, name, rank.Name );
            }
            if( fields[3] != "-" ) rankChangeDate = DateTime.Parse( fields[3] );
            else rankChangeDate = DateTime.MinValue;
            rankChangedBy = fields[4];

            banned = (fields[5] == "b");

            if( fields[6] != "-" ) banDate = DateTime.Parse( fields[6] );
            else banDate = DateTime.MinValue;
            bannedBy = fields[7];
            if( fields[8] != "-" ) unbanDate = DateTime.Parse( fields[8] );
            else unbanDate = DateTime.MinValue;
            unbannedBy = fields[9];
            banReason = Unescape( fields[10] );
            unbanReason = Unescape( fields[11] );

            if( fields[12] != "-" ) lastFailedLoginDate = DateTime.Parse( fields[12] );
            else lastFailedLoginDate = DateTime.MinValue;
            if( fields[13] != "-" ) lastFailedLoginIP = IPAddress.Parse( fields[13] );
            else lastFailedLoginIP = IPAddress.None;
            failedLoginCount = Int16.Parse( fields[14] );

            firstLoginDate = DateTime.Parse( fields[15] );
            lastLoginDate = DateTime.Parse( fields[16] );
            totalTimeOnServer = TimeSpan.Parse( fields[17] );

            blocksBuilt = Int32.Parse( fields[18] );
            blocksDeleted = Int32.Parse( fields[19] );
            timesVisited = Int16.Parse( fields[20] );
            linesWritten = Int32.Parse( fields[21] );
            thanksReceived = Int16.Parse( fields[22] );
            warningsReceived = Int16.Parse( fields[23] );

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) previousRank = RankList.ParseRank( fields[24] );
                if( fields[25].Length > 0 ) rankChangeReason = Unescape( fields[25] );
                timesKicked = Int16.Parse( fields[26] );
                timesKickedOthers = Int16.Parse( fields[27] );
                timesBannedOthers = Int16.Parse( fields[28] );
            }
        }


        // save to file
        public string Serialize() {
            string[] fields = new string[MaxFieldCount];

            fields[0] = name;
            fields[1] = lastIP.ToString();

            fields[2] = rank.ToString();
            if( rankChangeDate == DateTime.MinValue ) fields[3] = "-";
            else fields[3] = rankChangeDate.ToString( DateFormat );
            fields[4] = rankChangedBy;

            if( banned ) fields[5] = "b";
            else fields[5] = "-";
            if( banDate == DateTime.MinValue ) fields[6] = "-";
            else fields[6] = banDate.ToString( DateFormat );
            fields[7] = bannedBy;
            if( unbanDate == DateTime.MinValue ) fields[8] = "-";
            else fields[8] = unbanDate.ToString( DateFormat );
            fields[9] = unbannedBy;
            fields[10] = Escape( banReason );
            fields[11] = Escape( unbanReason );

            if( lastFailedLoginDate == DateTime.MinValue ) fields[12] = "-";
            else fields[12] = lastFailedLoginDate.ToString( DateFormat );
            if( lastFailedLoginIP == IPAddress.None ) fields[13] = "-";
            else fields[13] = lastFailedLoginIP.ToString();
            fields[14] = failedLoginCount.ToString();

            fields[15] = firstLoginDate.ToString( DateFormat );
            fields[16] = lastLoginDate.ToString( DateFormat );
            fields[17] = totalTimeOnServer.ToString();

            fields[18] = blocksBuilt.ToString();
            fields[19] = blocksDeleted.ToString();
            fields[20] = timesVisited.ToString();
            fields[21] = linesWritten.ToString();
            fields[22] = thanksReceived.ToString();
            fields[23] = warningsReceived.ToString();

            if( previousRank != null ) fields[24] = previousRank.ToString();
            else fields[24] = "";
            if( rankChangeReason != null ) fields[25] = Escape( rankChangeReason );
            else fields[25] = "";
            fields[26] = timesKicked.ToString();
            fields[27] = timesKickedOthers.ToString();
            fields[28] = timesBannedOthers.ToString();
            return String.Join( ",", fields );
        }


        // === Updating =======================================================

        // update information
        public void ProcessLogin( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();
            lastLoginDate = DateTime.Now;
            timesVisited++;
        }


        public void ProcessFailedLogin( Player player ) {
            lastFailedLoginDate = DateTime.Now;
            lastFailedLoginIP = player.session.GetIP();
            failedLoginCount++;
        }


        public void ProcessLogout( Player player ) {
            totalTimeOnServer += DateTime.Now.Subtract( player.session.loginTime );
        }


        public bool ProcessBan( Player _bannedBy, string _banReason ) {
            if( !banned ) {
                banned = true;
                bannedBy = _bannedBy.name;
                banDate = DateTime.Now;
                banReason = _banReason;
                _bannedBy.info.timesBannedOthers++;
                return true;
            } else {
                return false;
            }
        }



        public bool ProcessUnban( string _unbannedBy, string _unbanReason ) {
            if( banned ) {
                banned = false;
                unbannedBy = _unbannedBy;
                unbanDate = DateTime.Now;
                unbanReason = _unbanReason;
                return true;
            } else {
                return false;
            }
        }


        public void ProcessClassChange( Rank newClass, Player changer, string reason ) {
            previousRank = rank;
            rank = newClass;
            rankChangeDate = DateTime.Now;
            rankChangedBy = changer.name;
            rankChangeReason = reason;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                blocksDeleted++;
            } else {
                blocksBuilt++;
            }
        }


        public void ProcessKick( Player kickedBy ) {
            timesKicked++;
            kickedBy.info.timesKickedOthers++;
        }


        // === Utils ==========================================================

        public static string Escape( string str ) {
            return str.Replace( "\\", "\\\\" ).Replace( "'", "\\'" ).Replace( ',', '\xFF' );
        }

        public static string Unescape( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( "\\'", "'" ).Replace( "\\\\", "\\" );
        }

        public string GetClassyName() {
            string displayedName = name;
            if( Config.GetBool( ConfigKey.RankPrefixesInChat ) ) {
                displayedName = rank.Prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.RankColorsInChat ) ) {
                displayedName = rank.Color + displayedName;
                if( name == "fragmer" ) return "&4f&cr&ea&ag&bm&9e&5r";
                if( name == "Kirshi" ) return "&bKir&dshi";
            }
            return displayedName;
        }


        public static string PlayerArrayToString( PlayerInfo[] list ) {
            bool first = true;
            StringBuilder sb = new StringBuilder();
            for( int i = 0; i < list.Length; i++ ) {
                if( !first ) sb.Append( ", " );
                sb.Append( list[i].GetClassyName() );
            }
            return sb.ToString();
        }
    }
}