﻿using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public class MySqlPlayerDBProviderConfig {

        public MySqlPlayerDBProviderConfig()
            : this( "localhost", 3306, "fcraft", "user", "" ) { }

        public MySqlPlayerDBProviderConfig( string host, int port, string database, string userId, string password ) {
            this.Host = host;
            this.Port = port;
            this.Database = database;
            this.UserId = userId;
            this.Password = password;
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
                if( value == null ) throw new ArgumentNullException( "database" );
                if( value.Length == 0 ) throw new ArgumentException( "Database may not be left blank." );
                database = value;
            }
        }
        string database;


        [Description( "User id. Should have CREATE, SELECT, UPDATE, INSERT, and DELETE permissions." )]
        [NotNull]
        public string UserId {
            get { return userId; }
            set {
                if( value == null ) throw new ArgumentNullException( "userId" );
                if( value.Length == 0 ) throw new ArgumentException( "UserId may not be left blank." );
                userId = value;
            }
        }
        string userId;


        [PasswordPropertyText( true )]
        [Description( "Password for the UserId." )]
        [NotNull]
        public string Password {
            get { return password; }
            set {
                if( value == null ) throw new ArgumentNullException( "password" );
                password = value;
            }
        }
        string password;


        #region Serialization

        public static string XmlRootNameDefault = "MySqlPlayerDBProviderConfig";

        [NotNull]
        public XElement Serialize() {
            return Serialize( XmlRootNameDefault );
        }

        [NotNull]
        public XElement Serialize( [NotNull] string rootName ) {
            if( rootName == null ) throw new ArgumentNullException( "rootName" );
            XElement root = new XElement( rootName );
            root.Add( new XElement( "Host", Host ) );
            root.Add( new XElement( "Port", Port ) );
            root.Add( new XElement( "Database", Database ) );
            root.Add( new XElement( "UserId", UserId ) );
            root.Add( new XElement( "Password", Password ) );
            return root;
        }

        public MySqlPlayerDBProviderConfig( [NotNull] XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            XElement hostEl = el.Element( "Host" );
            if( hostEl == null || String.IsNullOrEmpty( hostEl.Value ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No host specified in config." );
            }
            Host = hostEl.Value;

            XElement portEl = el.Element( "Port" );
            if( portEl == null || String.IsNullOrEmpty( portEl.Value ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No port specified in config." );
            }
            Port = Int32.Parse( portEl.Value );

            XElement databaseEl = el.Element( "Database" );
            if( databaseEl == null || String.IsNullOrEmpty( databaseEl.Value ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No database specified in config." );
            }
            Database = databaseEl.Value;

            XElement userIdEl = el.Element( "UserId" );
            if( userIdEl == null || String.IsNullOrEmpty( userIdEl.Value ) ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No user id specified in config." );
            }
            UserId = userIdEl.Value;

            XElement passwordEl = el.Element( "Password" );
            if( passwordEl == null || passwordEl.Value == null ) {
                throw new SerializationException( "MySqlPlayerDBProvider: No password specified in config." );
            }
            Password = passwordEl.Value;
        }

        #endregion
    }
}