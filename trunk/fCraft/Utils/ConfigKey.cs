// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary>
    /// Enumeration of available configuration keys. See comment
    /// at the top of Config.cs for a history of changes.
    /// </summary>
    public enum ConfigKey {
        ServerName,
        MOTD,
        MaxPlayers,
        DefaultRank,
        IsPublic,
        Port,
        IP,
        UploadBandwidth,

        DefaultBuildRank,

        RankColorsInChat,
        RankColorsInWorldNames,
        RankPrefixesInChat,
        RankPrefixesInList,
        ShowJoinedWorldMessages,
        SystemMessageColor,
        HelpColor,
        SayColor,
        AnnouncementColor,
        PrivateMessageColor,
        MeColor,
        WarningColor,
        AnnouncementInterval,

        VerifyNames,
        LimitOneConnectionPerIP,
        AllowUnverifiedLAN,
        PatrolledRank,
        AntispamMessageCount,
        AntispamInterval,
        AntispamMuteDuration,
        AntispamMaxWarnings,
        PaidPlayersOnly,

        RequireBanReason,
        RequireRankChangeReason,
        AnnounceKickAndBanReasons,
        AnnounceRankChanges,

        SaveOnShutdown,
        SaveInterval,

        BackupOnStartup,
        BackupOnJoin,
        BackupOnlyWhenChanged,
        BackupInterval,
        MaxBackups,
        MaxBackupSize, // in megabytes

        LogMode,
        MaxLogs,

        IRCBot,
        IRCBotNick,
        IRCBotNetwork,
        IRCBotPort,
        IRCBotChannels,
        IRCBotForwardFromServer,
        IRCBotForwardFromIRC,
        IRCBotAnnounceServerJoins,
        IRCBotAnnounceIRCJoins,
        IRCBotAnnounceServerEvents,
        IRCRegisteredNick,
        IRCNickServ,
        IRCNickServMessage,
        IRCMessageColor,
        IRCDelay,
        IRCThreads,
        IRCUseColor,

        RelayAllBlockUpdates,
        AutomaticUpdates,
        NoPartialPositionUpdates,
        ProcessPriority,
        BlockUpdateThrottling,
        TickInterval,
        LowLatencyMode,
        SubmitCrashReports,
        MaxUndo,

        MapPath,
        LogPath,
        DataPath,

        AutoRankEnabled,
        HeartbeatEnabled
    }
}
