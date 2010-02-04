using System;
using System.Text.RegularExpressions;

namespace fCraft {
    sealed class Player {
        public string name, coloredName;
        public Session session;
        public PlayerInfo info;
        public int id;

        public object locker;

        public Position pos, newPos;

        public Player( string _name, Session _session ) {
            locker = new object();
            name = _name;
            session = _session;
            info = PlayerDB.GetPlayerInfo( this );
            coloredName = "&" + info.playerClass.color + name + Color.Reset();
        }

        public static bool IsValidName( string name ) {
            Match match = Regex.Match( name, "^\\w{1,16}$" );
            return match.Success;
        }

        public byte GetPlayerClassCode() {
            return 100;
        }

        public void Move( int _x, int _y, int _h, int _rotation, int _look ) {
            newPos.Set( _x, _y, _h,_rotation, _look );
        }

        public void SetTile( short x, short y, short h, byte mode, byte type ) {
            if( mode == 1 && info.playerClass.IsAllowed( Permissions.Build ) ) {
                World.level.QueueUpdate( new BlockUpdate( this, x, y, h, type ) );
            }else if( mode == 0 && info.playerClass.IsAllowed(Permissions.Delete)){
                World.level.QueueUpdate( new BlockUpdate( this, x, y, h, 0 ) );
            }else{
                SysMessage( "Not allowed!" );
            }
        }

        public bool Can( Permissions permission ) {
            return info.playerClass.IsAllowed( permission );
        }

        public void Message( Player player, string message ) {
            session.Send( PacketWriter.MakeMessage( player.id, message ) );
        }

        public void SysMessage( string message ) {
            session.Send( PacketWriter.MakeSysMessage( message ) );
        }
    }
}
