// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Diagnostics;

namespace fCraft {
    /// <summary>
    /// Enumeration of available configuration keys. See comment
    /// at the top of Config.cs for a history of changes.
    /// </summary>
    public enum ConfigKey {
        #region General

        [StringKey( "Custom Minecraft Server (fCraft)", ConfigSection.General, MinLength = 1, MaxLength = 64 )]
        ServerName,

        [StringKey( "Welcome to the server!", ConfigSection.General, MinLength = 0, MaxLength = 64 )]
        MOTD,

        [IntKey( 20, ConfigSection.General, MinValue = 1, MaxValue = 128 )]
        MaxPlayers,

        [RankKey( RankKeyAttribute.BlankValueMeaning.LowestRank, ConfigSection.General )]
        DefaultRank,

        [BoolKey( false, ConfigSection.General )]
        IsPublic,

        [IntKey( 25565, ConfigSection.General, MinValue = 1, MaxValue = 65535 )]
        Port,

        [IPKey( IPKeyAttribute.BlankValueMeaning.Any, ConfigSection.General )]
        IP,

        [IntKey( 100, ConfigSection.General, MinValue = 1, MaxValue = short.MaxValue )]
        UploadBandwidth,

        #endregion


        #region Worlds

        [RankKey( RankKeyAttribute.BlankValueMeaning.DefaultRank, ConfigSection.Worlds )]
        DefaultBuildRank,

        [StringKey( "maps", ConfigSection.Worlds )]
        MapPath,

        #endregion


        #region Chat

        [BoolKey( true, ConfigSection.Chat )]
        RankColorsInChat,

        [BoolKey( true, ConfigSection.Chat )]
        RankColorsInWorldNames,

        [BoolKey( false, ConfigSection.Chat )]
        RankPrefixesInChat,

        [BoolKey( false, ConfigSection.Chat )]
        RankPrefixesInList,

        [BoolKey( true, ConfigSection.Chat )]
        ShowConnectionMessages,

        [BoolKey( true, ConfigSection.Chat )]
        ShowBannedConnectionMessages,

        [BoolKey( true, ConfigSection.Chat )]
        ShowJoinedWorldMessages,

        [ColorKey( Color.SysDefault, ConfigSection.Chat )]
        SystemMessageColor,

        [ColorKey( Color.HelpDefault, ConfigSection.Chat )]
        HelpColor,

        [ColorKey( Color.SayDefault, ConfigSection.Chat )]
        SayColor,

        [ColorKey( Color.AnnouncementDefault, ConfigSection.Chat )]
        AnnouncementColor,

        [ColorKey( Color.PMDefault, ConfigSection.Chat )]
        PrivateMessageColor,

        [ColorKey( Color.MeDefault, ConfigSection.Chat )]
        MeColor,

        [ColorKey( Color.WarningDefault, ConfigSection.Chat )]
        WarningColor,

        [IntKey( 0, ConfigSection.Chat )]
        AnnouncementInterval,

        #endregion


        #region Security

        [EnumKey( NameVerificationMode.Balanced, ConfigSection.Security )]
        VerifyNames,

        [BoolKey( false, ConfigSection.Security )]
        LimitOneConnectionPerIP,

        [BoolKey( false, ConfigSection.Security )]
        AllowUnverifiedLAN,

        [RankKey( RankKeyAttribute.BlankValueMeaning.DefaultRank, ConfigSection.Security )]
        PatrolledRank,

        [IntKey( 4, ConfigSection.Security, MinValue = 2, MaxValue = 64 )]
        AntispamMessageCount,

        [IntKey( 5, ConfigSection.Security, MinValue = 0, MaxValue = 64 )]
        AntispamInterval,

        [IntKey( 5, ConfigSection.Security, MinValue = 0, MaxValue = 36000 )]
        AntispamMuteDuration,

        [IntKey( 2, ConfigSection.Security, MinValue = 0, MaxValue = 64 )]
        AntispamMaxWarnings,

        [BoolKey( false, ConfigSection.Security )]
        PaidPlayersOnly,

        [BoolKey( false, ConfigSection.Security )]
        RequireBanReason,

        [BoolKey( false, ConfigSection.Security )]
        RequireRankChangeReason,

        [BoolKey( true, ConfigSection.Security )]
        AnnounceKickAndBanReasons,

