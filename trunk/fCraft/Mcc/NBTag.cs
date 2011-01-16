// 
//  Authors:
//   *  Tyler Kennedy <tk@tkte.ch>
//   *  Matvei Stefarov <fragmer@gmail.com>
// 
//  Copyright (c) 2010-2011, Tyler Kennedy & Matvei Stefarov
// 
//  All rights reserved.
// 
//  Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
// 
//     * Redistributions of source code must retain the above copyright notice, this
//       list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice,
//       this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of MCC nor the names of its contributors may be
//       used to endorse or promote products derived from this software without
//       specific prior written permission.
// 
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;


namespace Mcc {
    public enum NBTType : byte {
        End,
        Byte,
        Short,
        Int,
        Long,
        Float,
        Double,
        Bytes,
        String,
        List,
        Compound
    }


    public class NBTag : IEnumerable<NBTag> {
        public NBTType Type { get; set; }
        public string Name { get; set; }
        public object Payload { get; set; }
        public NBTag Parent { get; set; }

        #region Constructors
        public NBTag() { }

        public NBTag( NBTType _type, NBTag _parent ) {
            Type = _type;
            Parent = _parent;
        }

        public NBTag( NBTType _type, string _name, object _payload, NBTag _parent ) {
            Type = _type;
            Name = _name;
            Payload = _payload;
            Parent = _parent;
        }

        #endregion

        #region Shorthand Contructors
        public NBTag Append( NBTag tag ) {
            if( this is NBTCompound ) {
                tag.Parent = this;
                ((NBTCompound)this)[tag.Name] = tag;
                return tag;
            } else {
                return null;
            }
        }

        public NBTag Append( string name, byte value ) {
            return Append( new NBTag( NBTType.Byte, name, value, this ) );
        }
        public NBTag Append( string name, short value ) {
            return Append( new NBTag( NBTType.Short, name, value, this ) );
        }
        public NBTag Append( string name, int value ) {
            return Append( new NBTag( NBTType.Int, name, value, this ) );
        }
        public NBTag Append( string name, long value ) {
            return Append( new NBTag( NBTType.Long, name, value, this ) );
        }
        public NBTag Append( string name, float value ) {
            return Append( new NBTag( NBTType.Float, name, value, this ) );
        }
        public NBTag Append( string name, double value ) {
            return Append( new NBTag( NBTType.Double, name, value, this ) );
        }
        public NBTag Append( string name, byte[] value ) {
            return Append( new NBTag( NBTType.Bytes, name, value, this ) );
        }
        public NBTag Append( string name, string value ) {
            return Append( new NBTag( NBTType.String, name, value, this ) );
        }
        public NBTag Append( string name, params NBTag[] tags ) {
            NBTCompound compound = new NBTCompound();
            compound.Name = name;
            foreach( NBTag tag in tags ) {
                compound.Tags.Add( tag.Name, tag );
            }
            return Append( compound );
        }
        public NBTag Append( params NBTag[] tags ) {
            foreach( NBTag tag in tags ) {
                Append( tag );
            }
            return this;
        }
        #endregion

        #region Child Tag Manipulation
        public bool Contains( string name ) {
            if( this is NBTCompound ) {
                return ((NBTCompound)this).Tags.ContainsKey( name );
            } else {
                return false;
            }
        }
        public NBTag Remove( string name ) {
            if( this is NBTCompound ) {
                NBTag tag = ((NBTCompound)this)[name];
                ((NBTCompound)this).Tags.Remove( name );
                return tag;
            } else {
                throw new NotSupportedException( "Can only Remove() from compound tags." );
            }
        }
        public NBTag Remove() {
            if( this.Parent != null && this.Parent is NBTCompound ) {
                ((NBTCompound)this.Parent).Remove( this.Name );
                return this;
            } else {
                throw new NotSupportedException( "Cannot Remove() - no parent tag." );
            }
        }
        #endregion

        #region Loading

        public static NBTCompound ReadFile( string fileName ) {
            using( FileStream fs = File.OpenRead( fileName ) ) {
                using( GZipStream gs = new GZipStream( fs, CompressionMode.Decompress ) ) {
                    return ReadStream( gs );
                }
            }
        }

        public static NBTCompound ReadStream( Stream stream ) {
            BinaryReader reader = new BinaryReader( stream );
            return (NBTCompound)ReadTag( reader, (NBTType)reader.ReadByte(), null, null );
        }

