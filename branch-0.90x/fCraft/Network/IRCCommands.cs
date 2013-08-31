// Part of fCraft | fCraft is copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
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

namespace fCraft {
    /// <summary> Provides methods for constructing IRC command messages. </summary>
    public static class IRCCommands {
        public static string Pass( string password ) {
            return "PASS " + password;
        }

        public static string Nick( string nickname ) {
            return "NICK " + nickname;
        }

        public static string User( string username, int userMode, string realName ) {
            return "USER " + username + " " + userMode + " * :" + realName;
        }

        public static string Oper( string name, string password ) {
            return "OPER " + name + " " + password;
        }

        public static string Privmsg( string destination, string message ) {
            return "PRIVMSG " + destination + " :" + message;
        }

        public static string Notice( string destination, string message ) {
            return "NOTICE " + destination + " :" + message;
        }

        public static string Join( string channel ) {
            return "JOIN " + channel;
        }

        public static string Join( string[] channels ) {
            string channelList = String.Join( ",", channels );
            return "JOIN " + channelList;
        }

        public static string Join( string channel, string key ) {
            return "JOIN " + channel + " " + key;
        }

        public static string Join( string[] channels, string[] keys ) {
            string channelList = String.Join( ",", channels );
            string keyList = String.Join( ",", keys );
            return "JOIN " + channelList + " " + keyList;
        }

        public static string Part( string channel ) {
            return "PART " + channel;
        }

        public static string Part( string[] channels ) {
            string channelList = String.Join( ",", channels );
            return "PART " + channelList;
        }

        public static string Part( string channel, string partMessage ) {
            return "PART " + channel + " :" + partMessage;
        }

        public static string Part( string[] channels, string partMessage ) {
            string channelList = String.Join( ",", channels );
            return "PART " + channelList + " :" + partMessage;
        }

        public static string Kick( string channel, string nickname ) {
            return "KICK " + channel + " " + nickname;
        }

        public static string Kick( string channel, string nickname, string comment ) {
            return "KICK " + channel + " " + nickname + " :" + comment;
        }

        public static string Kick( string[] channels, string nickname ) {
            string channelList = String.Join( ",", channels );
            return "KICK " + channelList + " " + nickname;
        }

        public static string Kick( string[] channels, string nickname, string comment ) {
            string channelList = String.Join( ",", channels );
            return "KICK " + channelList + " " + nickname + " :" + comment;
        }

        public static string Kick( string channel, string[] nicknames ) {
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channel + " " + nicknameList;
        }

        public static string Kick( string channel, string[] nicknames, string comment ) {
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channel + " " + nicknameList + " :" + comment;
        }

        public static string Kick( string[] channels, string[] nicknames ) {
            string channelList = String.Join( ",", channels );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channelList + " " + nicknameList;
        }

        public static string Kick( string[] channels, string[] nicknames, string comment ) {
            string channelList = String.Join( ",", channels );
            string nicknameList = String.Join( ",", nicknames );
            return "KICK " + channelList + " " + nicknameList + " :" + comment;
        }

        public static string Motd() {
            return "MOTD";
        }

        public static string Motd( string target ) {
            return "MOTD " + target;
        }

        public static string Luser() {
            return "LUSER";
        }

        public static string Luser( string mask ) {
            return "LUSER " + mask;
        }

        public static string Luser( string mask, string target ) {
            return "LUSER " + mask + " " + target;
        }

        public static string Version() {
            return "VERSION";
        }

        public static string Version( string target ) {
            return "VERSION " + target;
        }

        public static string Stats() {
            return "STATS";
        }

        public static string Stats( string query ) {
            return "STATS " + query;
        }

        public static string Stats( string query, string target ) {
            return "STATS " + query + " " + target;
        }

        public static string Links() {
            return "LINKS";
        }

        public static string Links( string serverMask ) {
            return "LINKS " + serverMask;
        }

        public static string Links( string remoteServer, string serverMask ) {
            return "LINKS " + remoteServer + " " + serverMask;
        }

        public static string Time() {
            return "TIME";
        }

        public static string Time( string target ) {
            return "TIME " + target;
        }

        public static string Connect( string targetServer, string port ) {
            return "CONNECT " + targetServer + " " + port;
        }

        public static string Connect( string targetServer, string port, string remoteServer ) {
            return "CONNECT " + targetServer + " " + port + " " + remoteServer;
        }

        public static string Trace() {
            return "TRACE";
        }

        public static string Trace( string target ) {
            return "TRACE " + target;
        }

        public static string Admin() {
            return "ADMIN";
        }

        public static string Admin( string target ) {
            return "ADMIN " + target;
        }

        public static string Info() {
            return "INFO";
        }

        public static string Info( string target ) {
            return "INFO " + target;
        }

        public static string Servlist() {
            return "SERVLIST";
        }

        public static string Servlist( string mask ) {
            return "SERVLIST " + mask;
        }

        public static string Servlist( string mask, string type ) {
            return "SERVLIST " + mask + " " + type;
        }

        public static string Squery( string serviceName, string serviceText ) {
            return "SQUERY " + serviceName + " :" + serviceText;
        }

