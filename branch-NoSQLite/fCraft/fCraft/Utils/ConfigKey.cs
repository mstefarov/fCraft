using System;


namespace fCraft {
    public enum ConfigKey {
        ServerName,
        MOTD,
        MaxPlayers,
        DefaultClass,
        IsPublic,
        Port,
        IP,
        UploadBandwidth,

        ClassColorsInChat,
        ClassColorsInWorldNames,
        ClassPrefixesInChat,
        ClassPrefixesInList,
        ShowJoinedWorldMessages,
        SystemMessageColor,
        HelpColor,
        SayColor,
        AnnouncementColor,
        PrivateMessageColor,
        AnnouncementInterval,

        VerifyNames,

        LimitOneConnectionPerIP,

        PatrolledClass,

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
        MaxBackupSize, // in megabytes

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
