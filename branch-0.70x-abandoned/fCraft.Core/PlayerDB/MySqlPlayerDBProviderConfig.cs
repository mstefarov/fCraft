// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class MySqlPlayerDBProviderConfig {

        public MySqlPlayerDBProviderConfig()
            : this( "localhost", 3306, "fcraft", "user", "" ) { }

        public MySqlPlayerDBProviderConfig( string host, int port, string database, string userID, string password ) {
            Host = host;
            Port = port;
            Database = database;
            UserID = userID;
            Password = password;
        }


        [Description( "Hostname or IP address of MySQL server." )]
        [DefaultValue( "localhost" )]
        [NotNull]
        public string Host {
            get { return host; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value.Length == 0 ) throw new ArgumentException( "Host may not be left blank." );
                host = value;
            }
        }
        string host;


        [Description( "Port number of MySQL server." )]
        [DefaultValue( 3306 )]
        public int Port {
            get { return port; }
            set {
                if( value < 1 || value > 65535 ) {
                    throw new ArgumentOutOfRangeException( "value", "Port number must be between 1 and 65535" );
                }
                port = value;
            }
        }
        int port;


        [Description( "Name of the database to use." )]
        [DefaultValue( "fcraft" )]
        [NotNull]
        public string Database {
            get { return database; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value.Length == 0 ) throw new ArgumentException( "Database may not be left blank." );
                database = value;
            }
        }
        string database;


        [Description( "User id. Should have CREATE, SELECT, UPDATE, INSERT, and DELETE permissions." )]
        [NotNull]
        public string UserID {
            get { return userID; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value.Length == 0 ) throw new ArgumentException( "UserID may not be left blank." );
                userID = value;
            }
        }
        string userID;


        [PasswordPropertyText( true )]
        [Description( "Password for the UserID. Note: stored in plaintext in config.xml" )]
        [NotNull]
        public string Password {
            get { return password; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                password = value;
            }
        }
        string password;


        #region Serialization

        [NotNull]
        public JsonObject Serialize() {
            return new JsonObject{
                { "Host",       Host },
                { "Port",       Port },
                { "Database",   Database },
                { "UserID",     UserID },
                { "Password",   Password }
            };
        }


        public MySqlPlayerDBProviderConfig( [NotNull] JsonObject el ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            string tempString;
            if( !el.TryGetString( "Host", out tempString ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No host specified in config." );
            }
            Host = tempString;

            int tempInt;
            if( !el.TryGetInt( "Port", out tempInt ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No port specified in config." );
            }
            Port = tempInt;

            if( !el.TryGetString( "Database", out tempString ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No database specified in config." );
            }
            Database = tempString;

            if( !el.TryGetString( "UserID", out tempString ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No user id specified in config." );
            }
            UserID = tempString;

            if( !el.TryGetString( "Password", out tempString ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No password specified in config." );
            }
            Password = tempString;
        }

        #endregion
    }
}