// Copyright 2009-2014 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Three state enum used for parameters which can be manually enabled/disabled (yes/no),
    /// or left alone (auto). Default value is "Auto". </summary>
    public enum YesNoAuto {
        /// <summary> Indicates that automatically-deduced or default value should be used. </summary>
        Auto,

        /// <summary> Indicates "true" in all cases. </summary>
        Yes,

        /// <summary> Indicates "false" in all cases. </summary>
        No
    }
}