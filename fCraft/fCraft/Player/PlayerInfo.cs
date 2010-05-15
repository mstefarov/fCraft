// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Net;


namespace fCraft {
    public sealed class PlayerInfo {

        public const int fieldCount = 24;
        public const string DateFormat = "o";

        public string name;
        public IPAddress lastIP;
        public PlayerClass playerClass;
        public DateTime classChangeDate;
        public string classChangedBy;

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


        // === Serialization & Defaults =======================================
        // fabricate info for a player
        public PlayerInfo( string _name, PlayerClass _playerClass ){
            name = _name;
            lastIP = IPAddress.None;

            playerClass = _playerClass;
            classChangeDate = DateTime.MinValue;
            classChangedBy = "-";

            banned = false;
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
            failedLoginCount = 0;

            totalTimeOnServer = new TimeSpan( 0 );
            blocksBuilt = 0;
            blocksDeleted = 0;
            timesVisited = 1;
            linesWritten = 0;
            thanksReceived = 0;
            warningsReceived = 0;
        }


        // generate info for a new player
        public PlayerInfo( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();

            playerClass = ClassList.defaultClass;
            classChangeDate = DateTime.MinValue;
            classChangedBy = "-";

            banned = false;
            banDate = DateTime.MinValue;
            bannedBy = "-";
            unbanDate = DateTime.MinValue;
            unbannedBy = "-";
            banReason = "-";
            unbanReason = "-";

            lastFailedLoginDate = DateTime.MinValue;
            lastFailedLoginIP = IPAddress.None;
            failedLoginCount = 0;

            firstLoginDate = DateTime.Now;
            lastLoginDate = firstLoginDate;

            totalTimeOnServer = new TimeSpan( 0 );
            blocksBuilt = 0;
            blocksDeleted = 0;
            timesVisited = 1;

            linesWritten = 0;
            thanksReceived = 0;
            warningsReceived = 0;
        }


        // load info from file
        public PlayerInfo( string[] fields ) {
            name = fields[0];
            lastIP = IPAddress.Parse( fields[1] );

            playerClass = ClassList.ParseClass( fields[2] );
            if( playerClass == null ) {
                playerClass = ClassList.defaultClass;
                Logger.Log( "PlayerInfo: Could not parse class for player {0}. Setting to default ({1}).", LogType.Error, name, playerClass.name );
            }
            if( fields[3] != "-" ) classChangeDate = DateTime.Parse( fields[3] );
            else classChangeDate = DateTime.MinValue;
            classChangedBy = fields[4];

            banned = (fields[5] == "b");

            if( fields[6] != "-" ) banDate = DateTime.Parse( fields[6] );
            else banDate = DateTime.MinValue;
            bannedBy = fields[7];
            if( fields[8] != "-" ) unbanDate = DateTime.Parse( fields[8] );
            else unbanDate = DateTime.MinValue;
            unbannedBy = fields[9];
            banReason = UnEscape(fields[10]);
            unbanReason = UnEscape(fields[11]);

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
        }


        // save to file
        public string Serialize() {
            string[] fields = new string[fieldCount];

            fields[0] = name;
            fields[1] = lastIP.ToString();

            fields[2] = playerClass.name;
            if( classChangeDate == DateTime.MinValue ) fields[3] = "-";
            else fields[3] = classChangeDate.ToString( DateFormat );
            fields[4] = classChangedBy;

            if( banned ) fields[5] = "b";
            else fields[5] = "-";
            if( banDate == DateTime.MinValue ) fields[6] = "-";
            else fields[6] = banDate.ToString( DateFormat );
            fields[7] = bannedBy;
            if( unbanDate == DateTime.MinValue ) fields[8] = "-";
            else fields[8] = unbanDate.ToString( DateFormat );
            fields[9] = unbannedBy;
            fields[10] = Escape(banReason);
            fields[11] = Escape(unbanReason);

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


        public bool ProcessBan( string _bannedBy, string _banReason ) {
            if( !banned ) {
                banned = true;
                bannedBy = _bannedBy;
                banDate = DateTime.Now;
                banReason = _banReason;
                return true;
            } else {
                return false;
            }
        }
        

        public bool ProcessUnBan( string _unbannedBy, string _unbanReason ) {
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


        public void ProcessBlockBuild( byte type ) {
            if( type == 0 ) {
                blocksDeleted++;
            } else {
                blocksBuilt++;
            }
        }


        // === Utils ==========================================================

        public static string Escape( string str ) {
            return str.Replace("\\","\\\\").Replace("'","\\'");
        }

        public static string UnEscape( string str ) {
            return str.Replace( "\\'", "'" ).Replace( "\\\\", "\\" );
        }
    }
}