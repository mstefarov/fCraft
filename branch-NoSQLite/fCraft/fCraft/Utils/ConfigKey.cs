using System;


namespace fCraft {
    public enum ConfigKey {
        ServerName,
        MOTD,
        MaxPlayers,
        DefaultClass,
        IsPublic,
        Port,
        UploadBandwidth,

        ClassColorsInChat,
        ClassPrefixesInChat,
        ClassPrefixesInList,
        SystemMessageColor,
        HelpColor,
        SayColor,
        AnnouncementColor,
        PrivateMessageColor,
        AnnouncementInterval,

        VerifyNames,

        LimitOneConnectionPerIP,

        AntispamMessageCount,
        AntispamInterval,
        AntispamMuteDuration,
        AntispamMaxWarnings,

        RequireBanReason,
        RequireClassChangeReason,
        AnnounceKickAndBanReasons,
        AnnounceClassChanges,

        SaveOnShutdown,
        SaveInterval,

        BackupOnStartup,
        BackupOnJoin,
        BackupOnlyWhenChanged,
        BackupInterval,
        MaxBackups,
        MaxBackupSize,

        LogMode,
        MaxLogs,

        IRCBot,
        IRCBotNick,
        IRCBotQuitMsg,
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
        PingInterval,
        AutomaticUpdates,
        NoPartialPositionUpdates,
        ProcessPriority,
        BlockUpdateThrottling,
        TickInterval,
        LowLatencyMode,
        SubmitCrashReports
    }
}
