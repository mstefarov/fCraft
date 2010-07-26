using System;


namespace fCraft {
    public enum ConfigKey : byte {
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

        VerifyNames,
        AnnounceUnverifiedNames,

        LimitOneConnectionPerIP,

        AntispamMessageCount,
        AntispamInterval,
        AntispamMuteDuration,
        AntispamMaxWarnings,

        AntigriefBlockCount,
        AntigriefInterval,

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
