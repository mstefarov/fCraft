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
        IRCMsgs,
        IRCBotNick,
        IRCBotQuitMsg,
        IRCBotNetwork,
        IRCBotPort,
        IRCBotChannels,
        IRCBotForwardFromServer,
        IRCBotForwardFromIRC,

        PolicyColorCodesInChat,
        PolicyIllegalCharacters,
        SendRedundantBlockUpdates,
        PingInterval,
        AutomaticUpdates,
        NoPartialPositionUpdates,
        ProcessPriority,
        RunOnStartup,
        BlockUpdateThrottling,
        TickInterval,
        LowLatencyMode
    }
}
