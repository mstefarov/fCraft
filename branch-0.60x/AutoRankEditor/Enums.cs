using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoRankEditor {
    public enum ComparisonOperation {
        eq,
        neq,
        gt,
        gte,
        lt,
        lte
    }


    public enum ConditionField {
        TimeSinceFirstLogin,
        TimeSinceLastLogin,
        LastSeen,
        TotalTime,
        BlocksBuilt,
        BlocksDeleted,
        BlocksChanged, // BlocksBuilt+BlocksDeleted
        BlocksDrawn,
        TimesVisited,
        MessagesWritten,
        TimesKicked,
        TimeSinceRankChange,
        TimeSinceLastKick
    }


    public enum ConditionScopeType {
        Total,
        SinceRankChange,
        SinceKick
    }


    public enum ActionType {
        Suggested,
        Required,
        Automatic
    }


    public enum RankChangeType {
        Default,
        Promoted,
        Demoted,
        AutoPromoted,
        AutoDemoted
    }
}
