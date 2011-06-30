// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> An EventArgs for an event that can be cancelled. </summary>
    public interface ICancellableEvent {
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
}
