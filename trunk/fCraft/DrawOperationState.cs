// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;


namespace fCraft {
    public class DrawOperationState {
        public PlayerInfo Player;
        public Map Map;
        public Position[] Marks;
        public BoundingBox Bounds;
        public Command Command;
        public DateTime StartTime;

        public byte[] UndoBuffer; // this will have to be populated on-the-fly

        public int BlocksChecked;
        public int BlocksUpdated;
        public int BlocksTotal; // estimated

        public object UserState;
    }
}
