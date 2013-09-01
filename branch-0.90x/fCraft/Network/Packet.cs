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


        #region Making regular packets

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


        #region Making extended packets

        [Pure]
        public static Packet MakeExtInfo( short extCount ) {
            Packet packet = new Packet( OpCode.ExtInfo );
            Encoding.ASCII.GetBytes( "fCraft " + Updater.CurrentRelease.VersionString, 0, 64, packet.Bytes, 1 );
            ToNetOrder( extCount, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtEntry( [NotNull] string name, int version ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Packet packet = new Packet( OpCode.ExtEntry );
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            ToNetOrder( version, packet.Bytes, 65 );
            return packet;
        }


        [Pure]
        public static Packet MakeSetClickDistance( short distance ) {
            if( distance < 0 ) throw new ArgumentOutOfRangeException( "distance" );
            Packet packet = new Packet( OpCode.SetClickDistance );
            ToNetOrder( distance, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            Packet packet = new Packet( OpCode.CustomBlockSupportLevel );
            packet.Bytes[1] = level;
            return packet;
        }


        [Pure]
        public static Packet MakeHoldThis( Block block, bool preventChange ) {
            Packet packet = new Packet( OpCode.HoldThis );
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(preventChange ? 1 : 0);
            return packet;
        }


        [Pure]
        public static Packet MakeSetTextHotKey( [NotNull] string label, [NotNull] string action, int keyCode, byte keyMods ) {
            if( label == null ) throw new ArgumentNullException( "label" );
            if( action == null ) throw new ArgumentNullException( "action" );
            Packet packet = new Packet( OpCode.SetTextHotKey );
            Encoding.ASCII.GetBytes( label.PadRight( 64 ), 0, 64, packet.Bytes, 1 );
            Encoding.ASCII.GetBytes( action.PadRight( 64 ), 0, 64, packet.Bytes, 65 );
            ToNetOrder( keyCode, packet.Bytes, 129 );
            packet.Bytes[133] = keyMods;
            return packet;
        }


        [Pure]
        public static Packet MakeExtAddPlayerName( short nameId, string playerName, string listName, string groupName, byte groupRank ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            if( listName == null ) throw new ArgumentNullException( "listName" );
            if( groupName == null ) throw new ArgumentNullException( "groupName" );
            Packet packet = new Packet( OpCode.ExtAddPlayerName );
            ToNetOrder( nameId, packet.Bytes, 1 );
            Encoding.ASCII.GetBytes( playerName.PadRight( 64 ), 0, 64, packet.Bytes, 3 );
            Encoding.ASCII.GetBytes( listName.PadRight( 64 ), 0, 64, packet.Bytes, 67 );
            Encoding.ASCII.GetBytes( groupName.PadRight( 64 ), 0, 64, packet.Bytes, 131 );
            packet.Bytes[195] = groupRank;
            return packet;
        }


        [Pure]
        public static Packet ExtAddEntity( byte entityId, [NotNull] string inGameName, [NotNull] string skinName ) {
            if( inGameName == null ) throw new ArgumentNullException( "inGameName" );
            if( skinName == null ) throw new ArgumentNullException( "skinName" );
            Packet packet = new Packet( OpCode.ExtAddEntity );
            packet.Bytes[1] = entityId;
            Encoding.ASCII.GetBytes( inGameName.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            Encoding.ASCII.GetBytes( skinName.PadRight( 64 ), 0, 64, packet.Bytes, 66 );
            return packet;
        }


        [Pure]
        public static Packet MakeExtRemovePlayerName( short nameId ) {
            Packet packet = new Packet( OpCode.ExtRemovePlayerName );
            ToNetOrder( nameId, packet.Bytes, 1 );
            return packet;
        }


        [Pure]
        public static Packet MakeEnvSetColor( EnvVariable variable, int color ) {
            Packet packet = new Packet( OpCode.EnvSetColor );
            packet.Bytes[1] = (byte)variable;
            packet.Bytes[2] = (byte)((color >> 16) & 0xFF);
            packet.Bytes[3] = (byte)((color >> 8) & 0xFF);
            packet.Bytes[4] = (byte)(color & 0xFF);
            return packet;
        }


        [Pure]
        public static Packet MakeMakeSelection( byte selectionId, [NotNull] string label, [NotNull] BoundingBox bounds, int color, byte opacity ) {
            if( label == null ) throw new ArgumentNullException( "label" );
            if( bounds == null ) throw new ArgumentNullException( "bounds" );
            Packet packet = new Packet( OpCode.MakeSelection );
            packet.Bytes[1] = selectionId;
            Encoding.ASCII.GetBytes( label.PadRight( 64 ), 0, 64, packet.Bytes, 2 );
            ToNetOrder( bounds.XMin, packet.Bytes, 66 );
            ToNetOrder( bounds.ZMin, packet.Bytes, 68 );
            ToNetOrder( bounds.YMin, packet.Bytes, 70 );
            ToNetOrder( bounds.XMax, packet.Bytes, 72 );
            ToNetOrder( bounds.ZMax, packet.Bytes, 74 );
            ToNetOrder( bounds.YMax, packet.Bytes, 76 );
            packet.Bytes[78] = (byte)((color >> 16) & 0xFF);
            packet.Bytes[79] = (byte)((color >> 8) & 0xFF);
            packet.Bytes[81] = (byte)(color & 0xFF);
            packet.Bytes[82] = opacity;
            return packet;
        }


        [Pure]
        public static Packet MakeRemoveSelection( byte selectionId ) {
            Packet packet = new Packet( OpCode.RemoveSelection );
            packet.Bytes[1] = selectionId;
            return packet;
        }


        [Pure]
        public static Packet MakeSetBlockPermission( Block block, bool canPlace, bool canDelete ) {
            Packet packet = new Packet( OpCode.SetBlockPermission );
            packet.Bytes[1] = (byte)block;
            packet.Bytes[2] = (byte)(canPlace ? 1 : 0);
            packet.Bytes[3] = (byte)(canDelete ? 1 : 0);
            return packet;
        }

        #endregion


        static void ToNetOrder( short number, [NotNull] byte[] arr, int offset ) {
            arr[offset] = (byte)( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = (byte)( number & 0x00ff );
        }

        static void ToNetOrder( int number, [NotNull] byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff000000) >> 24);
            arr[offset + 1] = (byte)((number & 0x00ff0000) >> 16);
            arr[offset + 2] = (byte)((number & 0x0000ff00) >> 8);
            arr[offset + 3] = (byte)(number & 0x000000ff);
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
            2, // SetPermission
            67, // ExtInfo
            69, // ExtEntry
            3, // SetClickDistance
            2, // CustomBlockSupportLevel
            2, // HoldThis
            134, // SetTextHotKey
            196, // ExtAddPlayerName
            130, // ExtAddEntity
            3, // ExtRemovePlayerName
            5, // EnvSetColor
            82, // MakeSelection
            2, // RemoveSelection
            4 // SetBlockPermission
        };
    }
}