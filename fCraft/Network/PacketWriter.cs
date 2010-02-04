using System;
using System.IO;
using System.Text;
using System.Net;

namespace fCraft {
    // Protocol encoder for outgoing packets
    sealed class PacketWriter {
        private BinaryWriter writer;

        public PacketWriter( BinaryWriter _writer ) {
            writer = _writer;
        }

        public void Write( OutputCodes opcode ) {
            writer.Write( (byte)opcode );
        }

        public void Write( byte data ) {
            writer.Write( data );
        }

        public void Write( sbyte data ) {
            writer.Write( data );
        }

        public void Write( short data ) {
            writer.Write( SwapBytes( data ) );
        }

        public void Write( int data ) {
            writer.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        public void Write( string data ) {
            writer.Write( ASCIIEncoding.ASCII.GetBytes( data.PadRight( 64 ).Substring( 0, 64 ) ) );
        }

        public void Write( byte[] data ) {
            writer.Write( data );
        }


        // below are builders for specific packet codes
        public void WriteDisconnect( string reason ) {
            Write( OutputCodes.Disconnect );
            Write( reason );
        }

        public void WritePing() {
            Write( OutputCodes.Ping );
            Write( (byte)1 );
        }

        public void WriteHandshake( Player player ) {
            Write( OutputCodes.Handshake );
            Write( (byte)Config.ProtocolVersion );
            Write( Config.GetString( "ServerName" ) );
            Write( Config.GetString( "MOTD" ) );
            Write( (byte)0 );
        }

        public void WriteLevelBegin() {
            Write( OutputCodes.LevelBegin );
        }

        public void WriteLevelChunk( byte[] chunk, int chunkSize, byte progress ) {
            Write( OutputCodes.LevelChunk );
            Write( (short)chunkSize );
            writer.Write( chunk, 0, 1024 );
            Write( (byte)progress );
        }

        public void WriteLevelEnd() {
            Write( OutputCodes.LevelEnd );
            Write( (short)World.level.widthX );
            Write( (short)World.level.height );
            Write( (short)World.level.widthY );
        }

        public void WriteAddEntity( byte id, string name, Position pos ) {
            Write( OutputCodes.AddEntity );
            Write( id );
            Write( name );
            Write( (short)pos.x );
            Write( (short)pos.h );
            Write( (short)pos.y );
            Write( (sbyte)pos.r );
            Write( (sbyte)pos.l );
        }

        public void WriteTeleport( byte id, Position pos ) {
            Write( OutputCodes.Teleport );
            Write( id );
            Write( (short)pos.x );
            Write( (short)pos.h );
            Write( (short)pos.y );
            Write( (sbyte)pos.r );
            Write( (sbyte)pos.l );
        }

        public void WriteMessage( byte id, string message ) {
            Write( OutputCodes.Message );
            Write( id );
            Write( message );
        }

        public static Packet MakeMessage( int id, string message ) {
            Packet packet = new Packet( 66 );
            packet.data[0] = (byte)OutputCodes.Message;
            packet.data[1] = (byte)id;
            ASCIIEncoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.data, 2 );
            return packet;
        }

        public static Packet MakeSysMessage( string message ) {
            Packet packet = new Packet( 66 );
            packet.data[0] = (byte)OutputCodes.Message;
            packet.data[1] = 0;
            ASCIIEncoding.ASCII.GetBytes( ("&e" + message).PadRight( 64 ), 0, 64, packet.data, 2 );
            return packet;
        }

        public static Packet MakeAddEntity( Player player, Position pos ) {
            Packet packet = new Packet( 74 );
            packet.data[0] = (byte)OutputCodes.AddEntity;
            packet.data[1] = (byte)player.id;
            ASCIIEncoding.ASCII.GetBytes( player.name.PadRight( 64 ), 0, 64, packet.data, 2 );
            ToNetOrder( pos.x, packet.data, 66 );
            ToNetOrder( pos.h, packet.data, 68 );
            ToNetOrder( pos.y, packet.data, 70 );
            packet.data[72] = (byte)pos.r;
            packet.data[73] = (byte)pos.l;
            return packet;
        }

        public static Packet MakeDisconnect( string reason ) {
            Packet packet = new Packet( 65 );
            packet.data[0] = (byte)OutputCodes.Disconnect;
            ASCIIEncoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.data, 1 );
            return packet;
        }

        public static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( 2 );
            packet.data[0] = (byte)OutputCodes.RemoveEntity;
            packet.data[1] = (byte)id;
            return packet;
        }

        public static Packet MakeTeleport( Player player, Position pos ) {
            Packet packet = new Packet( 10 );
            packet.data[0] = (byte)OutputCodes.Teleport;
            packet.data[1] = (byte)player.id;
            ToNetOrder( pos.x, packet.data, 2 );
            ToNetOrder( pos.h, packet.data, 4 );
            ToNetOrder( pos.y, packet.data, 6 );
            packet.data[8] = (byte)pos.r;
            packet.data[9] = (byte)pos.l;
            return packet;
        }

        public static Packet MakeSetBlock( int x, int y, int h, byte type ) {
            Packet packet = new Packet( 8 );
            packet.data[0] = (byte)OutputCodes.SetTile;
            ToNetOrder( x, packet.data, 1 );
            ToNetOrder( h, packet.data, 3 );
            ToNetOrder( y, packet.data, 5 );
            packet.data[7] = type;
            return packet;
        }

        public void Flush() {
            writer.Flush();
        }

        public void Close() {
            writer.Close();
        }


        public static void ToNetOrder( int number, byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 1] = (byte)(number & 0x000000ff);
        }

        public static short SwapBytes( short number ) {
            return unchecked( (short)(((number & 0xff00) >> 8) | ((number & 0x00ff) << 8)) );
        }
    }
}