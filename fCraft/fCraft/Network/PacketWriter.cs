// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Text;
using System.Net;


namespace fCraft {
    // Protocol encoder for outgoing packets
    sealed class PacketWriter : BinaryWriter {

        public PacketWriter( Stream stream ) : base( stream ) { }

        #region Direct Writing
        public void Write( OutputCodes opcode ) {
            Write( (byte)opcode );
        }

        public override void Write( short data ) {
            base.Write( Server.SwapBytes( data ) );
        }

        public override void Write( int data ) {
            Write( IPAddress.HostToNetworkOrder( data ) );
        }

        public override void Write( string data ) {
            Write( Encoding.ASCII.GetBytes( data.PadRight( 64 ).Substring( 0, 64 ) ) );
        }

        public void Write( Packet packet ) {
            Write( packet.data );
        }


        #region Direct Writing Whole Packets

        // below are builders for specific packet codes
        public void WriteDisconnect( string reason ) {
            Write( OutputCodes.Disconnect );
            Write( reason );
        }

        public void WritePing() {
            Write( OutputCodes.Ping );
        }

        public void WriteLevelBegin() {
            Write( OutputCodes.LevelBegin );
        }

        public void WriteLevelChunk( byte[] chunk, int chunkSize, byte progress ) {
            Write( OutputCodes.LevelChunk );
            Write( (short)chunkSize );
            Write( chunk, 0, 1024 );
            Write( (byte)progress );
        }

        public void WriteAddEntity( byte id, Player player, Position pos ) {
            Write( OutputCodes.AddEntity );
            Write( id );
            Write( player.GetListName() );
            Write( (short)pos.x );
            Write( (short)pos.h );
            Write( (short)pos.y );
            Write( pos.r );
            Write( pos.l );
        }

        public void WriteTeleport( byte id, Position pos ) {
            Write( OutputCodes.Teleport );
            Write( id );
            Write( (short)pos.x );
            Write( (short)pos.h );
            Write( (short)pos.y );
            Write( pos.r );
            Write( pos.l );
        }
        #endregion

        #endregion

        #region Packet Making

        internal static Packet MakeHandshake( Player player, string serverName, string MOTD ) {
            Packet packet = new Packet( 131 );
            packet.data[0] = (byte)OutputCodes.Handshake;
            packet.data[1] = (byte)Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.data, 2 );
            Encoding.ASCII.GetBytes( MOTD.PadRight( 64 ), 0, 64, packet.data, 66 );
            packet.data[130] = (byte)player.GetOPPacketCode();
            return packet;
        }


        internal static Packet MakeLevelEnd( Map map ) {
            Packet packet = new Packet( 7 );
            packet.data[0] = (byte)OutputCodes.LevelEnd;
            ToNetOrder( (short)map.widthX, packet.data, 1 );
            ToNetOrder( (short)map.height, packet.data, 3 );
            ToNetOrder( (short)map.widthY, packet.data, 5 );
            return packet;
        }

        internal static Packet MakeMessage( string message ) {
            Packet packet = new Packet( 66 );
            packet.data[0] = (byte)OutputCodes.Message;
            packet.data[1] = 0;
            Encoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.data, 2 );
            return packet;
        }

        internal static Packet MakeAddEntity( Player player, Position pos ) {
            Packet packet = new Packet( 74 );
            packet.data[0] = (byte)OutputCodes.AddEntity;
            packet.data[1] = (byte)player.id;
            Encoding.ASCII.GetBytes( player.GetListName().PadRight( 64 ), 0, 64, packet.data, 2 );
            ToNetOrder( pos.x, packet.data, 66 );
            ToNetOrder( pos.h, packet.data, 68 );
            ToNetOrder( pos.y, packet.data, 70 );
            packet.data[72] = pos.r;
            packet.data[73] = pos.l;
            return packet;
        }

        internal static Packet MakeDisconnect( string reason ) {
            Packet packet = new Packet( 65 );
            packet.data[0] = (byte)OutputCodes.Disconnect;
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.data, 1 );
            return packet;
        }

        internal static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( 2 );
            packet.data[0] = (byte)OutputCodes.RemoveEntity;
            packet.data[1] = (byte)id;
            return packet;
        }

        internal static Packet MakeTeleport( int id, Position pos ) {
            Packet packet = new Packet( 10 );
            packet.data[0] = (byte)OutputCodes.Teleport;
            packet.data[1] = (byte)id;
            ToNetOrder( pos.x, packet.data, 2 );
            ToNetOrder( pos.h, packet.data, 4 );
            ToNetOrder( pos.y, packet.data, 6 );
            packet.data[8] = pos.r;
            packet.data[9] = pos.l;
            return packet;
        }

        internal static Packet MakeMoveRotate( int id, Position pos ) {
            Packet packet = new Packet( 7 );
            packet.data[0] = (byte)OutputCodes.MoveRotate;
            packet.data[1] = (byte)id;
            packet.data[2] = (byte)(pos.x&0xFF);
            packet.data[3] = (byte)(pos.h&0xFF);
            packet.data[4] = (byte)(pos.y&0xFF);
            packet.data[5] = pos.r;
            packet.data[6] = pos.l;
            return packet;
        }

        internal static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( 5 );
            packet.data[0] = (byte)OutputCodes.Move;
            packet.data[1] = (byte)id;
            packet.data[2] = (byte)pos.x;
            packet.data[3] = (byte)pos.h;
            packet.data[4] = (byte)pos.y;
            return packet;
        }

        internal static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( 4 );
            packet.data[0] = (byte)OutputCodes.Rotate;
            packet.data[1] = (byte)id;
            packet.data[2] = pos.r;
            packet.data[3] = pos.l;
            return packet;
        }

        internal static Packet MakeSetBlock( int x, int y, int h, byte type ) {
            Packet packet = new Packet( 8 );
            packet.data[0] = (byte)OutputCodes.SetTile;
            ToNetOrder( x, packet.data, 1 );
            ToNetOrder( h, packet.data, 3 );
            ToNetOrder( y, packet.data, 5 );
            packet.data[7] = type;
            return packet;
        }

        internal static Packet MakeSetPermission( Player player ) {
            Packet packet = new Packet( 2 );
            packet.data[0] = (byte)OutputCodes.SetPermission;
            packet.data[1] = player.GetOPPacketCode();
            return packet;
        }

        #endregion

        #region Utilities

        internal static void ToNetOrder( int number, byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)(number & 0x00ff);
        }

        #endregion
    }
}