// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using fCraft;

namespace ConfigTool {
    partial class ConfigUI {

        void FillToolTipsGeneral() {

            string tipServerName =
@"The name of the server, as shown on the welcome screen
and the official server list (if server is public).";
            toolTip.SetToolTip( lServerName, tipServerName );
            toolTip.SetToolTip( tServerName, tipServerName );

            string tipMOTD =
@"MOTD (Message Of The Day) is a message shown to
connecting players right under the server name.
It can be left blank.";
            toolTip.SetToolTip( lMOTD, tipMOTD );
            toolTip.SetToolTip( tMOTD, tipMOTD );



            string tipPublic =
@"Public servers are listed on minecraft.net server list, so expect
random players to join. Private servers can only be joined by players
who already know the server port/address or URL. Note that the URL
changes if your computer's IP or server's port change.";
            toolTip.SetToolTip( lPublic, tipPublic );
            toolTip.SetToolTip( cPublic, tipPublic );

            string tipMaxPlayers =
@"Maximum number of players on the server. Having more players
uses more RAM and more bandwidth. If a player's rank is given a
""reserved slot"" on the server, they can join even if server is full.
Minecraft protocol limits total number of players to 128.";
            toolTip.SetToolTip( lMaxPlayers, tipMaxPlayers );
            toolTip.SetToolTip( nMaxPlayers, tipMaxPlayers );


            string tipUploadBandwidth =
@"Total available upload bandwidth, in kilobytes.
This number is used to pace drawing commands
to prevent server from overwhelming the Internet
connection with data.";
            toolTip.SetToolTip( nUploadBandwidth, tipUploadBandwidth );
            toolTip.SetToolTip( lUploadBandwidth, tipUploadBandwidth );

            toolTip.SetToolTip( bMeasure,
@"Test your connection\'s upload speed with speedtest.net
Note: to convert from megabits to kilobytes, multiply the
number by 128" );

            string tipDefaultRank =
@"New players will be assigned this rank by default.
It\'s generally a good idea not to give new players
many powers until they prove themselves trustworthy.";
            toolTip.SetToolTip( lDefaultRank, tipDefaultRank );
            toolTip.SetToolTip( cDefaultRank, tipDefaultRank );

            string tipPort =
@"Port number on your local machine that fCraft uses to listen for
incoming connections. If you are behind a router, you may need
to set up port forwarding. You may also need to add a firewall 
exception for fCraftUI/fCraftConsole/ConfigTool.  Note that your
server's URL will change if you change the port number.";
            toolTip.SetToolTip( nPort, tipPort );
            toolTip.SetToolTip( lPort, tipPort );

            toolTip.SetToolTip( bPortCheck,
@"Check if the selected port is connectible.
If port check fails, you may need to set up
port forwarding on your router." );

            string tipIP =
@"If the machine has more than one available IP address
(for example if you have more than one NIC) you can
use this setting to make fCraft bind to the same IP
every time.";
            toolTip.SetToolTip( xIP, tipIP );
            toolTip.SetToolTip( tIP, tipIP );



            string tipColorSys = "This is the color of normal system messages. Default is yellow.";
            toolTip.SetToolTip( bColorSys, tipColorSys );
            toolTip.SetToolTip( lColorSys, tipColorSys );

            string tipColorHelp = "Color of command usage examples in help. Default is lime-green.";
            toolTip.SetToolTip( bColorHelp, tipColorHelp );
            toolTip.SetToolTip( lColorHelp, tipColorHelp );

            string tipColorSay = "Color of messages produced by \"/say\" command. Default is dark-green.";
            toolTip.SetToolTip( bColorSay, tipColorSay );
            toolTip.SetToolTip( lColorSay, tipColorSay );

            string tipColorAnnouncement =
@"Color of announcements and rules. Default is dark-green.
Note that this default color can be overriden by
colorcodes in announcement and rule files.";
            toolTip.SetToolTip( bColorAnnouncement, tipColorAnnouncement );
            toolTip.SetToolTip( lColorAnnouncement, tipColorAnnouncement );

            string tipColorPM = "Color of private messages and rank-wide messages. Default is aqua.";
            toolTip.SetToolTip( bColorPM, tipColorPM );
            toolTip.SetToolTip( lColorPM, tipColorPM );



            toolTip.SetToolTip( xShowJoinedWorldMessages, "Show messages when players change worlds." );

            toolTip.SetToolTip( xRankColors, "Color player names in chat and in-game based on their rank." );

            toolTip.SetToolTip( xRankColorsInWorldNames, "Color world names in chat based on their build and access permissions." );

            toolTip.SetToolTip( xChatPrefixes,
@"Show 1-letter prefixes in chat before player names. This can be
used to set up IRC-style ""+"" and ""@"" prefixes for ops." );

            toolTip.SetToolTip( xListPrefixes,
@"Show prefixes in the player list. As a side-effect, Minecraft client
will not show custom skins for players with prefixed names." );



            toolTip.SetToolTip( bRules,
@"Edit the list of rules displayed by the ""/rules"" command.
This list is stored in rules.txt, and can also be edited with any text editor.
If rules.txt is missing or empty, ""/rules"" shows this message:
""Use common sense!""" );

            toolTip.SetToolTip( bAnnouncements,
@"Edit the list of announcements (announcements.txt).
One line is shown at a time, in random order.
You can include any color codes in the announcements.
You can also edit announcements.txt with any text editor." );


            string tipAnnouncements =
@"Show a random announcement every once in a while.
Announcements are shown to all players, one line at a time, in random order.";
            toolTip.SetToolTip( xAnnouncements, tipAnnouncements );
            toolTip.SetToolTip( nAnnouncements, tipAnnouncements );
            toolTip.SetToolTip( lAnnouncementsUnits, tipAnnouncements );

            toolTip.SetToolTip( bGreeting,
@"Edit a custom greeting that's shown to connecting players.
You can use any color codes, and these special variables:
    {SERVER_NAME} = server name (as defined in config)
    {RANK} = connecting player's rank" );
        }


        void FillToolTipsWorlds() {
            toolTip.SetToolTip( bAddWorld, "Add a new world to the list." );
            toolTip.SetToolTip( bWorldEdit, "Edit or replace an existing world." );
            toolTip.SetToolTip( cMainWorld, "Main world is the first world that players see when they join the server." );
            toolTip.SetToolTip( bWorldDelete, "Delete a world from the list." );
            string tipDefaultBuildRank =
@"When new maps are loaded with the /wload command,
the build permission for new maps will default to this rank.";
            toolTip.SetToolTip( lDefaultBuildRank, tipDefaultBuildRank );
            toolTip.SetToolTip( cDefaultBuildRank, tipDefaultBuildRank );
        }


        void FillToolTipsRanks() {
            toolTip.SetToolTip( bAddRank, "Add a new rank to the list." );
            toolTip.SetToolTip( bDeleteRank,
@"Delete a rank from the list. You will be prompted to specify a replacement
rank - to be able to convert old references to the deleted rank." );
            toolTip.SetToolTip( bRaiseRank,
@"Raise a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );
            toolTip.SetToolTip( bLowerRank,
@"Lower a rank (and all players of the rank) on the hierarchy.
The hierarchy is used for all permission checks." );

            string tipRankName = "Name of the rank - between 2 and 16 alphanumeric characters.";
            toolTip.SetToolTip( lRankName, tipRankName );
            toolTip.SetToolTip( tRankName, tipRankName );

            string tipRankColor =
@"Color associated with this rank.
Rank colors may be applied to player and world names.";
            toolTip.SetToolTip( lRankColor, tipRankColor );
            toolTip.SetToolTip( bColorRank, tipRankColor );

            string tipPrefix =
@"1-character prefix that may be shown above player names.
The option to show prefixes in chat is on ""General"" tab.";
            toolTip.SetToolTip( lPrefix, tipPrefix );
            toolTip.SetToolTip( tPrefix, tipPrefix );



            string tipKickLimit =
@"Limit on who can be kicked by players of this rank.
By default, players can only kick players of same or lower rank.";
            toolTip.SetToolTip( lKickLimit, tipKickLimit );
            toolTip.SetToolTip( cKickLimit, tipKickLimit );

            string tipBanLimit =
@"Limit on who can be banned by players of this rank.
By default, players can only ban players of same or lower rank.";
            toolTip.SetToolTip( lBanLimit, tipBanLimit );
            toolTip.SetToolTip( cBanLimit, tipBanLimit );

            string tipPromoteLimit =
@"Limit on how much can players of this rank promote others.
By default, players can only promote up to the same or lower rank.";
            toolTip.SetToolTip( lPromoteLimit, tipPromoteLimit );
            toolTip.SetToolTip( cPromoteLimit, tipPromoteLimit );

            string tipDemoteLimit =
@"Limit on who can be demoted by players of this rank.
By default, players can only demote players of same or lower rank.";
            toolTip.SetToolTip( lDemoteLimit, tipDemoteLimit );
            toolTip.SetToolTip( cDemoteLimit, tipDemoteLimit );

            string tipHideLimit =
@"Limit on whom can players of this rank hide from.
By default, players can only hide from players of same or lower rank.";
            toolTip.SetToolTip( lMaxHideFrom, tipHideLimit );
            toolTip.SetToolTip( cMaxHideFrom, tipHideLimit );

            string tipFreezeLimit =
@"Limit on who can be frozen by players of this rank.
By default, players can only freeze players of same or lower rank.";
            toolTip.SetToolTip( lFreezeLimit, tipFreezeLimit );
            toolTip.SetToolTip( cFreezeLimit, tipFreezeLimit );

            string tipMuteLimit =
@"Limit on who can be muted by players of this rank.
By default, players can only mute players of same or lower rank.";
            toolTip.SetToolTip( lMuteLimit, tipMuteLimit );
            toolTip.SetToolTip( cMuteLimit, tipMuteLimit );


            toolTip.SetToolTip( xReserveSlot,
@"Allows players of this rank to join the server
even if it reached the maximum number of players." );

            string tipKickIdle = "Allows kicking players who have been inactive/AFK for some time.";
            toolTip.SetToolTip( xKickIdle, tipKickIdle );
            toolTip.SetToolTip( nKickIdle, tipKickIdle );
            toolTip.SetToolTip( lKickIdleUnits, tipKickIdle );

            toolTip.SetToolTip( xAntiGrief,
@"Antigrief is an automated system for kicking players who build
or delete at abnormally high rates. This helps stop certain kinds
of malicious software (like MCTunnel) from doing large-scale damage
to server maps. False positives can sometimes occur if server or
player connection is very laggy." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Maximum number of blocks that players of this rank are
allowed to build in a specified time period." );

            toolTip.SetToolTip( nAntiGriefBlocks,
@"Minimum time interval that players of this rank are
expected to spent to build a specified number of blocks." );

            string tipDrawLimit =
@"Limit on the number of blocks that a player is
allowed to affect with drawing or copy/paste commands
at one time. If unchecked, there is no limit.";
            toolTip.SetToolTip( xDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( nDrawLimit, tipDrawLimit );
            toolTip.SetToolTip( lDrawLimitUnits, tipDrawLimit );




            vPermissions.Items[(int)Permission.Ban].ToolTipText =
@"Ability to ban/unban other players from the server.
Affected commands:
    /ban
    /unban";

            vPermissions.Items[(int)Permission.BanAll].ToolTipText =
@"Ability to ban/unban a player account, his IP, and all other accounts that used the IP.
BanAll/UnbanAll commands can be used on players who keep evading bans.
Required permissions: Ban & BanIP
Affected commands:
    /banall
    /unbanall";

            vPermissions.Items[(int)Permission.BanIP].ToolTipText =
@"Ability to ban/unban players by IP.
Required permission: Ban
Affected commands:
    /banip
    /unbanip";

            vPermissions.Items[(int)Permission.Bring].ToolTipText =
@"Ability to bring/summon other players to your location.
This works a bit like reverse-teleport - other player is sent to you.
Affected commands:
    /bring";

            vPermissions.Items[(int)Permission.Build].ToolTipText =
@"Ability to place blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.Chat].ToolTipText =
@"Ability to chat and PM players. Note that players without this
permission can still type in commands, receive PMs, and read chat.
Affected commands:
    /say
    @ (pm)
    @@ (rank chat)";

            vPermissions.Items[(int)Permission.CopyAndPaste].ToolTipText =
@"Ability to copy (or cut) and paste blocks. The total number of
blocks that can be copied or pasted at a time is affected by
the draw limit.
Affected commands:
    /copy
    /cut
    /mirror
    /paste, /pastenot
    /rotate";

            vPermissions.Items[(int)Permission.Delete].ToolTipText =
@"Ability to delete or replace blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.DeleteAdmincrete].ToolTipText =
@"Ability to delete admincrete (aka adminium) blocks. Even if someone
has this permission, it can be overriden by world-specific and
zone-specific permissions.
Required permission: Delete";

            vPermissions.Items[(int)Permission.Demote].ToolTipText =
@"Ability to demote other players to a lower rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.Draw].ToolTipText =
@"Ability to use drawing tools (commands capable of affecting many blocks
at once). This permission can be overriden by world-specific and
zone-specific permissions.
Required permission: Build, Delete
Affected commands:
    /cancel
    /cuboid, /cuboidh, and /cuboidw
    /ellipsoid
    /line
    /mark
    /replace and /replacenot
    /undo";

            vPermissions.Items[(int)Permission.EditPlayerDB].ToolTipText =
@"Ability to edit the player database directly. This also adds the ability to
promote/demote/ban players by name, even if they have not visited the server yet.
Affected commands:
    /autorankall
    /autorankreload
    /editplayerinfo
    /massrank
    /setinfo";

            vPermissions.Items[(int)Permission.Freeze].ToolTipText =
@"Ability to freeze/unfreeze players. Frozen players cannot
move or build/delete.
Affected commands:
    /freeze
    /unfreeze";

            vPermissions.Items[(int)Permission.Hide].ToolTipText =
@"Ability to appear hidden from other players. You can still chat,
build/delete blocks, use all commands, and join worlds while hidden.
Hidden players are completely invisible to other players.
Affected commands:
    /hide
    /unhide";

            vPermissions.Items[(int)Permission.Import].ToolTipText =
@"Ability to import rank and ban lists from files. Useful if you
are switching from another server software.
Affected commands:
    /importranks
    /importbans";

            vPermissions.Items[(int)Permission.Kick].ToolTipText =
@"Ability to kick players from the server.
Affected commands:
    /kick";

            vPermissions.Items[(int)Permission.Lock].ToolTipText =
@"Ability to lock/unlock maps (locking puts a map into read-only state).
Affected commands:
    /lock
    /unlock
    /lockall
    /unlockall";

            vPermissions.Items[(int)Permission.ManageWorlds].ToolTipText =
@"Ability to manipulate the world list: adding, renaming, and deleting worlds,
loading/saving maps, change per-world permissions, and using the map generator.
Affected commands:
    /wload
    /wunload
    /wrename
    /wmain
    /waccess and /wbuild
    /wflush
    /gen";

            vPermissions.Items[(int)Permission.ManageZones].ToolTipText =
@"Ability to manipulate zones: adding, editing, renaming, and removing zones.
Affected commands:
    /zadd
    /zedit
    /zremove";

            vPermissions.Items[(int)Permission.Mute].ToolTipText =
@"Ability to temporarily mute players. Muted players cannot write chat or 
send PMs, but they can still type in commands, receive PMs, and read chat.
Affected commands:
    /mute
    /unmute";

            vPermissions.Items[(int)Permission.Patrol].ToolTipText =
@"Ability to patrol lower-ranked players. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden.
Required permission: Teleport
Affected commands:
    /patrol";

            vPermissions.Items[(int)Permission.PlaceAdmincrete].ToolTipText =
@"Ability to place admincrete/adminium. This also affects draw commands.
Required permission: Build
Affected commands:
    /solid
    /bind";

            vPermissions.Items[(int)Permission.PlaceGrass].ToolTipText =
@"Ability to place grass blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /grass
    /bind";

            vPermissions.Items[(int)Permission.PlaceLava].ToolTipText =
@"Ability to place lava blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /lava
    /bind";

            vPermissions.Items[(int)Permission.PlaceWater].ToolTipText =
@"Ability to place water blocks. This also affects draw commands.
Required permission: Build
Affected commands:
    /water
    /bind";

            vPermissions.Items[(int)Permission.Promote].ToolTipText =
@"Ability to promote players to a higher rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.ReadStaffChat].ToolTipText =
@"Ability to read staff chat.";

            vPermissions.Items[(int)Permission.ReloadConfig].ToolTipText =
@"Ability to reload the configuration file without restarting.
Affected commands:
    /reloadconfig";

            vPermissions.Items[(int)Permission.Say].ToolTipText =
@"Ability to use /say command to show announcements.
Required permission: Chat
Affected commands:
    /say";

            vPermissions.Items[(int)Permission.SetSpawn].ToolTipText =
@"Ability to change the spawn point of a world or a player.
Affected commands:
    /setspawn";

            vPermissions.Items[(int)Permission.ShutdownServer].ToolTipText =
@"Ability to initiate a graceful shutdown remotely.
Useful for servers that are run on dedicated machines.
Affected commands:
    /shutdown
    /restart";

            vPermissions.Items[(int)Permission.Teleport].ToolTipText =
@"Ability to teleport to other players.
Affected commands:
    /tp";

            vPermissions.Items[(int)Permission.UseSpeedHack].ToolTipText =
@"Ability to move at a faster-than-normal rate (using hacks).
WARNING: Speedhack detection is experimental, and may produce many
false positives - especially on laggy servers.";

            vPermissions.Items[(int)Permission.ViewOthersInfo].ToolTipText =
@"Ability to view extended information about other players.
Affected commands:
    /info
    /baninfo
    /where";
        }


        void FillToolTipsSecurity() {
            string tipVerifyNames =
@"Name verification ensures that connecting players are not impersonating
someone else. Strict verification uses only the main verification method.
Sometimes it can produce false negatives - for example if server has just
restarted, or if minecraft.net heartbeats are timing out. Balanced verification
checks player's current and on-record IP address to eliminate false negatives.";
            toolTip.SetToolTip( lVerifyNames, tipVerifyNames );
            toolTip.SetToolTip( cVerifyNames, tipVerifyNames );

            toolTip.SetToolTip( xLimitOneConnectionPerIP,
@"Only allow 1 connection per IP. Note that all players on the same LAN
will share an IP, and may be prevented from joining together. Don't enable
this option unless there is a specific need/threat." );

            toolTip.SetToolTip( xAllowUnverifiedLAN,
@"Allow players from your local network (LAN) to connect without name verification.
May be useful if minecraft.net is blocked on your LAN for some reason.
Warning: unverified players can log in with ANY name - even as you!" );

            toolTip.SetToolTip( xRequireBanReason, "Require players to specify a reason/memo when banning or unbanning someone." );
            toolTip.SetToolTip( xRequireRankChangeReason, "Require players to specify a reason/memo when promoting or demoting someone." );
            toolTip.SetToolTip( xAnnounceKickAndBanReasons, "Show the reason/memo in chat for everyone when someone gets kicked/banned/unbanned." );
            toolTip.SetToolTip( xAnnounceRankChanges, "Announce promotions and demotions in chat." );

            string tipPatrolledRank =
@"When players use the /patrol command, they will be  teleported
to players of this (or lower) rank. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden.";
            toolTip.SetToolTip( lPatrolledRank, tipPatrolledRank );
            toolTip.SetToolTip( cPatrolledRank, tipPatrolledRank );
            toolTip.SetToolTip( lPatrolledRankAndBelow, tipPatrolledRank );

            toolTip.SetToolTip( xPaidPlayersOnly,
@"Only allow players who have a paid Minecraft account (not recommended).
This will help filter out griefers with throwaway accounts,
but will also prevent many legitimate players from joining." );

            toolTip.SetToolTip( xAllowSecurityCircumvention,
@"Allows players to manupulate whitelists/blacklists or rank requirements
in order to join restricted worlds, or to build in worlds/zones. Normally
players with ManageWorlds and ManageZones permissions are not allowed to do this.
Affected commands:
    /waccess
    /wbuild
    /wmain
    /zedit" );
        }


        void FillToolTipsSavingAndBackup() {
            toolTip.SetToolTip( xSaveOnShutdown,
@"Whether to save maps when server is shutting down or not.
generally this is a good idea." );

            string tipSaveInterval =
@"Whether to save maps (if modified) automatically once in a while.
If disabled, maps are only saved when a world is unloaded.";
            toolTip.SetToolTip( xSaveInterval, tipSaveInterval );
            toolTip.SetToolTip( nSaveInterval, tipSaveInterval );
            toolTip.SetToolTip( lSaveIntervalUnits, tipSaveInterval );

            toolTip.SetToolTip( xBackupOnStartup, "Create a backup of every map when the server starts." );

            string tipBackupInterval =
@"Create backups of loaded maps automatically once in a while.
A world is considered ""loaded"" if there is at least one player on it.";
            toolTip.SetToolTip( xBackupInterval, tipBackupInterval );
            toolTip.SetToolTip( nBackupInterval, tipBackupInterval );
            toolTip.SetToolTip( lBackupIntervalUnits, tipBackupInterval );

            toolTip.SetToolTip( xBackupOnlyWhenChanged, "Only save backups if the map changed in any way since last backup." );

            toolTip.SetToolTip( xBackupOnJoin,
@"Create backups any time a player joins a map.
Both a timestamp and player's name are included in the filename." );

            string tipMaxBackups =
@"Maximum number of backup files that fCraft should keep.
If exceeded, oldest backups will be deleted.";
            toolTip.SetToolTip( xMaxBackups, tipMaxBackups );
            toolTip.SetToolTip( nMaxBackups, tipMaxBackups );
            toolTip.SetToolTip( lMaxBackups, tipMaxBackups );

            string tipMaxBackupSize =
@"Maximum combined filesize of all backups.
If exceeded, oldest backups will be deleted.";
            toolTip.SetToolTip( xMaxBackupSize, tipMaxBackupSize );
            toolTip.SetToolTip( nMaxBackupSize, tipMaxBackupSize );
            toolTip.SetToolTip( lMaxBackupSize, tipMaxBackupSize );
        }


        void FillToolTipsLogging() {
            string tipLogMode = "Select the way logs are stored.";
            toolTip.SetToolTip( lLogMode, tipLogMode );
            toolTip.SetToolTip( cLogMode, tipLogMode );

            string tipLogLimit = "If enabled, old logs will be automatically erased.";
            toolTip.SetToolTip( xLogLimit, tipLogLimit );
            toolTip.SetToolTip( nLogLimit, tipLogLimit );
            toolTip.SetToolTip( lLogLimitUnits, tipLogLimit );

            vLogFileOptions.Items[(int)LogType.ConsoleInput].ToolTipText = "Commands typed in from the server console.";
            vLogFileOptions.Items[(int)LogType.ConsoleOutput].ToolTipText =
@"Things sent directly in response to console input,
e.g. output of commands called from console.";
            vLogFileOptions.Items[(int)LogType.Debug].ToolTipText = "Technical information that may be useful to find bugs.";
            vLogFileOptions.Items[(int)LogType.Error].ToolTipText = "Major errors and problems.";
            vLogFileOptions.Items[(int)LogType.SeriousError].ToolTipText = "Errors that prevent server from starting or result in crashes.";
            vLogFileOptions.Items[(int)LogType.GlobalChat].ToolTipText = "Normal chat messages written by players.";
            vLogFileOptions.Items[(int)LogType.IRC].ToolTipText =
@"IRC-related status and error messages.
Does not include IRC chatter (see IRCChat).";
            vLogFileOptions.Items[(int)LogType.PrivateChat].ToolTipText = "PMs (Private Messages) exchanged between players (@player message).";
            vLogFileOptions.Items[(int)LogType.RankChat].ToolTipText = "Rank-wide messages (@@rank message).";
            vLogFileOptions.Items[(int)LogType.SuspiciousActivity].ToolTipText = "Suspicious activity - hack attempts, failed logins, unverified names.";
            vLogFileOptions.Items[(int)LogType.SystemActivity].ToolTipText = "Status messages regarding normal system activity.";
            vLogFileOptions.Items[(int)LogType.UserActivity].ToolTipText = "Status messages regarding players' actions.";
            vLogFileOptions.Items[(int)LogType.UserCommand].ToolTipText = "Commands types in by players.";
            vLogFileOptions.Items[(int)LogType.Warning].ToolTipText = "Minor, recoverable errors and problems.";

            foreach( LogType type in Enum.GetValues( typeof( LogType ) ) ) {
                if( type == LogType.Trace ) continue;
                vConsoleOptions.Items[(int)type].ToolTipText = vLogFileOptions.Items[(int)type].ToolTipText;
            }
        }


        void FillToolTipsIRC() {
            toolTip.SetToolTip( xIRC, 
@"fCraft contains an IRC (Internet Relay Chat) bot for
relaying messages to and from any IRC network.
Note that encrypted IRC (via SSL) is not supported." );

            string tipIRCList =
@"Choose one of these popular IRC networks,
or type in address/port manually below.";
            toolTip.SetToolTip( lIRCList, tipIRCList );
            toolTip.SetToolTip( cIRCList, tipIRCList );

            string tipIRCBotNetwork = "Host or address of the IRC network.";
            toolTip.SetToolTip( lIRCBotNetwork, tipIRCBotNetwork );
            toolTip.SetToolTip( tIRCBotNetwork, tipIRCBotNetwork );

            string tipIRCBotPort = "Port number of the IRC network (default: 6667).";
            toolTip.SetToolTip( lIRCBotPort, tipIRCBotPort );
            toolTip.SetToolTip( nIRCBotPort, tipIRCBotPort );

            string tipIRCDelay =
@"Minimum delay (in milliseconds) between IRC messages.
Many networks have strict anti-flood limits, so a delay
of at least 500ms is recommended.";
            toolTip.SetToolTip( lIRCDelay, tipIRCDelay );
            toolTip.SetToolTip( nIRCDelay, tipIRCDelay );
            toolTip.SetToolTip( lIRCDelayUnits, tipIRCDelay );

            toolTip.SetToolTip( tIRCBotChannels,
@"Comma-separated list of channels to join. Channel names should include the hash (#).
One some IRC networks, channel names are case-sensitive." );

            string tipIRCBotNick =
@"IRC bot's nickname. If the nickname is taken, fCraft will append
an underscore (_) to the name and retry.";
            toolTip.SetToolTip( lIRCBotNick, tipIRCBotNick );
            toolTip.SetToolTip( tIRCBotNick, tipIRCBotNick );

            toolTip.SetToolTip( xIRCRegisteredNick,
@"Check this if bot's nickname is registered
or requires identification/authentication." );

            string tipIRCNickServ = "Name of the registration service bot (usually NickServ)";
            toolTip.SetToolTip( lIRCNickServ, tipIRCNickServ );
            toolTip.SetToolTip( tIRCNickServ, tipIRCNickServ );

            string tipIRCNickServMessage = "Message to send to registration service bot.";
            toolTip.SetToolTip( lIRCNickServMessage, tipIRCNickServMessage );
            toolTip.SetToolTip( tIRCNickServMessage, tipIRCNickServMessage );

            string tipColorIRC = "Color of IRC message in-game.";
            toolTip.SetToolTip( lColorIRC, tipColorIRC );
            toolTip.SetToolTip( bColorIRC, tipColorIRC );

            toolTip.SetToolTip( xIRCBotForwardFromServer,
@"If checked, all chat messages on IRC are shown in the game.
Otherwise, only IRC messages starting with a hash (#) will be relayed." );

            toolTip.SetToolTip( xIRCBotForwardFromIRC,
@"If checked, all chat messages from the server are shown on IRC.
Otherwise, only chat messages starting with a hash (#) will be relayed." );

            toolTip.SetToolTip( xIRCBotAnnounceServerJoins, "Show a message on IRC when someone joins of leaves the server." );
            toolTip.SetToolTip( xIRCBotAnnounceIRCJoins, "Show a message in-gam,e when someone joins of leaves the IRC channel." );
        }


        void FillToolTipsAdvanced() {
            toolTip.SetToolTip( xRelayAllBlockUpdates,
@"When a player places or deletes a block, vanilla Minecraft server
relays the action back. This is not needed, and only wastes bandwidth." );

            toolTip.SetToolTip( xNoPartialPositionUpdates,
@"Minecraft protocol specifies 4 different movement packet types.
One of them sends absolute position, and other 3 send incremental relative positions." );

            toolTip.SetToolTip( xLowLatencyMode,
@"This mode reduces lag by up to 200ms, at the cost of vastly increased
bandwidth use. It's only practical if you have a very fast connection
with few players, or if your server is LAN-only." );

            string tipProcessPriority =
@"It is recommended to leave fCraft at default priority.
Setting this below ""Normal"" may starve fCraft of resources.
Setting this above ""Normal"" may slow down other software on your machine.";
            toolTip.SetToolTip( lProcessPriority, tipProcessPriority );
            toolTip.SetToolTip( cProcessPriority, tipProcessPriority );

            string tipUpdater =
@"fCraft can automatically update to latest stable versions.
The update check is done on-startup.
    ""Disabled"" - no check is done at all
    ""Notify"" - fCraft only shows a message about availability
    ""Download/Prompt"" - fCraft downloads the update automatically,
        shows a list of changes, and asks to continue (or cancel).
    ""Automatic"" - fCraft downloads and applies updates at once.";
            toolTip.SetToolTip( lUpdater, tipUpdater );
            toolTip.SetToolTip( cUpdater, tipUpdater );

            string tipThrottling =
@"The maximum number of block changes that can be sent to each client per second.
Unmodified Minecraft client can only handle about 2500 updates per second.
Setting this any higher may cause lag. Setting this lower will show down
drawing commands (like cuboid).";
            toolTip.SetToolTip( lThrottling, tipThrottling );
            toolTip.SetToolTip( nThrottling, tipThrottling );
            toolTip.SetToolTip( lThrottlingUnits, tipThrottling );

            string tipTickInterval =
@"The rate at which fCraft applies block updates. Lowering this will slightly
reduce bandwidth and CPU use, but will add latency to block placement.";
            toolTip.SetToolTip( lTickInterval, tipTickInterval );
            toolTip.SetToolTip( nTickInterval, tipTickInterval );
            toolTip.SetToolTip( lTickIntervalUnits, tipTickInterval );

            string tipMaxUndo =
@"The number of blocks that players can undo at a time.
Only the most-recent draw command can be undo, so the actual
limit also depends on rank draw limits. Saving undo information
takes up 16 bytes per block.";
            toolTip.SetToolTip( xMaxUndo, tipMaxUndo );
            toolTip.SetToolTip( nMaxUndo, tipMaxUndo );
            toolTip.SetToolTip( lMaxUndoUnits, tipMaxUndo );
        }
    }
}