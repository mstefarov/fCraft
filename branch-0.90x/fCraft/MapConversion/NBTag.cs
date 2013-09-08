// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Standard NBT data types. </summary>
    public enum NBTType : byte {
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


    public class NBTag : IEnumerable<NBTag> {
        public NBTType Type { get; protected set; }
        public string Name { get; set; }
        public object Payload { get; set; }
        [CanBeNull]
        public NBTag Parent { get; set; }


        #region Constructors

        protected NBTag() { }

        NBTag( NBTType type, NBTag parent ) {
            Type = type;
            Parent = parent;
        }

        public NBTag( NBTType type, string name, object payload, NBTag parent ) {
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
            return (NBTCompound)ReadTag( reader, (NBTType)reader.ReadByte(), null, null );
        }

        public static NBTag ReadTag( [NotNull] BinaryReader reader, NBTType type, string name, NBTag parent ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            if( name == null && type != NBTType.End ) {
                name = ReadString( reader );
            }
            switch( type ) {
                case NBTType.End:
                    return new NBTag( NBTType.End, parent );

                case NBTType.Byte:
                    return new NBTag( NBTType.Byte, name, reader.ReadByte(), parent );

                case NBTType.Short:
                    return new NBTag( NBTType.Short, name, IPAddress.NetworkToHostOrder( reader.ReadInt16() ), parent );

                case NBTType.Int:
                    return new NBTag( NBTType.Int, name, IPAddress.NetworkToHostOrder( reader.ReadInt32() ), parent );

                case NBTType.Long:
                    return new NBTag( NBTType.Long, name, IPAddress.NetworkToHostOrder( reader.ReadInt64() ), parent );

                case NBTType.Float:
                    return new NBTag( NBTType.Float, name, reader.ReadSingle(), parent );

                case NBTType.Double:
                    return new NBTag( NBTType.Double, name, reader.ReadDouble(), parent );

                case NBTType.Bytes:
                    int bytesLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    return new NBTag( NBTType.Bytes, name, reader.ReadBytes( bytesLength ), parent );

                case NBTType.String:
                    return new NBTag( NBTType.String, name, ReadString( reader ), parent );


                case NBTType.List:
                    NBTList list = new NBTList {
                        Type = NBTType.List,
                        Name = name,
                        Parent = parent,
                        ListType = (NBTType)reader.ReadByte()
                    };
                    int listLength = IPAddress.NetworkToHostOrder( reader.ReadInt32() );
                    list.Tags = new NBTag[listLength];
                    for( int i = 0; i < listLength; i++ ) {
                        list.Tags[i] = ReadTag( reader, list.ListType, "", list );
                    }
                    return list;

                case NBTType.Compound:
                    NBTCompound compound = new NBTCompound {
                        Type = NBTType.Compound,
                        Name = name,
                        Parent = parent
                    };
                    while( true ) {
                        NBTag childTag = ReadTag( reader, (NBTType)reader.ReadByte(), null, compound );
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

        public void Set( object payload ) { Payload = payload; }

        public byte GetByte() { return (byte)Payload; }
        public short GetShort() { return (short)Payload; }
        public int GetInt() { return (int)Payload; }
        public long GetLong() { return (long)Payload; }
        public float GetFloat() { return (float)Payload; }
        public double GetDouble() { return (double)Payload; }
        public byte[] GetBytes() { return (byte[])Payload; }
        public string GetString() { return (string)Payload; }

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
            set {
                if( this is NBTList ) {
                    ( (NBTList)this ).Tags[index] = value;
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
            set {
                if( this is NBTCompound ) {
                    ( (NBTCompound)this ).Tags[key] = value;
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


    public sealed class NBTList : NBTag {
        public NBTList() {
            Type = NBTType.List;
        }
        public NBTag[] Tags;
        public NBTType ListType;
    }


    public sealed class NBTCompound : NBTag {
        public NBTCompound() {
            Type = NBTType.Compound;
        }
        public readonly Dictionary<string, NBTag> Tags = new Dictionary<string, NBTag>();
    }
}