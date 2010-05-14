using System;
using System.Collections.Generic;



namespace fCraft {
    public class MapSenderParams {
        public World world;
        public Map map;
        public Player player;
    }

    public static class MapSender {
        // wrapper for use with Tasks
        public static void StreamLoad( object param ) {
            StreamLoad( (MapSenderParams)param );
        }

        public static bool StreamLoad( MapSenderParams param ) {
            try {
                param.world.completedBlockUpdates = 0;
                param.world.totalBlockUpdates = param.world.map.CompareAndUpdate( param.map );
                param.world.map.spawn = param.map.spawn;

                int ETA = param.world.totalBlockUpdates / param.world.server.CalculateMaxPacketsPerUpdate() / 10 + 1;
                param.world.SendToAll( Color.Red + "Reverting to a backup. ETA to completion: " + ETA + " seconds.", null );

                param.world.loadProgressReported = ETA < 10;

                param.world.loadInProgress = false;
                param.world.loadSendingInProgress = true;
                return true;

            } catch( Exception ex ) {
                Logger.Log( "An error occured while trying to load a map: " + ex.Message, LogType.Error );
                param.world.loadInProgress = false;
                param.world.EndLockDown();
                return false;
            }
        }
        
        // work in progress - not functional yet
        public static bool FullLoad( MapSenderParams param ) {
            try {
                //param.world.
                param.world.loadInProgress = false;
                param.world.loadSendingInProgress = true;
                return true;

            } catch( Exception ex ) {
                Logger.Log( "An error occured while trying to load a map: " + ex.Message, LogType.Error );
                param.world.loadInProgress = false;
                param.world.EndLockDown();
                return false;
            }
        }
    }
}
