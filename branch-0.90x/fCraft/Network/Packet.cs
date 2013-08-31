// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Packet struct, just a wrapper for a byte array. </summary>
    public struct Packet {
        /// <summary> ID byte used in the protocol to indicate that an action should apply to self.
        /// When used in AddEntity packet, sets player's own respawn point.
        /// When used in Teleport packet, teleports the player. </summary>
        public const sbyte SelfId = -1;

        /// <summary> Raw bytes of this packet. </summary>
        public readonly byte[] Bytes;

        /// <summary> OpCode (first byte) of this packet. </summary>
        public OpCode OpCode {
            get { return (OpCode)Bytes[0]; }
        }


        /// <summary> Creates a new packet from given raw bytes. Data not be null. </summary>
        public Packet( [NotNull] byte[] rawBytes ) {
            if( rawBytes == null ) throw new ArgumentNullException( "rawBytes" );
            Bytes = rawBytes;
        }


        /// <summary> Creates a packet of correct size for a given opCode,
        /// and sets the first (opCode) byte. </summary>
        Packet( OpCode opCode ) {
            Bytes = new byte[PacketSizes[(int)opCode]];
            Bytes[0] = (byte)opCode;
        }


        #region Packet Making

        /// <summary> Creates a new Handshake (0x00) packet. </summary>
        /// <param name="serverName"> Server name, to be shown on recipient's loading screen. May not be null. </param>
        /// <param name="player"> Player to whom this packet is being sent.
        /// Used to determine DeleteAdmincrete permission, for client-side checks. May not be null. </param>
        /// <param name="motd"> Message-of-the-day (text displayed below the server name). May not be null. </param>
        /// <exception cref="ArgumentNullException"> player, serverName, or motd is null </exception>
        public static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            packet.Bytes[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            packet.Bytes[130] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }


        /// <summary> Creates a new SetBlockServer (0x06) packet. </summary>
        /// <param name="x"> X coordinate (horizontal, along width) of the block. </param>
        /// <param name="y"> Y coordinate (horizontal, along length) of the block. </param>
        /// <param name="z"> Z coordinate (vertical, along height) of the block. </param>
        /// <param name="type"> Block type to set at given coordinates. </param>
        public static Packet MakeSetBlock( short x, short y, short z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Bytes, 1 );
            ToNetOrder( z, packet.Bytes, 3 );
            ToNetOrder( y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        /// <summary> Creates a new SetBlockServer (0x06) packet. </summary>
        /// <param name="coords"> Coordinates of the block. </param>
        /// <param name="type"> Block type to set at given coordinates. </param>
        public static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( (short)coords.X, packet.Bytes, 1 );
            ToNetOrder( (short)coords.Z, packet.Bytes, 3 );
            ToNetOrder( (short)coords.Y, packet.Bytes, 5 );
            packet.Bytes[7] = (byte)type;
            return packet;
        }


        /// <summary> Creates a new AddEntity (0x07) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="name"> Entity name. May not be null. </param>
        /// <param name="spawnPosition"> Spawning position for the player. </param>
        /// <exception cref="ArgumentNullException"> name is null </exception>
        public static Packet MakeAddEntity( sbyte id, [NotNull] string name, Position spawnPosition ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Bytes[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( spawnPosition.X, packet.Bytes, 66 );
            ToNetOrder( spawnPosition.Z, packet.Bytes, 68 );
            ToNetOrder( spawnPosition.Y, packet.Bytes, 70 );
            packet.Bytes[72] = spawnPosition.R;
            packet.Bytes[73] = spawnPosition.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet. </summary>
        /// <param name="id"> Entity ID. Negative values refer to "self". </param>
        /// <param name="newPosition"> Position to teleport the entity to. </param>
        public static Packet MakeTeleport( sbyte id, Position newPosition ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Bytes[1] = (byte)id;
            ToNetOrder( newPosition.X, packet.Bytes, 2 );
            ToNetOrder( newPosition.Z, packet.Bytes, 4 );
            ToNetOrder( newPosition.Y, packet.Bytes, 6 );
            packet.Bytes[8] = newPosition.R;
            packet.Bytes[9] = newPosition.L;
            return packet;
        }


        /// <summary> Creates a new Teleport (0x08) packet, and sets ID to -1 ("self"). </summary>
        /// <param name="newPosition"> Position to teleport player to. </param>
        public static Packet MakeSelfTeleport( Position newPosition ) {
            return MakeTeleport( -1, newPosition.GetFixed() );
        }


        /// <summary> Creates a new MoveRotate (0x09) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="positionDelta"> Positioning information.
        /// Coordinates (X/Y/Z) should be relative and between -128 and 127.
        /// Rotation (R/L) should be absolute. </param>
        public static Packet MakeMoveRotate( sbyte id, Position positionDelta ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)( positionDelta.X & 0xFF );
            packet.Bytes[3] = (byte)( positionDelta.Z & 0xFF );
            packet.Bytes[4] = (byte)( positionDelta.Y & 0xFF );
            packet.Bytes[5] = positionDelta.R;
            packet.Bytes[6] = positionDelta.L;
            return packet;
        }


        /// <summary> Creates a new Move (0x0A) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="positionDelta"> Positioning information.
        /// Coordinates (X/Y/Z) should be relative and between -128 and 127. Rotation (R/L) is not sent. </param>
        public static Packet MakeMove( sbyte id, Position positionDelta ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = (byte)positionDelta.X;
            packet.Bytes[3] = (byte)positionDelta.Z;
            packet.Bytes[4] = (byte)positionDelta.Y;
            return packet;
        }


        /// <summary> Creates a new Rotate (0x0B) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="newPosition"> Positioning information.
        /// Rotation (R/L) should be absolute. Coordinates (X/Y/Z) are not sent. </param>
        public static Packet MakeRotate( sbyte id, Position newPosition ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Bytes[1] = (byte)id;
            packet.Bytes[2] = newPosition.R;
            packet.Bytes[3] = newPosition.L;
            return packet;
        }


        /// <summary> Creates a new RemoveEntity (0x0C) packet. </summary>
        /// <param name="id"> Entity ID. </param>
        public static Packet MakeRemoveEntity( sbyte id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Bytes[1] = (byte)id;
            return packet;
        }


        /// <summary> Creates a new Kick (0x0E) packet. </summary>
        /// <param name="reason"> Given reason. Only first 64 characters will be sent. May not be null. </param>
        /// <exception cref="ArgumentNullException"> reason is null </exception>
        public static Packet MakeKick( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            return packet;
        }


        /// <summary> Creates a new SetPermission (0x0F) packet. </summary>
        /// <param name="player"> Player to whom this packet is being sent.
        /// Used to determine DeleteAdmincrete permission, for client-side checks. May not be null. </param>
        /// <exception cref="ArgumentNullException"> player is null </exception>
        public static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Bytes[1] = (byte)( player.Can( Permission.DeleteAdmincrete ) ? 100 : 0 );
            return packet;
        }

        #endregion


        static void ToNetOrder( short number, byte[] arr, int offset ) {
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
        }


        static readonly int[] PacketSizes = {
            131, // Handshake
            1, // Ping
            1, // MapBegin
            1028, // MapChunk
            7, // MapEnd
            9, // SetBlockClient
            8, // SetBlockServer
            74, // AddEntity
            10, // Teleport
            7, // MoveRotate
            5, // Move
            4, // Rotate
            2, // RemoveEntity
            66, // Message
            65, // Kick
            2 // SetPermission
        };
    }
}