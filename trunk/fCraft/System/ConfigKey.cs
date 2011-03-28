// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Diagnostics;

namespace fCraft {
    /// <summary>
    /// Enumeration of available configuration keys. See comment
    /// at the top of Config.cs for a history of changes.
    /// </summary>
    public enum ConfigKey {
        #region General

        [StringKey( ConfigSection.General, "Custom Minecraft Server (fCraft)", MinLength = 1, MaxLength = 64 )]
        ServerName,

        [StringKey( ConfigSection.General, "Welcome to the server!", MinLength = 0, MaxLength = 64 )]
        MOTD,

        [IntKey( ConfigSection.General, 20, MinValue = 1, MaxValue = 128 )]
        MaxPlayers,

        [RankKey( RankKeyAttribute.BlankValueMeaning.LowestRank, ConfigSection.General )]
        DefaultRank,

        [BoolKey( ConfigSection.General, false )]
        IsPublic,

        [IntKey( ConfigSection.General, 25565, MinValue = 1, MaxValue = 65535 )]
        Port,

        [IPKey( ConfigSection.General, IPKeyAttribute.BlankValueMeaning.Any )]
        IP,

        [IntKey( ConfigSection.General, 100, MinValue = 1, MaxValue = short.MaxValue )]
        UploadBandwidth,

        #endregion


        #region Worlds

        [RankKey( RankKeyAttribute.BlankValueMeaning.DefaultRank, ConfigSection.Worlds )]
        DefaultBuildRank,

        [StringKey( ConfigSection.Worlds, "maps" )]
        MapPath,

        #endregion


        #region Chat

        [BoolKey( ConfigSection.Chat, true )]
        RankColorsInChat,

        [BoolKey( ConfigSection.Chat, true )]
        RankColorsInWorldNames,

        [BoolKey( ConfigSection.Chat, false )]
        RankPrefixesInChat,

        [BoolKey( ConfigSection.Chat, false )]
        RankPrefixesInList,

        [BoolKey( ConfigSection.Chat, true )]
        ShowConnectionMessages,

        [BoolKey( ConfigSection.Chat, true )]
        ShowBannedConnectionMessages,

        [BoolKey( ConfigSection.Chat, true )]
        ShowJoinedWorldMessages,

        [ColorKey( ConfigSection.Chat, Color.SysDefault )]
        SystemMessageColor,

        [ColorKey( ConfigSection.Chat, Color.HelpDefault )]
        HelpColor,

        [ColorKey( ConfigSection.Chat, Color.SayDefault )]
        SayColor,

        [ColorKey( ConfigSection.Chat, Color.AnnouncementDefault )]
        AnnouncementColor,

        [ColorKey( ConfigSection.Chat, Color.PMDefault )]
        PrivateMessageColor,

        [ColorKey( ConfigSection.Chat, Color.MeDefault )]
        MeColor,

        [ColorKey( ConfigSection.Chat, Color.WarningDefault )]
        WarningColor,

        [IntKey( ConfigSection.Chat, 0, AlwaysAllowZero = true )]
        AnnouncementInterval,

        #endregion


        #region Security

        [EnumKey( ConfigSection.Security, NameVerificationMode.Balanced )]
        VerifyNames,

        [BoolKey( ConfigSection.Security, false )]
        LimitOneConnectionPerIP,

        [BoolKey( ConfigSection.Security, false )]
        AllowUnverifiedLAN,

        [RankKey( RankKeyAttribute.BlankValueMeaning.DefaultRank, ConfigSection.Security )]
        PatrolledRank,

        [IntKey( ConfigSection.Security, 4, AlwaysAllowZero = true, MinValue = 2, MaxValue = 64 )]
        AntispamMessageCount,

        [IntKey( ConfigSection.Security, 5, AlwaysAllowZero = true, MinValue = 1, MaxValue = 64 )]
        AntispamInterval,

        [IntKey( ConfigSection.Security, 5, MinValue = 0, MaxValue = 36000 )]
        AntispamMuteDuration,

        [IntKey( ConfigSection.Security, 2, MinValue = 0, MaxValue = 64 )]
        AntispamMaxWarnings,

        [BoolKey( ConfigSection.Security, false )]
        PaidPlayersOnly,

        [BoolKey( ConfigSection.Security, false )]
        RequireBanReason,

        [BoolKey( ConfigSection.Security, false )]
        RequireRankChangeReason,

        [BoolKey( ConfigSection.Security, true )]
        AnnounceKickAndBanReasons,

