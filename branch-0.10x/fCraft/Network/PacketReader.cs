using System;
using System.IO;
using System.Text;

namespace fCraft {
    // Protocol decoder for incoming packets
    sealed class PacketReader {
        
        public uint opcode;
        private MemoryStream stream;
        private BinaryReader reader;

        public PacketReader( BinaryReader netReader ) {
            Packet packet = new Packet( netReader );
            opcode = packet.opcode;
            stream = new MemoryStream( packet.data );
            reader = new BinaryReader( stream );
        }

        public PacketReader( Packet packet ) {
            opcode = packet.opcode;
            stream = new MemoryStream( packet.data );
            reader = new BinaryReader( stream );
        }

        public byte ReadByte() {
            return reader.ReadByte();
        }

        public short ReadShort() {
            return reader.ReadInt16();
        }

        public int ReadInt() {
            return reader.ReadInt32();
        }

        public long ReadLong() {
            return reader.ReadInt64();
        }

        public string ReadString() {
            return ASCIIEncoding.ASCII.GetString( reader.ReadBytes( 64 ) ).Trim();
        }

        public void ReadData( out byte[] data ) {
            data = reader.ReadBytes( 1024 );
        }

        ~PacketReader() {
            if( reader != null ) reader.Close();
        }
    }
}
