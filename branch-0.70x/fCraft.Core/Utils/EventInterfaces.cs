﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    /// <summary> An EventArgs for an event that can be cancelled. </summary>
    public interface ICancellableEvent {
        /// <summary> Set to "true" to cancel the event. </summary>
        bool Cancel { get; set; }
    }


    /// <summary> An EventArgs for an event that directly relates to a particular player. </summary>
    public interface IPlayerEvent {
        Player Player { get; }
    }


    /// <summary> An EventArgs for an event that directly relates to a particular world. </summary>
    public interface IWorldEvent {
        World World { get; }
    }


    /// <summary> Simple interface for objects to notify of changes in their serializable state.
    /// This event is used to trigger saving things like Zone- and MetadataCollection.
    /// sender should be set for EventHandler, and e should be set to EventArgs.Empty </summary>
    interface INotifiesOnChange {
        event EventHandler Changed;
    }
}