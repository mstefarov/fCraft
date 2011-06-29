// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Text;

namespace fCraft {
    // Protocol encoder for outgoing packets
    public sealed class PacketWriter : BinaryWriter {

        public PacketWriter( Stream stream ) : base( stream ) { }


        #region Direct Writing

        public void Write( OpCode opcode ) {
            Write( (byte)opcode );
        }

        public override void Write( short data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        public override void Write( int data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        public override void Write( string data ) {
            Write( Encoding.ASCII.GetBytes( data.PadRight( 64 ).Substring( 0, 64 ) ) );
        }

        #endregion


        #region Direct Writing Whole Packets

        // below are builders for specific packet codes

        public void WritePing() {
            Write( OpCode.Ping );
        }

        public void WriteLevelBegin() {
            Write( OpCode.MapBegin );
        }

        public void WriteLevelChunk( byte[] chunk, int chunkSize, byte progress ) {
            if( chunk == null ) throw new ArgumentNullException( "chunk" );
            Write( OpCode.MapChunk );
            Write( (short)chunkSize );
            Write( chunk, 0, 1024 );
            Write( progress );
        }

        internal void WriteLevelEnd( Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            Write( OpCode.MapEnd );
            Write( (short)map.WidthX );
            Write( (short)map.Height );
            Write( (short)map.WidthY );
        }

        public void WriteAddEntity( byte id, Player player, Position pos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Write( OpCode.AddEntity );
            Write( id );
            Write( player.ListName );
            Write( pos.X );
            Write( pos.H );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        public void WriteTeleport( byte id, Position pos ) {
            Write( OpCode.Teleport );
            Write( id );
            Write( pos.X );
            Write( pos.H );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        #endregion


        #region Packet Making

        internal static Packet MakeHandshake( Player player, string serverName, string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            packet.Data[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Data, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Data, 66 );
            packet.Data[130] = player.GetOpPacketCode();
            return packet;
        }


        internal static Packet MakeMessage( string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );

            Packet packet = new Packet( OpCode.Message );
            packet.Data[1] = 0;
            Encoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.Data, 2 );
            return packet;
        }


        internal static Packet MakeAddEntity( int id, string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Data[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Data, 2 );
            ToNetOrder( pos.X, packet.Data, 66 );
            ToNetOrder( pos.H, packet.Data, 68 );
            ToNetOrder( pos.Y, packet.Data, 70 );
            packet.Data[72] = pos.R;
            packet.Data[73] = pos.L;
            return packet;
        }


        internal static Packet MakeDisconnect( string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Data, 1 );
            return packet;
        }


        internal static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Data[1] = (byte)id;
            return packet;
        }


        internal static Packet MakeTeleport( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Data[1] = (byte)id;
            ToNetOrder( pos.X, packet.Data, 2 );
            ToNetOrder( pos.H, packet.Data, 4 );
            ToNetOrder( pos.Y, packet.Data, 6 );
            packet.Data[8] = pos.R;
            packet.Data[9] = pos.L;
            return packet;
        }


        internal static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( 255, pos.GetFixed() );
        }


        internal static Packet MakeMoveRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)(pos.X & 0xFF);
            packet.Data[3] = (byte)(pos.H & 0xFF);
            packet.Data[4] = (byte)(pos.Y & 0xFF);
            packet.Data[5] = pos.R;
            packet.Data[6] = pos.L;
            return packet;
        }


        internal static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)pos.X;
            packet.Data[3] = (byte)pos.H;
            packet.Data[4] = (byte)pos.Y;
            return packet;
        }


        internal static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = pos.R;
            packet.Data[3] = pos.L;
            return packet;
        }


        internal static Packet MakeSetBlock( int x, int y, int h, byte type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( h, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = type;
            return packet;
        }


        internal static Packet MakeSetBlock( int x, int y, int h, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( h, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }


        internal static Packet MakeSetPermission( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Data[1] = player.GetOpPacketCode();
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