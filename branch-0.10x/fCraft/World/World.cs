using System;
using System.Collections.Generic;

namespace fCraft {
    static class World {
        public static Server server;
        public static Level level;
        public static List<Session> sessions = new List<Session>();

        public static void RegisterSession( Session session ) {
            sessions.Add( session );
        }

        public static int GetPlayerCount() { return 0; }
    }
}