        [BoolKey( ConfigSection.Security, true )]
        AnnounceRankChanges,

        #endregion


        #region Saving and Backup

        [BoolKey( ConfigSection.SavingAndBackup, true )]
        SaveOnShutdown,

        [IntKey( ConfigSection.SavingAndBackup, 90, AlwaysAllowZero = true, MinValue = 10 )]
        SaveInterval,

        [BoolKey( ConfigSection.SavingAndBackup, true )]
        BackupOnStartup,

        [BoolKey( ConfigSection.SavingAndBackup, false )]
        BackupOnJoin,

        [BoolKey( ConfigSection.SavingAndBackup, true )]
        BackupOnlyWhenChanged,

        [IntKey( ConfigSection.SavingAndBackup, 20, AlwaysAllowZero = true, MinValue = 1 )]
        BackupInterval,

        [IntKey( ConfigSection.SavingAndBackup, 0 )]
        MaxBackups,

        [IntKey( ConfigSection.SavingAndBackup, 0 )]
        MaxBackupSize, // in megabytes

        #endregion


        #region Logging

        [EnumKey( ConfigSection.Logging, LogSplittingType.OneFile )]
        LogMode,

        [IntKey( ConfigSection.Logging, 0 )]
        MaxLogs,

        #endregion


        #region IRC

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotEnabled,

        [StringKey( ConfigSection.IRC, "MinecraftBot", MinLength = 1, MaxLength = 32 )]
        IRCBotNick,

        [StringKey( ConfigSection.IRC, "irc.esper.net" )]
        IRCBotNetwork,

        [IntKey( ConfigSection.IRC, 6667, MinValue = 1, MaxValue = 65535 )]
        IRCBotPort,

        [StringKey( ConfigSection.IRC, "#changeme" )]
        IRCBotChannels,

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotForwardFromServer,

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotForwardFromIRC,

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotAnnounceServerJoins,

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotAnnounceIRCJoins,

        [BoolKey( ConfigSection.IRC, false )]
        IRCBotAnnounceServerEvents,

        [BoolKey( ConfigSection.IRC, false )]
        IRCRegisteredNick,

        [StringKey( ConfigSection.IRC, "NickServ", MinLength = 1, MaxLength = 32 )]
        IRCNickServ,

        [StringKey( ConfigSection.IRC, "IDENTIFY passwordGoesHere" )]
        IRCNickServMessage,

        [ColorKey( ConfigSection.IRC, Color.IRCDefault )]
        IRCMessageColor,

        [IntKey( ConfigSection.IRC, 750, MinValue = 0 )]
        IRCDelay,

        [IntKey( ConfigSection.IRC, 1, MinValue = 1 )]
        IRCThreads,

        [BoolKey( ConfigSection.IRC, true )]
        IRCUseColor,

        #endregion


        #region Advanced

        [BoolKey( ConfigSection.Advanced, false )]
        RelayAllBlockUpdates,

        [EnumKey( ConfigSection.Advanced, fCraft.UpdaterMode.Prompt )]
        UpdaterMode,

        [StringKey( ConfigSection.Advanced, "", NotBlank = false )]
        RunBeforeUpdate,

        [StringKey( ConfigSection.Advanced, "", NotBlank = false )]
        RunAfterUpdate,

        [BoolKey( ConfigSection.Advanced, true )]
        BackupBeforeUpdate,

        [BoolKey( ConfigSection.Advanced, false )]
        NoPartialPositionUpdates,

        [EnumKey( ConfigSection.Advanced, ProcessPriorityClass.Normal, NotBlank = false )]
        ProcessPriority,

        [IntKey( ConfigSection.Advanced, 2048, MinValue = 10 )]
        BlockUpdateThrottling,

        [IntKey( ConfigSection.Advanced, 100, MinValue = 10, MaxValue = 10000 )]
        TickInterval,

        [BoolKey( ConfigSection.Advanced, false )]
        LowLatencyMode,

        [BoolKey( ConfigSection.Advanced, true )]
        SubmitCrashReports,

        [IntKey( ConfigSection.Advanced, 2000000, AlwaysAllowZero = true, MinValue = 1 )]
        MaxUndo,

        [StringKey( ConfigSection.Advanced, "(console)", MinLength = 1, MaxLength = 64 )]
        ConsoleName,

        [BoolKey( ConfigSection.Advanced, false )]
        AutoRankEnabled,

        [BoolKey( ConfigSection.Advanced, true )]
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