        public static string List() {
            return "LIST";
        }

        public static string List( string channel ) {
            return "LIST " + channel;
        }

        public static string List( string[] channels ) {
            string channelList = String.Join( ",", channels );
            return "LIST " + channelList;
        }

        public static string List( string channel, string target ) {
            return "LIST " + channel + " " + target;
        }

        public static string List( string[] channels, string target ) {
            string channelList = String.Join( ",", channels );
            return "LIST " + channelList + " " + target;
        }

        public static string Names() {
            return "NAMES";
        }

        public static string Names( string channel ) {
            return "NAMES " + channel;
        }

        public static string Names( string[] channels ) {
            string channelList = String.Join( ",", channels );
            return "NAMES " + channelList;
        }

        public static string Names( string channel, string target ) {
            return "NAMES " + channel + " " + target;
        }

        public static string Names( string[] channels, string target ) {
            string channelList = String.Join( ",", channels );
            return "NAMES " + channelList + " " + target;
        }

        public static string Topic( string channel ) {
            return "TOPIC " + channel;
        }

        public static string Topic( string channel, string newTopic ) {
            return "TOPIC " + channel + " :" + newTopic;
        }

        public static string Mode( string target ) {
            return "MODE " + target;
        }

        public static string Mode( string target, string newMode ) {
            return "MODE " + target + " " + newMode;
        }

        public static string Service( string nickname, string distribution, string info ) {
            return "SERVICE " + nickname + " * " + distribution + " * * :" + info;
        }

        public static string Invite( string nickname, string channel ) {
            return "INVITE " + nickname + " " + channel;
        }

        public static string Who() {
            return "WHO";
        }

        public static string Who( string mask ) {
            return "WHO " + mask;
        }

        public static string Who( string mask, bool ircop ) {
            if( ircop ) {
                return "WHO " + mask + " o";
            } else {
                return "WHO " + mask;
            }
        }

        public static string Whois( string mask ) {
            return "WHOIS " + mask;
        }

        public static string Whois( string[] masks ) {
            string maskList = String.Join( ",", masks );
            return "WHOIS " + maskList;
        }

        public static string Whois( string target, string mask ) {
            return "WHOIS " + target + " " + mask;
        }

        public static string Whois( string target, string[] masks ) {
            string maskList = String.Join( ",", masks );
            return "WHOIS " + target + " " + maskList;
        }

        public static string Whowas( string nickname ) {
            return "WHOWAS " + nickname;
        }

        public static string Whowas( string[] nicknames ) {
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList;
        }

        public static string Whowas( string nickname, string count ) {
            return "WHOWAS " + nickname + " " + count + " ";
        }

        public static string Whowas( string[] nicknames, string count ) {
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList + " " + count + " ";
        }

        public static string Whowas( string nickname, string count, string target ) {
            return "WHOWAS " + nickname + " " + count + " " + target;
        }

        public static string Whowas( string[] nicknames, string count, string target ) {
            string nicknameList = String.Join( ",", nicknames );
            return "WHOWAS " + nicknameList + " " + count + " " + target;
        }

        public static string Kill( string nickname, string comment ) {
            return "KILL " + nickname + " :" + comment;
        }

        public static string Ping( string server ) {
            return "PING " + server;
        }

        public static string Ping( string server, string server2 ) {
            return "PING " + server + " " + server2;
        }

        public static string Pong( string server ) {
            return "PONG " + server;
        }

        public static string Pong( string server, string server2 ) {
            return "PONG " + server + " " + server2;
        }

        public static string Error( string errorMessage ) {
            return "ERROR :" + errorMessage;
        }

        public static string Away() {
            return "AWAY";
        }

        public static string Away( string awayText ) {
            return "AWAY :" + awayText;
        }

        public static string Rehash() {
            return "REHASH";
        }

        public static string Die() {
            return "DIE";
        }

        public static string Restart() {
            return "RESTART";
        }

        public static string Summon( string user ) {
            return "SUMMON " + user;
        }

        public static string Summon( string user, string target ) {
            return "SUMMON " + user + " " + target;
        }

        public static string Summon( string user, string target, string channel ) {
            return "SUMMON " + user + " " + target + " " + channel;
        }

        public static string Users() {
            return "USERS";
        }

        public static string Users( string target ) {
            return "USERS " + target;
        }

        public static string Wallops( string wallopsText ) {
            return "WALLOPS :" + wallopsText;
        }

        public static string Userhost( string nickname ) {
            return "USERHOST " + nickname;
        }

        public static string Userhost( string[] nicknames ) {
            string nicknameList = String.Join( " ", nicknames );
            return "USERHOST " + nicknameList;
        }

        public static string Ison( string nickname ) {
            return "ISON " + nickname;
        }

        public static string Ison( string[] nicknames ) {
            string nicknameList = String.Join( " ", nicknames );
            return "ISON " + nicknameList;
        }

        public static string Quit() {
            return "QUIT";
        }

        public static string Quit( string quitMessage ) {
            return "QUIT :" + quitMessage;
        }

        public static string Squit( string server, string comment ) {
            return "SQUIT " + server + " :" + comment;
        }
    }
}
