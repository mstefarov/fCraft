// Part of fCraft | fCraft is copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

#pragma warning disable 1591

/* Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
using System;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides methods for constructing IRC command messages. </summary>
    public static class IRCCommands {
        [NotNull]
        public static string Pass( [NotNull] string password ) {
            if( password == null ) throw new ArgumentNullException( "password" );
            return "PASS " + password;
        }

        [NotNull]
        public static string Nick( [NotNull] string nickname ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            return "NICK " + nickname;
        }

        [NotNull]
        public static string User( [NotNull] string username, int userMode, [NotNull] string realName ) {
            if( username == null ) throw new ArgumentNullException( "username" );
            if( realName == null ) throw new ArgumentNullException( "realName" );
            return "USER " + username + " " + userMode + " * :" + realName;
        }

        [NotNull]
        public static string Oper( [NotNull] string name, [NotNull] string password ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( password == null ) throw new ArgumentNullException( "password" );
            return "OPER " + name + " " + password;
        }

        [NotNull]
        public static string Privmsg( [NotNull] string destination, [NotNull] string message ) {
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( message == null ) throw new ArgumentNullException( "message" );
            return "PRIVMSG " + destination + " :" + message;
        }

        [NotNull]
        public static string Notice( [NotNull] string destination, [NotNull] string message ) {
            if( destination == null ) throw new ArgumentNullException( "destination" );
            if( message == null ) throw new ArgumentNullException( "message" );
            return "NOTICE " + destination + " :" + message;
        }

        [NotNull]
        public static string Join( [NotNull] string channel ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "JOIN " + channel;
        }

        [NotNull]
        public static string Join( [NotNull] string[] channels ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            string channelList = String.Join( ",", channels );
            return "JOIN " + channelList;
        }

        [NotNull]
        public static string Join( [NotNull] string channel, [NotNull] string key ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( key == null ) throw new ArgumentNullException( "key" );
            return "JOIN " + channel + " " + key;
        }

        [NotNull]
        public static string Join( [NotNull] string[] channels, [NotNull] string[] keys ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( keys == null ) throw new ArgumentNullException( "keys" );
            string channelList = String.Join( ",", channels );
            string keyList = String.Join( ",", keys );
            return "JOIN " + channelList + " " + keyList;
        }

        [NotNull]
        public static string Part( [NotNull] string channel ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "PART " + channel;
        }

        [NotNull]
        public static string Part( [NotNull] string[] channels ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            string channelList = String.Join( ",", channels );
            return "PART " + channelList;
        }

        [NotNull]
        public static string Part( [NotNull] string channel, [NotNull] string partMessage ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( partMessage == null ) throw new ArgumentNullException( "partMessage" );
            return "PART " + channel + " :" + partMessage;
        }

        [NotNull]
        public static string Part( [NotNull] string[] channels, [NotNull] string partMessage ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( partMessage == null ) throw new ArgumentNullException( "partMessage" );
            string channelList = String.Join( ",", channels );
            return "PART " + channelList + " :" + partMessage;
        }

        [NotNull]
        public static string Kick( [NotNull] string channel, [NotNull] string nickname ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            return "KICK " + channel + " " + nickname;
        }

        [NotNull]
        public static string Kick( [NotNull] string channel, [NotNull] string nickname, [NotNull] string comment ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            return "KICK " + channel + " " + nickname + " :" + comment;
        }

        [NotNull]
        public static string Kick( [NotNull] string[] channels, [NotNull] string nickname ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            string channelList = String.Join( ",", channels );
            return "KICK " + channelList + " " + nickname;
        }

        [NotNull]
        public static string Kick( [NotNull] string[] channels, [NotNull] string nickname, [NotNull] string comment ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            string channelList = String.Join( ",", channels );
            return "KICK " + channelList + " " + nickname + " :" + comment;
        }

        [NotNull]
        public static string Kick( [NotNull] string channel, [NotNull] string[] nicknames ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channel + " " + nicknameList;
        }

        [NotNull]
        public static string Kick( [NotNull] string channel, [NotNull] string[] nicknames, [NotNull] string comment ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channel + " " + nicknameList + " :" + comment;
        }

        [NotNull]
        public static string Kick( [NotNull] string[] channels, [NotNull] string[] nicknames ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            string channelList = String.Join( ",", channels );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channelList + " " + nicknameList;
        }

        [NotNull]
        public static string Kick( [NotNull] string[] channels, [NotNull] string[] nicknames, [NotNull] string comment ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            string channelList = String.Join( ",", channels );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channelList + " " + nicknameList + " :" + comment;
        }

        [NotNull]
        public static string Motd() {
            return "MOTD";
        }

        [NotNull]
        public static string Motd( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "MOTD " + target;
        }

        [NotNull]
        public static string Luser() {
            return "LUSER";
        }

        [NotNull]
        public static string Luser( [NotNull] string mask ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            return "LUSER " + mask;
        }

        [NotNull]
        public static string Luser( [NotNull] string mask, [NotNull] string target ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "LUSER " + mask + " " + target;
        }

        [NotNull]
        public static string Version() {
            return "VERSION";
        }

        [NotNull]
        public static string Version( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "VERSION " + target;
        }

        [NotNull]
        public static string Stats() {
            return "STATS";
        }

        [NotNull]
        public static string Stats( [NotNull] string query ) {
            if( query == null ) throw new ArgumentNullException( "query" );
            return "STATS " + query;
        }

        [NotNull]
        public static string Stats( [NotNull] string query, [NotNull] string target ) {
            if( query == null ) throw new ArgumentNullException( "query" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "STATS " + query + " " + target;
        }

        [NotNull]
        public static string Links() {
            return "LINKS";
        }

        [NotNull]
        public static string Links( [NotNull] string serverMask ) {
            if( serverMask == null ) throw new ArgumentNullException( "serverMask" );
            return "LINKS " + serverMask;
        }

        [NotNull]
        public static string Links( [NotNull] string remoteServer, [NotNull] string serverMask ) {
            if( remoteServer == null ) throw new ArgumentNullException( "remoteServer" );
            if( serverMask == null ) throw new ArgumentNullException( "serverMask" );
            return "LINKS " + remoteServer + " " + serverMask;
        }

        [NotNull]
        public static string Time() {
            return "TIME";
        }

        [NotNull]
        public static string Time( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "TIME " + target;
        }

        [NotNull]
        public static string Connect( [NotNull] string targetServer, [NotNull] string port ) {
            if( targetServer == null ) throw new ArgumentNullException( "targetServer" );
            if( port == null ) throw new ArgumentNullException( "port" );
            return "CONNECT " + targetServer + " " + port;
        }

        [NotNull]
        public static string Connect( [NotNull] string targetServer, [NotNull] string port,
                                      [NotNull] string remoteServer ) {
            if( targetServer == null ) throw new ArgumentNullException( "targetServer" );
            if( port == null ) throw new ArgumentNullException( "port" );
            if( remoteServer == null ) throw new ArgumentNullException( "remoteServer" );
            return "CONNECT " + targetServer + " " + port + " " + remoteServer;
        }

        [NotNull]
        public static string Trace() {
            return "TRACE";
        }

        [NotNull]
        public static string Trace( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "TRACE " + target;
        }

        [NotNull]
        public static string Admin() {
            return "ADMIN";
        }

        [NotNull]
        public static string Admin( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "ADMIN " + target;
        }

        [NotNull]
        public static string Info() {
            return "INFO";
        }

        [NotNull]
        public static string Info( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "INFO " + target;
        }

        [NotNull]
        public static string Servlist() {
            return "SERVLIST";
        }

        [NotNull]
        public static string Servlist( [NotNull] string mask ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            return "SERVLIST " + mask;
        }

        [NotNull]
        public static string Servlist( [NotNull] string mask, [NotNull] string type ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            if( type == null ) throw new ArgumentNullException( "type" );
            return "SERVLIST " + mask + " " + type;
        }

        [NotNull]
        public static string Squery( [NotNull] string serviceName, [NotNull] string serviceText ) {
            if( serviceName == null ) throw new ArgumentNullException( "serviceName" );
            if( serviceText == null ) throw new ArgumentNullException( "serviceText" );
            return "SQUERY " + serviceName + " :" + serviceText;
        }

        [NotNull]
        public static string List() {
            return "LIST";
        }

        [NotNull]
        public static string List( [NotNull] string channel ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "LIST " + channel;
        }

        [NotNull]
        public static string List( [NotNull] string[] channels ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            string channelList = String.Join( ",", channels );
            return "LIST " + channelList;
        }

        [NotNull]
        public static string List( [NotNull] string channel, [NotNull] string target ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "LIST " + channel + " " + target;
        }

        [NotNull]
        public static string List( [NotNull] string[] channels, [NotNull] string target ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( target == null ) throw new ArgumentNullException( "target" );
            string channelList = String.Join( ",", channels );
            return "LIST " + channelList + " " + target;
        }

        [NotNull]
        public static string Names() {
            return "NAMES";
        }

        [NotNull]
        public static string Names( [NotNull] string channel ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "NAMES " + channel;
        }

        [NotNull]
        public static string Names( [NotNull] string[] channels ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            string channelList = String.Join( ",", channels );
            return "NAMES " + channelList;
        }

        [NotNull]
        public static string Names( [NotNull] string channel, [NotNull] string target ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "NAMES " + channel + " " + target;
        }

        [NotNull]
        public static string Names( [NotNull] string[] channels, [NotNull] string target ) {
            if( channels == null ) throw new ArgumentNullException( "channels" );
            if( target == null ) throw new ArgumentNullException( "target" );
            string channelList = String.Join( ",", channels );
            return "NAMES " + channelList + " " + target;
        }

        [NotNull]
        public static string Topic( [NotNull] string channel ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "TOPIC " + channel;
        }

        [NotNull]
        public static string Topic( [NotNull] string channel, [NotNull] string newTopic ) {
            if( channel == null ) throw new ArgumentNullException( "channel" );
            if( newTopic == null ) throw new ArgumentNullException( "newTopic" );
            return "TOPIC " + channel + " :" + newTopic;
        }

        [NotNull]
        public static string Mode( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "MODE " + target;
        }

        [NotNull]
        public static string Mode( [NotNull] string target, [NotNull] string newMode ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            if( newMode == null ) throw new ArgumentNullException( "newMode" );
            return "MODE " + target + " " + newMode;
        }

        [NotNull]
        public static string Service( [NotNull] string nickname, [NotNull] string distribution, [NotNull] string info ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( distribution == null ) throw new ArgumentNullException( "distribution" );
            if( info == null ) throw new ArgumentNullException( "info" );
            return "SERVICE " + nickname + " * " + distribution + " * * :" + info;
        }

        [NotNull]
        public static string Invite( [NotNull] string nickname, [NotNull] string channel ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "INVITE " + nickname + " " + channel;
        }

        [NotNull]
        public static string Who() {
            return "WHO";
        }

        [NotNull]
        public static string Who( [NotNull] string mask ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            return "WHO " + mask;
        }

        [NotNull]
        public static string Who( [NotNull] string mask, bool ircop ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            if( ircop ) {
                return "WHO " + mask + " o";
            } else {
                return "WHO " + mask;
            }
        }

        [NotNull]
        public static string Whois( [NotNull] string mask ) {
            if( mask == null ) throw new ArgumentNullException( "mask" );
            return "WHOIS " + mask;
        }

        [NotNull]
        public static string Whois( [NotNull] string[] masks ) {
            if( masks == null ) throw new ArgumentNullException( "masks" );
            string maskList = String.Join( ",", masks );
            return "WHOIS " + maskList;
        }

        [NotNull]
        public static string Whois( [NotNull] string target, [NotNull] string mask ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            if( mask == null ) throw new ArgumentNullException( "mask" );
            return "WHOIS " + target + " " + mask;
        }

        [NotNull]
        public static string Whois( [NotNull] string target, [NotNull] string[] masks ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            if( masks == null ) throw new ArgumentNullException( "masks" );
            string maskList = String.Join( ",", masks );
            return "WHOIS " + target + " " + maskList;
        }

        [NotNull]
        public static string Whowas( [NotNull] string nickname ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            return "WHOWAS " + nickname;
        }

        [NotNull]
        public static string Whowas( [NotNull] string[] nicknames ) {
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList;
        }

        [NotNull]
        public static string Whowas( [NotNull] string nickname, [NotNull] string count ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( count == null ) throw new ArgumentNullException( "count" );
            return "WHOWAS " + nickname + " " + count + " ";
        }

        [NotNull]
        public static string Whowas( [NotNull] string[] nicknames, [NotNull] string count ) {
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            if( count == null ) throw new ArgumentNullException( "count" );
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList + " " + count + " ";
        }

        [NotNull]
        public static string Whowas( [NotNull] string nickname, [NotNull] string count, [NotNull] string target ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( count == null ) throw new ArgumentNullException( "count" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "WHOWAS " + nickname + " " + count + " " + target;
        }

        [NotNull]
        public static string Whowas( [NotNull] string[] nicknames, [NotNull] string count, [NotNull] string target ) {
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            if( count == null ) throw new ArgumentNullException( "count" );
            if( target == null ) throw new ArgumentNullException( "target" );
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList + " " + count + " " + target;
        }

        [NotNull]
        public static string Kill( [NotNull] string nickname, [NotNull] string comment ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            return "KILL " + nickname + " :" + comment;
        }

        [NotNull]
        public static string Ping( [NotNull] string server ) {
            if( server == null ) throw new ArgumentNullException( "server" );
            return "PING " + server;
        }

        [NotNull]
        public static string Ping( [NotNull] string server, [NotNull] string server2 ) {
            if( server == null ) throw new ArgumentNullException( "server" );
            if( server2 == null ) throw new ArgumentNullException( "server2" );
            return "PING " + server + " " + server2;
        }

        [NotNull]
        public static string Pong( [NotNull] string server ) {
            if( server == null ) throw new ArgumentNullException( "server" );
            return "PONG " + server;
        }

        [NotNull]
        public static string Pong( [NotNull] string server, [NotNull] string server2 ) {
            if( server == null ) throw new ArgumentNullException( "server" );
            if( server2 == null ) throw new ArgumentNullException( "server2" );
            return "PONG " + server + " " + server2;
        }

        [NotNull]
        public static string Error( [NotNull] string errorMessage ) {
            if( errorMessage == null ) throw new ArgumentNullException( "errorMessage" );
            return "ERROR :" + errorMessage;
        }

        [NotNull]
        public static string Away() {
            return "AWAY";
        }

        [NotNull]
        public static string Away( [NotNull] string awayText ) {
            if( awayText == null ) throw new ArgumentNullException( "awayText" );
            return "AWAY :" + awayText;
        }

        [NotNull]
        public static string Rehash() {
            return "REHASH";
        }

        [NotNull]
        public static string Die() {
            return "DIE";
        }

        [NotNull]
        public static string Restart() {
            return "RESTART";
        }

        [NotNull]
        public static string Summon( [NotNull] string user ) {
            if( user == null ) throw new ArgumentNullException( "user" );
            return "SUMMON " + user;
        }

        [NotNull]
        public static string Summon( [NotNull] string user, [NotNull] string target ) {
            if( user == null ) throw new ArgumentNullException( "user" );
            if( target == null ) throw new ArgumentNullException( "target" );
            return "SUMMON " + user + " " + target;
        }

        [NotNull]
        public static string Summon( [NotNull] string user, [NotNull] string target, [NotNull] string channel ) {
            if( user == null ) throw new ArgumentNullException( "user" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( channel == null ) throw new ArgumentNullException( "channel" );
            return "SUMMON " + user + " " + target + " " + channel;
        }

        [NotNull]
        public static string Users() {
            return "USERS";
        }

        [NotNull]
        public static string Users( [NotNull] string target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            return "USERS " + target;
        }

        [NotNull]
        public static string Wallops( [NotNull] string wallopsText ) {
            if( wallopsText == null ) throw new ArgumentNullException( "wallopsText" );
            return "WALLOPS :" + wallopsText;
        }

        [NotNull]
        public static string Userhost( [NotNull] string nickname ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            return "USERHOST " + nickname;
        }

        [NotNull]
        public static string Userhost( [NotNull] string[] nicknames ) {
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            string nicknameList = String.Join( " ", nicknames );
            return "USERHOST " + nicknameList;
        }

        [NotNull]
        public static string Ison( [NotNull] string nickname ) {
            if( nickname == null ) throw new ArgumentNullException( "nickname" );
            return "ISON " + nickname;
        }

        [NotNull]
        public static string Ison( [NotNull] string[] nicknames ) {
            if( nicknames == null ) throw new ArgumentNullException( "nicknames" );
            string nicknameList = String.Join( " ", nicknames );
            return "ISON " + nicknameList;
        }

        [NotNull]
        public static string Quit() {
            return "QUIT";
        }

        [NotNull]
        public static string Quit( [NotNull] string quitMessage ) {
            if( quitMessage == null ) throw new ArgumentNullException( "quitMessage" );
            return "QUIT :" + quitMessage;
        }

        [NotNull]
        public static string Squit( [NotNull] string server, [NotNull] string comment ) {
            if( server == null ) throw new ArgumentNullException( "server" );
            if( comment == null ) throw new ArgumentNullException( "comment" );
            return "SQUIT " + server + " :" + comment;
        }
    }
}
