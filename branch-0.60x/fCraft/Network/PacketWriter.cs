// Part of fCraft | Copyright (c) 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    sealed class PacketWriter : BinaryWriter {
        public PacketWriter( [NotNull] Stream stream )
            : base( stream ) { }


        public void Write( OpCode opcode ) {
            Write( (byte)opcode );
        }


        public override void Write( short data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }


        public override void Write( string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 64 ) throw new ArgumentException( "String is too long (>64).", "str" );
            Write( Encoding.ASCII.GetBytes( str.PadRight( 64 ) ) );
        }
    }
}