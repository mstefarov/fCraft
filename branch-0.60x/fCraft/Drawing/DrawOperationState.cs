// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {

    public abstract class DrawOperationState {
        public readonly Player Player;
        public readonly Map Map;
        public readonly Position[] Marks;
        public DateTime StartTime;

        public byte[] UndoBuffer;
        public IBrush Brush;

        public int BlocksChecked,
                   BlocksUpdated,
                   BlocksDenied,
                   BlocksTotalEstimate;

        public Vector3I Coords;

        public bool UseAlternateBlock;

        public abstract bool DrawBatch( int maxBlocksToDraw );
    }
}