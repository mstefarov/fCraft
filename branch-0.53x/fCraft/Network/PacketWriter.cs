// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace fCraft {
    // Protocol encoder for outgoing packets
    public sealed class PacketWriter : BinaryWriter {

        public PacketWriter( Stream stream ) : base( stream ) { }


        #region Direct Writing

        public void Write( OutputCode opcode ) {
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

        public void Write( Packet packet ) {
            Write( packet.Data );
        }


        #region Direct Writing Whole Packets

        // below are builders for specific packet codes

        public void WritePing() {
            Write( OutputCode.Ping );
        }

        public void WriteLevelBegin() {
            Write( OutputCode.LevelBegin );
        }

        public void WriteLevelChunk( byte[] chunk, int chunkSize, byte progress ) {
            Write( OutputCode.LevelChunk );
            Write( (short)chunkSize );
            Write( chunk, 0, 1024 );
            Write( progress );
        }

        public void WriteAddEntity( byte id, Player player, Position pos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Write( OutputCode.AddEntity );
            Write( id );
            Write( player.GetListName() );
            Write( pos.X );
            Write( pos.H );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        public void WriteTeleport( byte id, Position pos ) {
            Write( OutputCode.Teleport );
            Write( id );
            Write( pos.X );
            Write( pos.H );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        #endregion

        #endregion


        #region Packet Making

        internal static Packet MakeHandshake( Player player, string serverName, string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( 131 );
            packet.Data[0] = (byte)OutputCode.Handshake;
            packet.Data[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Data, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Data, 66 );
            packet.Data[130] = player.GetOpPacketCode();
            return packet;
        }


        internal static Packet MakeLevelEnd( Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );

            Packet packet = new Packet( 7 );
            packet.Data[0] = (byte)OutputCode.LevelEnd;
            ToNetOrder( (short)map.WidthX, packet.Data, 1 );
            ToNetOrder( (short)map.Height, packet.Data, 3 );
            ToNetOrder( (short)map.WidthY, packet.Data, 5 );
            return packet;
        }


        internal static Packet MakeMessage( string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );

            Packet packet = new Packet( 66 );
            packet.Data[0] = (byte)OutputCode.Message;
            packet.Data[1] = 0;
            Encoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.Data, 2 );
            return packet;
        }


        internal static Packet MakeAddEntity( int id, string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( 74 );
            packet.Data[0] = (byte)OutputCode.AddEntity;
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

            Packet packet = new Packet( 65 );
            packet.Data[0] = (byte)OutputCode.Disconnect;
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Data, 1 );
            return packet;
        }


        internal static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( 2 );
            packet.Data[0] = (byte)OutputCode.RemoveEntity;
            packet.Data[1] = (byte)id;
            return packet;
        }


        internal static Packet MakeTeleport( int id, Position pos ) {
            Packet packet = new Packet( 10 );
            packet.Data[0] = (byte)OutputCode.Teleport;
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
            Packet packet = new Packet( 7 );
            packet.Data[0] = (byte)OutputCode.MoveRotate;
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)(pos.X & 0xFF);
            packet.Data[3] = (byte)(pos.H & 0xFF);
            packet.Data[4] = (byte)(pos.Y & 0xFF);
            packet.Data[5] = pos.R;
            packet.Data[6] = pos.L;
            return packet;
        }


        internal static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( 5 );
            packet.Data[0] = (byte)OutputCode.Move;
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)pos.X;
            packet.Data[3] = (byte)pos.H;
            packet.Data[4] = (byte)pos.Y;
            return packet;
        }


        internal static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( 4 );
            packet.Data[0] = (byte)OutputCode.Rotate;
            packet.Data[1] = (byte)id;
            packet.Data[2] = pos.R;
            packet.Data[3] = pos.L;
            return packet;
        }


        internal static Packet MakeSetBlock( int x, int y, int h, byte type ) {
            Packet packet = new Packet( 8 );
            packet.Data[0] = (byte)OutputCode.SetBlock;
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( h, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = type;
            return packet;
        }


        internal static Packet MakeSetBlock( int x, int y, int h, Block type ) {
            Packet packet = new Packet( 8 );
            packet.Data[0] = (byte)OutputCode.SetBlock;
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( h, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }


        internal static Packet MakeSetPermission( Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( 2 );
            packet.Data[0] = (byte)OutputCode.SetPermission;
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


        internal static readonly string[] NewlineSplitter = new[] { "&N" };

        internal static IEnumerable<Packet> MakeWrappedMessage( string prefix, string text, bool appendPrefixToFirstLine ) {
            if( appendPrefixToFirstLine ) text = prefix + text;

            /* STEP 1: Split by lines */
            if( text.Contains( "&N" ) ) {
                bool first = true;
                foreach( string subline in text.Split( NewlineSplitter, StringSplitOptions.None ) ) {
                    foreach( Packet p in MakeWrappedMessage( prefix, subline, !first ) ) {
                        yield return p;
                    }
                    first = false;
                }
                yield break;
            }

            /* STEP 2: Replace special colorcodes */
            text = Color.SubstituteSpecialColors( text );

            /* STEP 3: Remove consecutive colorcodes */
            for( int i = 0; i < text.Length - 3; i++ ) {
                if( text[i] == '&' && IsColorCode( text[i + 1] ) && text[i + 2] == '&' && IsColorCode( text[i + 3] ) ) {
                    text = text.Substring( 0, i ) + text.Substring( i + 2 );
                    i--;
                }
            }

            /* STEP 4: Split */
            int lastIndex = 0;

            List<string> segments = new List<string>();
            for( int i = 0; i < text.Length; i++ ) {
                if( IsColorCode( text[i] ) && i > 0 && text[i - 1] == '&' ) {
                    // split at color codes
                    if( i > 1 ) {
                        segments.Add( text.Substring( lastIndex, i - lastIndex - 1 ) );
                        lastIndex = i - 1;
                    }

                } else if( text[i] == ' ' ) {
                    for( ; i < text.Length && text[i] == ' '; i++ ) { }
                    i--;
                    // split at spaces
                    segments.Add( text.Substring( lastIndex, i - lastIndex ) );
                    lastIndex = i;
                }
            }

            // add remainder of the string
            if( lastIndex != text.Length ) {
                segments.Add( text.Substring( lastIndex ) );
            }


            /* STEP 5: Delete empty segments */
            for( int i = segments.Count - 1; i >= 0; i-- ) {
                if( segments[i].Length == 0 ) segments.RemoveAt( i );
            }


            /* STEP 6: Join segments into strings */
            string line = "";
            string lastColorCode = "";
            List<string> lines = new List<string>();

            for( int i = 0; i < segments.Count; i++ ) {
                if( line.Length + segments[i].TrimEnd().Length + 1 > 64 ) {
                    // end of line, start new one
                    lines.Add( line );

                    if( segments[i].TrimStart().StartsWith( "&" ) ) {
                        lastColorCode = segments[i].Substring( 0, 2 );
                        line = prefix + segments[i].TrimStart();

                    } else {
                        line = prefix + lastColorCode + segments[i].TrimStart();
                    }
                } else {
                    // apending to line
                    if( segments[i].TrimStart().StartsWith( "&" ) ) {
                        lastColorCode = segments[i].Substring( 0, 2 );
                        line += segments[i];
                    } else {
                        line += segments[i];
                    }
                }
            }

            // last line
            lines.Add( line );


            /* STEP 7: Remove trailing whitespace and colorcodes */
            for( int l = lines.Count - 1; l >= 0; l-- ) {
                int i = lines[l].Length - 1;
                for( ; i >= 0 && (lines[l][i] == ' ' || lines[l][i] == '&' || IsColorCode( lines[l][i] ) && i > 0 && lines[l][i - 1] == '&'); i-- ) { }
                if( i == 0 ) {
                    lines.RemoveAt( l );
                } else {
                    lines[l] = lines[l].Substring( 0, i + 1 );
                }
            }

            /* STEP 8: DONE */
            foreach( string processedLine in lines ) {
                yield return MakeMessage( processedLine );
            }
        }


        static bool IsColorCode( char c ) {
            return (c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');
        }
    }
}