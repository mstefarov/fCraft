using System;

namespace fCraft {
    class StandardCommands {
        public static void Init(){
            Commands.Add( "ban", Ban );
            Commands.Add( "banip", Ban );
            Commands.Add( "k", Kick );
            Commands.Add( "kick", Kick );
        }

        static void Ban( Player player, string message ) {
            if( player.Can( Permissions.Ban ) ) {
                string[] tokens = Commands.Tokenize( message, 2 );
                string name = tokens[1];
                Player offender = World.FindPlayer( name );

                if( offender != null ) {
                    offender.info.banned = true;
                    offender.info.banDate = DateTime.Now;
                    offender.info.bannedBy = player.name;
                    offender.session.Kick( "You have been kicked by " + player.name );
                    World.SendToAll( PacketWriter.MakeSysMessage( offender.name + " was banned by " + player.name ), offender );
                } else {
                    World.NoPlayerMessage( player, name );
                }

                // TODO: add ip ban
            }
        }

        static void Kick( Player player, string message ) {
            if( player.Can( Permissions.Kick ) ) {
                string[] tokens = Commands.Tokenize( message, 2 );
                string name = tokens[1];
                Player offender = World.FindPlayer( name );
                if( offender != null ) {
                    offender.session.Kick( "You have been kicked by " + player.name );
                    World.SendToAll( PacketWriter.MakeSysMessage( offender.name + " was kicked by " + player.name ), offender );
                } else {
                    World.NoPlayerMessage( player, name );
                }
            }
        }
    }
}