        public static NBTag ReadTag( BinaryReader reader, NBTType type, string name, NBTag _parent ) {
            if( name == null && type != NBTType.End ) {
                name = ReadString( reader );
            }
            switch( type ) {
                case NBTType.End:
                    return new NBTag( NBTType.End, _parent );

                case NBTType.Byte:
                    return new NBTag( NBTType.Byte, name, reader.ReadByte(), _parent );

                case NBTType.Short:
                    return new NBTag( NBTType.Short, name, IPAddress.NetworkToHostOrder( reader.ReadInt16() ), _parent );

                case NBTType.Int:
                    return new NBTag( NBTType.Int, name, IPAddress.NetworkToHostOrder( reader.ReadInt32() ), _parent );

                case NBTType.Long:
                    return new NBTag( NBTType.Long, name, IPAddress.NetworkToHostOrder( reader.ReadInt64() ), _parent );

                case NBTType.Float:
                    return new NBTag( NBTType.Float, name, reader.ReadSingle(), _parent );

                case NBTType.Double:
                    return new NBTag( NBTType.Double, name, reader.ReadDouble(), _parent );

                case NBTType.Bytes:
                    int bytesLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    return new NBTag( NBTType.Bytes, name, reader.ReadBytes( bytesLength ), _parent );

                case NBTType.String:
                    return new NBTag( NBTType.String, name, (object)ReadString( reader ), _parent );


                case NBTType.List:
                    NBTList list = new NBTList();
                    list.Type = NBTType.List;
                    list.Name = name;
                    list.Parent = _parent;
                    list.ListType = (NBTType)reader.ReadByte();
                    int listLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    list.Tags = new NBTag[listLength];
                    for( int i = 0; i < listLength; i++ ) {
                        list.Tags[i] = ReadTag( reader, list.ListType, "", list );
                    }
                    return list;

                case NBTType.Compound:
                    NBTag childTag;
                    NBTCompound compound = new NBTCompound();
                    compound.Type = NBTType.Compound;
                    compound.Name = name;
                    compound.Parent = _parent;
                    while( true ) {
                        childTag = ReadTag( reader, (NBTType)reader.ReadByte(), null, compound );
                        if( childTag.Type == NBTType.End ) break;
                        if( childTag.Name == null )
                            continue;
                        if( compound.Tags.ContainsKey( childTag.Name ) ) {
                            throw new IOException( "NBT parsing error: null names and duplicate names are not allowed within a compound tags." );
                        } else {
                            compound.Tags.Add( childTag.Name, childTag );
                        }
                    }
                    return compound;

                default:
                    throw new IOException( "NBT parsing error: unknown tag type." );
            }
        }

        public static string ReadString( BinaryReader reader ) {
            short stringLength = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
            return Encoding.UTF8.GetString( reader.ReadBytes( stringLength ) );
        }

        public string GetFullName() {
            string fullName = ToString();
            NBTag tag = this;
            while( tag.Parent != null ) {
                tag = tag.Parent;
                fullName = tag.ToString() + "." + fullName;
            }
            return fullName;
        }

        public string GetIndentedName() {
            string fullName = ToString();
            NBTag tag = this;
            while( tag.Parent != null ) {
                tag = tag.Parent;
                fullName = "    " + fullName;
            }
            return fullName;
        }

        public override string ToString() {
            return Type.ToString() + " " + Name;
        }

        public string ToString( bool recursive ) {
            string output = GetIndentedName() + Environment.NewLine;
            foreach( NBTag tag in this ) {
                output += tag.ToString( recursive );
            }
            return output;
        }
        #endregion

        #region Saving
        public void WriteTag( string fileName ) {
            using( FileStream fs = File.OpenWrite( fileName ) ) {
                using( GZipStream gs = new GZipStream( fs, CompressionMode.Compress ) ) {
                    WriteTag( gs );
                }
            }
        }
        public void WriteTag( Stream stream ) {
            using( BinaryWriter writer = new BinaryWriter( stream ) ) {
                WriteTag( writer, true );
            }
        }
        public void WriteTag( BinaryWriter writer ) {
            WriteTag( writer, true );
        }
        public void WriteTag( BinaryWriter writer, bool writeType ) {
            if( writeType ) writer.Write( (byte)Type );
            if( Type == NBTType.End ) return;
            if( writeType ) WriteString( Name, writer );
            switch( Type ) {
                case NBTType.Byte:
                    writer.Write( (byte)Payload );
                    return;

                case NBTType.Short:
                    writer.Write( IPAddress.HostToNetworkOrder( (short)Payload ) );
                    return;

                case NBTType.Int:
                    writer.Write( IPAddress.HostToNetworkOrder( (int)Payload ) );
                    return;

                case NBTType.Long:
                    writer.Write( IPAddress.HostToNetworkOrder( (long)Payload ) );
                    return;

                case NBTType.Float:
                    writer.Write( (float)Payload );
                    return;

                case NBTType.Double:
                    writer.Write( (double)Payload );
                    return;

                case NBTType.Bytes:
                    writer.Write( IPAddress.HostToNetworkOrder( ((byte[])Payload).Length ) );
                    writer.Write( (byte[])Payload );
                    return;

                case NBTType.String:
                    WriteString( (string)Payload, writer );
                    return;


                case NBTType.List:
                    NBTList list = (NBTList)this;
                    writer.Write( (byte)list.ListType );
                    writer.Write( IPAddress.HostToNetworkOrder( list.Tags.Length ) );

                    for( int i = 0; i < list.Tags.Length; i++ ) {
                        list.Tags[i].WriteTag( writer, false );
                    }
                    return;

                case NBTType.Compound:
                    foreach( NBTag tag in this ) {
                        tag.WriteTag( writer );
                    }
                    writer.Write( (byte)NBTType.End );
                    return;

                default:
                    return;
            }
        }

