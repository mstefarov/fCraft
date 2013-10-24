// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;

namespace fCraft {
    /// <summary> An EventArgs for an event that can be cancelled. </summary>
    public interface ICancelableEvent {
        /// <summary> Set to "true" to cancel the event. </summary>
        bool Cancel { get; set; }
    }


    /// <summary> Simple interface for objects to notify of changes in their serializable state.
    /// This event is used to trigger saving things like Zone- and MetadataCollection.
    /// sender should be set for EventHandler, and e should be set to EventArgs.Empty </summary>
    internal interface INotifiesOnChange {
        event EventHandler Changed;
    }
}
