using System;
using System.IO;
using System.Text;

namespace fCraft {
    // Protocol encoder for outgoing packets
    sealed class PacketWriter {
        private MemoryStream stream;
        private BinaryWriter writer;
        private Packet packet;

        public PacketWriter( uint opcode ) {
            packet = new Packet( opcode );
            stream = new MemoryStream( packet.data );
            writer = new BinaryWriter( stream );
            writer.Write( (byte)opcode );
        }

        public void Write( byte data ) {
            writer.Write( data );
        }

        public void Write( short data ) {
            writer.Write( data );
        }

        public void Write( int data ) {
            writer.Write( data );
        }

        public void Write( long data ) {
            writer.Write( data );
        }

        public void Write( string data ) {
            writer.Write( ASCIIEncoding.ASCII.GetBytes( data.PadLeft( 64 ) ) );
        }

        public void Write( byte[] data ) {
            writer.Write( data );
        }

        public Packet GetPacket() {
            return packet;
        }

        ~PacketWriter() {
            if( writer != null ) writer.Close();
        }

        // Opcode-specific packet builders
        public static Packet MakeDisconnectPacket( string reason ) {
            PacketWriter pw = new PacketWriter( (uint)OutputCodes.Disconnect );
            pw.Write( reason );
            return pw.GetPacket();
        }

        public static Packet MakeHandshakePacket( Player player ) {
            PacketWriter pw = new PacketWriter( (uint)OutputCodes.Handshake );
            pw.Write( (byte)Config.ProtocolVersion );
            pw.Write( Config.ServerName );
            pw.Write( Config.MOTD );
            pw.Write( player.GetPlayerClassCode() );
            return pw.GetPacket();
        }
    }
}
