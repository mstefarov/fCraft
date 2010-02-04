using System;
using System.Text;
using System.Net;

namespace fCraft {
    class PlayerInfo {
        public string name;
        public IPAddress lastIP;
        public PlayerClass playerClass;
        public DateTime classChangeDate;
        public string classChangedBy;

        public bool banned;
        public DateTime banDate;
        public string bannedBy;

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

        // generate info for a new player
        public PlayerInfo( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();

            playerClass = PlayerClass.GetDefaultClass();
            classChangeDate = DateTime.MinValue;
            classChangedBy = "-";

            banned = false;
            banDate = DateTime.MinValue;
            bannedBy = "-";

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

            playerClass = PlayerClass.Parse( fields[2] );
            if( fields[3] != "-" ) classChangeDate = DateTime.Parse( fields[3] );
            else classChangeDate = DateTime.MinValue;
            classChangedBy = fields[4];

            banned = (fields[5] == "b");

            if( fields[6] != "-" ) banDate = DateTime.Parse( fields[6] );
            else banDate = DateTime.MinValue;
            bannedBy = fields[7];

            firstLoginDate = DateTime.Parse( fields[8] );
            lastLoginDate = DateTime.Parse( fields[9] );

            totalTimeOnServer = TimeSpan.Parse( fields[10] );
            blocksBuilt = Int32.Parse( fields[11] );
            blocksDeleted = Int32.Parse( fields[12] );
            timesVisited = Int16.Parse( fields[13] );

            linesWritten = Int32.Parse( fields[14] );
            thanksReceived = Int16.Parse( fields[15] );
            warningsReceived = Int16.Parse( fields[16] );
        }

        // save to file
        public string Serialize() {
            string[] fields = new string[17];

            fields[0] = name;
            fields[1] = lastIP.ToString();

            fields[2] = playerClass.ToString();
            if( classChangeDate == DateTime.MinValue ) fields[3] = "-";
            else fields[3] = classChangeDate.ToString( PlayerDB.DateFormat );
            fields[4] = classChangedBy;

            if( banned ) fields[5] = "b";
            else fields[5] = "-";
            if( banDate == DateTime.MinValue ) fields[6] = "-";
            else fields[6] = banDate.ToString( PlayerDB.DateFormat );
            fields[7] = bannedBy;

            fields[8] = firstLoginDate.ToString( PlayerDB.DateFormat );
            fields[9] = lastLoginDate.ToString( PlayerDB.DateFormat );

            fields[10] = totalTimeOnServer.ToString();
            fields[11] = blocksBuilt.ToString();
            fields[12] = blocksDeleted.ToString();
            fields[13] = timesVisited.ToString();

            fields[14] = linesWritten.ToString();
            fields[15] = thanksReceived.ToString();
            fields[16] = warningsReceived.ToString();

            return String.Join( ",", fields );
        }


        // === Updating =======================================================

        // update information
        public void ProcessLogin( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();
            timesVisited++;
        }

        public void ProcessLogout( Player player ) {
            totalTimeOnServer += DateTime.Now.Subtract( player.session.loginTime );
        }

        public void ProcessBlockBuild( byte type ) {
            if( type == 0 ) {
                blocksDeleted++;
            } else {
                blocksBuilt++;
            }
        }
    }
}