// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;
// ReSharper disable CodeAnnotationAnalyzer

namespace fCraft.MapConversion {
    /// <summary> Standard NBT data types. </summary>
    enum NbtType : byte { // TODO: replace with fNBT
        /// <summary> End of tag </summary>
        End,
        /// <summary> 8 bit integer </summary>
        Byte,
        /// <summary> 16 bit integer </summary>
        Short,
        /// <summary> 32 bit integer </summary>
        Int,
        /// <summary> 64 bit integer </summary>
        Long,
        /// <summary> 32 bit floating point number (IEEE 754) </summary>
        Float,
        /// <summary> 64 bit floating point number (IEEE 754) </summary>
        Double,
        Bytes,
        String,
        List,
        Compound
    }


    class NBTag : IEnumerable<NBTag> {
        public NbtType Type { get; protected set; }
        public string Name { get; set; }
        public object Payload { get; set; }
        [CanBeNull]
        public NBTag Parent { get; set; }


        #region Constructors

        protected NBTag() { }

        NBTag( NbtType type, NBTag parent ) {
            Type = type;
            Parent = parent;
        }

        public NBTag( NbtType type, string name, object payload, NBTag parent ) {
            Type = type;
            Name = name;
            Payload = payload;
            Parent = parent;
        }

        #endregion


        #region Loading

        public static NBTCompound ReadStream( [NotNull] Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            BinaryReader reader = new BinaryReader( stream );
            return (NBTCompound)ReadTag( reader, (NbtType)reader.ReadByte(), null, null );
        }

        public static NBTag ReadTag( [NotNull] BinaryReader reader, NbtType type, string name, NBTag parent ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            if( name == null && type != NbtType.End ) {
                name = ReadString( reader );
            }
            switch( type ) {
                case NbtType.End:
                    return new NBTag( NbtType.End, parent );

                case NbtType.Byte:
                    return new NBTag( NbtType.Byte, name, reader.ReadByte(), parent );

                case NbtType.Short:
                    return new NBTag( NbtType.Short, name, IPAddress.NetworkToHostOrder( reader.ReadInt16() ), parent );

                case NbtType.Int:
                    return new NBTag( NbtType.Int, name, IPAddress.NetworkToHostOrder( reader.ReadInt32() ), parent );

                case NbtType.Long:
                    return new NBTag( NbtType.Long, name, IPAddress.NetworkToHostOrder( reader.ReadInt64() ), parent );

                case NbtType.Float:
                    return new NBTag( NbtType.Float, name, reader.ReadSingle(), parent );

                case NbtType.Double:
                    return new NBTag( NbtType.Double, name, reader.ReadDouble(), parent );

                case NbtType.Bytes:
                    int bytesLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    return new NBTag( NbtType.Bytes, name, reader.ReadBytes( bytesLength ), parent );

                case NbtType.String:
                    return new NBTag( NbtType.String, name, ReadString( reader ), parent );


                case NbtType.List:
                    NBTList list = new NBTList {
                        Type = NbtType.List,
                        Name = name,
                        Parent = parent,
                        ListType = (NbtType)reader.ReadByte()
                    };
                    int listLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    list.Tags = new NBTag[listLength];
                    for( int i = 0; i < listLength; i++ ) {
                        list.Tags[i] = ReadTag( reader, list.ListType, "", list );
                    }
                    return list;

                case NbtType.Compound:
                    NBTCompound compound = new NBTCompound {
                        Type = NbtType.Compound,
                        Name = name,
                        Parent = parent
                    };
                    while( true ) {
                        NBTag childTag = ReadTag( reader, (NbtType)reader.ReadByte(), null, compound );
                        if( childTag.Type == NbtType.End ) break;
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

        public static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            short stringLength = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
            return Encoding.UTF8.GetString( reader.ReadBytes( stringLength ) );
        }

        public string FullName {
            get {
                string fullName = ToString();
                NBTag tag = this;
                while( tag.Parent != null ) {
                    tag = tag.Parent;
                    fullName = tag + "." + fullName;
                }
                return fullName;
            }
        }

        public string IndentedName {
            get {
                string fullName = ToString();
                NBTag tag = this;
                while( tag.Parent != null ) {
                    tag = tag.Parent;
                    fullName = "    " + fullName;
                }
                return fullName;
            }
        }

        public override string ToString() {
            return Type + " " + Name;
        }

        public string ToString( bool recursive ) {
            if( !recursive ) return ToString();
            StringBuilder sb = new StringBuilder( IndentedName );
            sb.AppendLine();
            foreach( NBTag tag in this ) {
                sb.Append( tag.ToString( true ) );
            }
            return sb.ToString();
        }

        #endregion


        #region Accessors

        public short GetShort() { return (short)Payload; }
        public byte[] GetBytes() { return (byte[])Payload; }

        #endregion


        #region Indexers

        public NBTag this[int index] {
            get {
                if( this is NBTList ) {
                    return ( (NBTList)this ).Tags[index];
                } else {
                    throw new NotSupportedException();
                }
            }
        }

        public NBTag this[string key] {
            get {
                if( this is NBTCompound ) {
                    return ( (NBTCompound)this ).Tags[key];
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

        public sealed class NBTEnumerator : IEnumerator<NBTag> {
            readonly NBTag[] tags;
            int index = -1;

            public NBTEnumerator( NBTag tag ) {
                if( tag is NBTCompound ) {
                    tags = new NBTag[( (NBTCompound)tag ).Tags.Count];
                    ( (NBTCompound)tag ).Tags.Values.CopyTo( tags, 0 );
                } else if( tag is NBTList ) {
                    tags = ( (NBTList)tag ).Tags;
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


    sealed class NBTList : NBTag {
        public NBTList() {
            Type = NbtType.List;
        }
        public NBTag[] Tags;
        public NbtType ListType;
    }


    sealed class NBTCompound : NBTag {
        public NBTCompound() {
            Type = NbtType.Compound;
        }
        public readonly Dictionary<string, NBTag> Tags = new Dictionary<string, NBTag>();
    }
}