using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fCraft.PlayerDBMigrationTool {
    class Program {
        static void Main( string[] args ) {
            Console.WriteLine( "PlayerDBMigration: Make sure that no fCraft processes are running. " +
                               "Server must be fully shut down before migration. " +
                               "Press <Enter> when you are ready." );
            Console.WriteLine( "PlayerDBMigration: Initializing fCraft..." );
            Console.ReadLine();
            Server.InitLibrary( args );
            if( !Config.Load( false, false ) ) {
                Console.WriteLine( "PlayerDBMigration: Failed to load config." );
                return;
            }

            Console.WriteLine( "Current provider type is: {0}", PlayerDB.ProviderType );
            Console.WriteLine( "1. Migrate to MySQL" );
            Console.WriteLine( "2. Migrate to File" );
            Console.WriteLine( "3. Cancel" );
        }

        static void MigrateToMySQL() {
        }

        static void MigrateToFile() {
        }
    }
}