using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoRankEditor {
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
}
