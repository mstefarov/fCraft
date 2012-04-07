// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft {
    /// <summary> Indicates what kind of per-entity override/exception is defined in a security controller. </summary>
    public enum PermissionOverride {
        /// <summary> No permission exception. </summary>
        None,

        /// <summary> Entity is explicitly allowed / whitelisted. </summary>
        Allow,

        /// <summary> Entity is explicitly denied / blacklisted. </summary>
        Deny
    }
}