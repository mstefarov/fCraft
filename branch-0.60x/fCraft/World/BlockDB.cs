// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Runtime.InteropServices;
using fCraft.Events;

namespace fCraft {
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    struct BlockDBEntry {
        public BlockDBEntry( int timestamp, int playerID, short x, short y, short z, Block oldBlock, Block newBlock ) {
            Timestamp = timestamp;
            PlayerID = playerID;
            X = x;
            Y = y;
            Z = z;
            OldBlock = oldBlock;
            NewBlock = newBlock;
        }
        public readonly int Timestamp, PlayerID;
        public readonly short X, Y, Z;
        public readonly Block OldBlock, NewBlock;
    }


    static class BlockDB {
        public const int BlockDBEntrySize = 16;
        public static bool IsEnabled { get; private set; }
        static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds( 90 );

        internal static void Init() {
            Paths.TestDirectory( "BlockDB", Paths.BlockDBPath, true );
            Server.PlayerPlacedBlock += OnPlayerPlacedBlock;
            Scheduler.NewBackgroundTask( FlushAll ).RunForever( FlushInterval, FlushInterval );
            IsEnabled = true;
        }


        static void OnPlayerPlacedBlock( object sender, PlayerPlacedBlockEventArgs e ) {
            World world = e.Player.World;
            if( world.IsBlockTracked ) {
                BlockDBEntry newEntry = new BlockDBEntry( (int)DateTime.UtcNow.ToUnixTime(),
                                                          e.Player.Info.ID,
                                                          e.X, e.Y, e.Z,
                                                          e.OldBlock,
                                                          e.NewBlock );
                world.AddBlockDBEntry( newEntry );
            }
        }


        static void FlushAll( SchedulerTask task ) {
            lock( WorldManager.WorldListLock ) {
                foreach( World w in WorldManager.WorldList.Where( w => w.IsBlockTracked ) ) {
                    w.FlushBlockDB();
                }
            }
        }
    }
}