        [BoolKey( true, ConfigSection.Security )]
        AnnounceRankChanges,

        #endregion


        #region Saving and Backup

        [BoolKey( true, ConfigSection.SavingAndBackup )]
        SaveOnShutdown,

        [IntKey( 90, ConfigSection.SavingAndBackup )]
        SaveInterval,

        [BoolKey( true, ConfigSection.SavingAndBackup )]
        BackupOnStartup,

        [BoolKey( true, ConfigSection.SavingAndBackup )]
        BackupOnJoin,

        [BoolKey( true, ConfigSection.SavingAndBackup )]
        BackupOnlyWhenChanged,

        [IntKey( 20, ConfigSection.SavingAndBackup )]
        BackupInterval,

        [IntKey( 0, ConfigSection.SavingAndBackup )]
        MaxBackups,

        [IntKey( 0, ConfigSection.SavingAndBackup )]
        MaxBackupSize, // in megabytes

        #endregion


        #region Logging

        [EnumKey( LogSplittingType.OneFile, ConfigSection.Logging )]
        LogMode,

        [IntKey( 0, ConfigSection.Logging )]
        MaxLogs,

        #endregion


        #region IRC

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotEnabled,

        [StringKey( "MinecraftBot", ConfigSection.IRC, MinLength = 1, MaxLength = 32 )]
        IRCBotNick,

        [StringKey( "irc.esper.net", ConfigSection.IRC )]
        IRCBotNetwork,

        [IntKey( 6667, ConfigSection.IRC, MinValue = 1, MaxValue = 65535 )]
        IRCBotPort,

        [StringKey( "#changeme", ConfigSection.IRC )]
        IRCBotChannels,

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotForwardFromServer,

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotForwardFromIRC,

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotAnnounceServerJoins,

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotAnnounceIRCJoins,

        [BoolKey( false, ConfigSection.IRC )]
        IRCBotAnnounceServerEvents,

        [BoolKey( false, ConfigSection.IRC )]
        IRCRegisteredNick,

        [StringKey( "NickServ", ConfigSection.IRC, MinLength = 1, MaxLength = 32 )]
        IRCNickServ,

        [StringKey( "IDENTIFY passwordGoesHere", ConfigSection.IRC )]
        IRCNickServMessage,

        [ColorKey( Color.IRCDefault, ConfigSection.IRC )]
        IRCMessageColor,

        [IntKey( 750, ConfigSection.IRC, MinValue = 0 )]
        IRCDelay,

        [IntKey( 1, ConfigSection.IRC, MinValue = 1 )]
        IRCThreads,

        [BoolKey( true, ConfigSection.IRC )]
        IRCUseColor,

        #endregion


        #region Advanced

        [BoolKey( false, ConfigSection.Advanced )]
        RelayAllBlockUpdates,

        [EnumKey( UpdaterMode.Prompt, ConfigSection.Advanced )]
        UpdateMode,

        [BoolKey( false, ConfigSection.Advanced )]
        UpdateAtStartup,

        [BoolKey( false, ConfigSection.Advanced )]
        NoPartialPositionUpdates,

        [EnumKey( ProcessPriorityClass.Normal, ConfigSection.Advanced, NotBlank = false )]
        ProcessPriority,

        [IntKey( 2048, ConfigSection.Advanced, MinValue = 10 )]
        BlockUpdateThrottling,

        [IntKey( 100, ConfigSection.Advanced, MinValue = 10, MaxValue = 10000 )]
        TickInterval,

        [BoolKey( false, ConfigSection.Advanced )]
        LowLatencyMode,

        [BoolKey( true, ConfigSection.Advanced )]
        SubmitCrashReports,

        [IntKey( 2000000, ConfigSection.Advanced, MinValue = 0 )]
        MaxUndo,

        [StringKey( "(console)", ConfigSection.Advanced, MinLength = 1, MaxLength = 64 )]
        ConsoleName,

        [BoolKey( false, ConfigSection.Advanced )]
        AutoRankEnabled,

        [BoolKey( true, ConfigSection.Advanced )]
        HeartbeatEnabled

        #endregion
    }


    public enum ConfigSection {
        General,
        Chat,
        Worlds,
        Security,
        SavingAndBackup,
        Logging,
        IRC,
        Advanced
    }
}