        public static void WriteString( string str, BinaryWriter writer ) {
            byte[] stringBytes = Encoding.UTF8.GetBytes( str );
            writer.Write( IPAddress.NetworkToHostOrder( (short)stringBytes.Length ) );
            writer.Write( stringBytes );
        }
        #endregion

        #region Accessors
        public byte GetByte() { return (byte)Payload; }
        public short GetShort() { return (short)Payload; }
        public int GetInt() { return (int)Payload; }
        public long GetLong() { return (long)Payload; }
        public float GetFloat() { return (float)Payload; }
        public double GetDouble() { return (double)Payload; }
        public byte[] GetBytes() { return (byte[])Payload; }
        public string GetString() { return (string)Payload; }

        public void Set( object _payload ) { Payload = _payload; }

        object GetChild( string name, object defaultValue ) {
            if( Contains( name ) ) {
                return this[name].Payload;
            } else {
                return defaultValue;
            }
        }

        public byte Get( string name, byte defaultValue ) { return (byte)GetChild( name, defaultValue ); }
        public short Get( string name, short defaultValue ) { return (short)GetChild( name, defaultValue ); }
        public int Get( string name, int defaultValue ) { return (int)GetChild( name, defaultValue ); }
        public long Get( string name, long defaultValue ) { return (long)GetChild( name, defaultValue ); }
        public float Get( string name, float defaultValue ) { return (float)GetChild( name, defaultValue ); }
        public double Get( string name, double defaultValue ) { return (double)GetChild( name, defaultValue ); }
        public byte[] Get( string name, byte[] defaultValue ) { return (byte[])GetChild( name, defaultValue ); }
        public string Get( string name, string defaultValue ) { return (string)GetChild( name, defaultValue ); }

        #endregion

        #region Indexers
        public NBTag this[int Index] {
            get {
                if( this is NBTList ) {
                    return ((NBTList)this).Tags[Index];
                } else {
                    throw new NotSupportedException();
                }
            }
            set {
                if( this is NBTList ) {
                    ((NBTList)this).Tags[Index] = value;
                } else {
                    throw new NotSupportedException();
                }
            }
        }

        public NBTag this[string Key] {
            get {
                if( this is NBTCompound ) {
                    return ((NBTCompound)this).Tags[Key];
                } else {
                    throw new NotSupportedException();
                }
            }
            set {
                if( this is NBTCompound ) {
                    ((NBTCompound)this).Tags[Key] = value;
                } else {
                    throw new NotSupportedException();
                }
            }
        }
        #endregion

        #region Enumerators
        public IEnumerator<NBTag> GetEnumerator() {
            return new NBTEnumerator( this );
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new NBTEnumerator( this );
        }

        public class NBTEnumerator : IEnumerator<NBTag> {
            NBTag[] tags;
            int index = -1;

            public NBTEnumerator( NBTag tag ) {
                if( tag is NBTCompound ) {
                    tags = new NBTag[((NBTCompound)tag).Tags.Count];
                    ((NBTCompound)tag).Tags.Values.CopyTo( tags, 0 );
                } else if( tag is NBTList ) {
                    tags = ((NBTList)tag).Tags;
                } else {
                    tags = new NBTag[0];
                }
            }

            public NBTag Current {
                get {
                    return tags[index];
                }
            }

            object IEnumerator.Current {
                get {
                    return tags[index];
                }
            }

            public bool MoveNext() {
                if( index < tags.Length ) index++;
                return index < tags.Length;
            }

            public void Reset() {
                index = -1;
            }

            public void Dispose() { }
        }
        #endregion
    }


    public class NBTList : NBTag {
        public NBTList() {
            Type = NBTType.List;
        }
        public NBTList( string _name, NBTType _type, int count ) {
            Name = _name;
            Type = NBTType.List;
            ListType = _type;
            Tags = new NBTag[count];
        }
        public NBTList( string _name, NBTType _type, List<object> _payloads ) {
            Name = _name;
            Type = NBTType.List;
            ListType = _type;
            Tags = new NBTag[_payloads.Count];
            int i = 0;
            foreach( object payload in _payloads ) {
                Tags[i++] = new NBTag( ListType, null, payload, this );
            }
        }
        public NBTag[] Tags;
        public NBTType ListType;
    }


    public class NBTCompound : NBTag {
        public NBTCompound() {
            Type = NBTType.Compound;
        }
        public NBTCompound( string _name ) {
            Name = _name;
            Type = NBTType.Compound;
        }
        public Dictionary<string, NBTag> Tags = new Dictionary<string, NBTag>();
    }
}