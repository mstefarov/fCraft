using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using fCraft.Events;

namespace fCraft {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    struct BlockDBEntry {
        public BlockDBEntry(int timestamp, int playerID, short x, short y, short z, byte oldBlock, byte newBlock){
            Timestamp=timestamp;
            PlayerID = playerID;
            X=x;
            Y=y;
            Z=z;
            OldBlock=oldBlock;
            NewBlock=newBlock;
        }
        public readonly int Timestamp, PlayerID;
        public readonly short X, Y, Z;
        public readonly byte OldBlock, NewBlock;
    }


    class BlockDB {
        static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds( 90 );

        internal static void Init() {
            Server.PlayerPlacedBlock += OnPlayerPlacedBlock;
            Scheduler.NewBackgroundTask( FlushPendingEntries ).RunForever( FlushInterval );
            Paths.TestDirectory( "BlockDB", Paths.BlockDBPath, true );
        }


        static void OnPlayerPlacedBlock( object sender, PlayerPlacedBlockEventArgs e ) {
            World world =e.Player.World;
            if( world.IsBlockTracked ) {
                BlockDBEntry newEntry = new BlockDBEntry( (int)DateTime.UtcNow.ToUnixTime(),
                                                          e.Player.Info.ID,
                                                          e.X, e.Y, e.Z,
                                                          (byte)e.OldBlock,
                                                          (byte)e.NewBlock );
                world.AddBlockDBEntry(newEntry);
            }
        }


        static void FlushPendingEntries( SchedulerTask task ) {
            lock( WorldManager.WorldListLock ) {
                foreach( World w in WorldManager.WorldList.Where( w => w.IsBlockTracked ) ) {
                    w.FlushBlockDB();
                }
            }
        }
    }
}