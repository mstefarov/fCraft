using System;


namespace fCraft {
    public enum ConfigKey {
        ServerName,
        MOTD,
        MaxPlayers,
        DefaultRank,
        IsPublic,
        Port,
        IP,
        UploadBandwidth,

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
        AnnouncementInterval,

        VerifyNames,

        LimitOneConnectionPerIP,

        PatrolledRank,

        AntispamMessageCount,
        AntispamInterval,
        AntispamMuteDuration,
        AntispamMaxWarnings,

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
        IRCMessageColor,
        IRCDelay,

        SendRedundantBlockUpdates,
        AutomaticUpdates,
        NoPartialPositionUpdates,
        ProcessPriority,
        BlockUpdateThrottling,
        TickInterval,
        LowLatencyMode,
        SubmitCrashReports
    }
}
