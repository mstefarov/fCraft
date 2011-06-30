// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    /// <summary> Simple interface for objects to notify of changes in their serializable state.
    /// This event is used to trigger saving things like Zone- and MetadataCollection.
    /// sender should be set for EventHandler, and e should be set to EventArgs.Empty </summary>
    interface INotifiesOnChange {
        event EventHandler Changed;
    }
}
