using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using JetBrains.Annotations;

namespace fNbt {
    /// <summary> A tag containing a single byte. </summary>
    public sealed class NbtByte : NbtTag {
        /// <summary> Type of this tag (Byte). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Byte;
            }
        }

        /// <summary> Value/payload of this tag (a single byte). </summary>
        public byte Value { get; set; }


        /// <summary> Creates an unnamed NbtByte tag with the default value of 0. </summary>
        public NbtByte() { }


        /// <summary> Creates an unnamed NbtByte tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtByte( byte value )
            : this( null, value ) { }


        /// <summary> Creates an NbtByte tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtByte( [CanBeNull] string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtByte tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtByte( [CanBeNull] string tagName, byte value ) {
            Name = tagName;
            Value = value;
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadByte();
                return false;
            }
            Value = readStream.ReadByte();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadByte();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Byte );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }


        /// <summary> Returns a String that represents the current NbtByte object.
        /// Format: TAG_Byte("Name"): Value </summary>
        /// <returns> A String that represents the current NbtByte object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Byte" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing an array of bytes. </summary>
    public sealed class NbtByteArray : NbtTag {
        /// <summary> Type of this tag (ByteArray). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.ByteArray;
            }
        }


        /// <summary> Value/payload of this tag (an array of bytes). May not be <c>null</c>. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        [NotNull]
        public byte[] Value {
            get {
                return bytes;
            }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                bytes = value;
            }
        }

        [NotNull]
        byte[] bytes;


        /// <summary> Creates an unnamed NbtByte tag, containing an empty array of bytes. </summary>
        public NbtByteArray()
            : this( null, new byte[0] ) { }


        /// <summary> Creates an unnamed NbtByte tag, containing the given array of bytes. </summary>
        /// <param name="value"> Byte array to assign to this tag's Value. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtByteArray( [NotNull] byte[] value )
            : this( null, value ) { }


        /// <summary> Creates an NbtByte tag with the given name, containing an empty array of bytes. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtByteArray( [CanBeNull] string tagName )
            : this( tagName, new byte[0] ) { }


        /// <summary> Creates an NbtByte tag with the given name, containing the given array of bytes. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Byte array to assign to this tag's Value. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtByteArray( [CanBeNull] string tagName, [NotNull] byte[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = (byte[])value.Clone();
        }


        /// <summary> Gets or sets a byte at the given index. </summary>
        /// <param name="tagIndex"> The zero-based index of the element to get or set. </param>
        /// <returns> The byte at the specified index. </returns>
        /// <exception cref="IndexOutOfRangeException"> <paramref name="tagIndex"/> is outside the array bounds. </exception>
        public new byte this[int tagIndex] {
            get {
                return Value[tagIndex];
            }
            set {
                Value[tagIndex] = value;
            }
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative length given in TAG_Byte_Array" );
            }

            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.Skip( length );
                return false;
            }
            Value = readStream.ReadBytes( length );
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative length given in TAG_Byte_Array" );
            }
            readStream.Skip( length );
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.ByteArray );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value.Length );
            writeStream.Write( Value, 0, Value.Length );
        }


        /// <summary> Returns a String that represents the current NbtByteArray object.
        /// Format: TAG_Byte_Array("Name"): [N bytes] </summary>
        /// <returns> A String that represents the current NbtByteArray object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Byte_Array" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} bytes]", bytes.Length );
        }
    }


    /// <summary> A tag containing a set of other named tags. Order is not guaranteed. </summary>
    public sealed class NbtCompound : NbtTag, ICollection<NbtTag>, ICollection {
        /// <summary> Type of this tag (Compound). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Compound;
            }
        }

        readonly Dictionary<string, NbtTag> tags = new Dictionary<string, NbtTag>();


        /// <summary> Creates an empty unnamed NbtByte tag. </summary>
        public NbtCompound() { }


        /// <summary> Creates an empty NbtByte tag with the given name. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtCompound( [CanBeNull] string tagName ) {
            Name = tagName;
        }


        /// <summary> Creates an unnamed NbtByte tag, containing the given tags. </summary>
        /// <param name="tags"> Collection of tags to assign to this tag's Value. May not be null </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>, or one of the tags is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If some of the given tags were not named, or two tags with the same name were given. </exception>
        public NbtCompound( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags ) { }


        /// <summary> Creates an NbtByte tag with the given name, containing the given tags. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="tags"> Collection of tags to assign to this tag's Value. May not be null </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>, or one of the tags is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If some of the given tags were not named, or two tags with the same name were given. </exception>
        public NbtCompound( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags ) {
            if( tags == null )
                throw new ArgumentNullException( "tags" );
            Name = tagName;
            foreach( NbtTag tag in tags ) {
                Add( tag );
            }
        }


        /// <summary> Gets or sets the tag with the specified name. May return <c>null</c>. </summary>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <param name="tagName"> The name of the tag to get or set. Must match tag's actual name. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>; or if trying to assign null value. </exception>
        /// <exception cref="ArgumentException"> <paramref name="tagName"/> does not match the given tag's actual name;
        /// or given tag already has a Parent. </exception>
        public override NbtTag this[[NotNull] string tagName] {
            [CanBeNull]
            get {
                return Get<NbtTag>( tagName );
            }
            set {
                if( tagName == null ) {
                    throw new ArgumentNullException( "tagName" );
                } else if( value == null ) {
                    throw new ArgumentNullException( "value" );
                } else if( value.Name != tagName ) {
                    throw new ArgumentException( "Given tag name must match tag's actual name." );
                } else if( value.Parent != null ) {
                    throw new ArgumentException( "A tag may only be added to one compound/list at a time." );
                }
                tags[tagName] = value;
                value.Parent = this;
            }
        }


        /// <summary> Gets the tag with the specified name. May return <c>null</c>. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        [CanBeNull]
        public T Get<T>( [NotNull] string tagName ) where T : NbtTag {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( tags.TryGetValue( tagName, out result ) ) {
                return (T)result;
            }
            return null;
        }


        /// <summary> Gets the tag with the specified name. May return <c>null</c>. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        [CanBeNull]
        public NbtTag Get( [NotNull] string tagName ) {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            NbtTag result;
            if( tags.TryGetValue( tagName, out result ) ) {
                return result;
            }
            return null;
        }


        /// <summary> Gets the tag with the specified name. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <param name="result"> When this method returns, contains the tag associated with the specified name, if the tag is found;
        /// otherwise, null. This parameter is passed uninitialized. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> true if the NbtCompound contains a tag with the specified name; otherwise, false. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        public bool TryGet<T>( [NotNull] string tagName, out T result ) where T : NbtTag {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            NbtTag tempResult;
            if( tags.TryGetValue( tagName, out tempResult ) ) {
                result = (T)tempResult;
                return true;
            } else {
                result = null;
                return false;
            }
        }


        /// <summary> Gets the tag with the specified name. </summary>
        /// <param name="tagName"> The name of the tag to get. </param>
        /// <param name="result"> When this method returns, contains the tag associated with the specified name, if the tag is found;
        /// otherwise, null. This parameter is passed uninitialized. </param>
        /// <returns> true if the NbtCompound contains a tag with the specified name; otherwise, false. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        public bool TryGet( [NotNull] string tagName, out NbtTag result ) {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            NbtTag tempResult;
            if( tags.TryGetValue( tagName, out tempResult ) ) {
                result = tempResult;
                return true;
            } else {
                result = null;
                return false;
            }
        }


        /// <summary> Copies all tags in this NbtCompound to an array. </summary>
        /// <returns> Array of NbtTags. </returns>
        [NotNull, Pure]
        public NbtTag[] ToArray() {
            NbtTag[] array = new NbtTag[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag;
            }
            return array;
        }


        /// <summary> Copies names of all tags in this NbtCompound to an array. </summary>
        /// <returns> Array of strings (tag names). </returns>
        [NotNull, Pure]
        public string[] ToNameArray() {
            string[] array = new string[tags.Count];
            int i = 0;
            foreach( NbtTag tag in tags.Values ) {
                array[i++] = tag.Name;
            }
            return array;
        }


        /// <summary> Adds all tags from the specified collection to this NbtCompound. </summary>
        /// <param name="newTags"> The collection whose elements should be added to this NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="newTags"/> is <c>null</c>, or one of the tags in newTags is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If one of the given tags was unnamed,
        /// or if a tag with the given name already exists in this NbtCompound. </exception>
        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null )
                throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


        /// <summary> Determines whether this NbtCompound contains a tag with a specific name. </summary>
        /// <param name="tagName"> Tag name to search for. May not be <c>null</c>. </param>
        /// <returns> true if a tag with given name was found; otherwise, false. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        [Pure]
        public bool Contains( [NotNull] string tagName ) {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            return tags.ContainsKey( tagName );
        }


        /// <summary> Removes the tag with the specified name from this NbtCompound. </summary>
        /// <param name="tagName"> The name of the tag to remove. </param>
        /// <returns> true if the tag is successfully found and removed; otherwise, false.
        /// This method returns false if name is not found in the NbtCompound. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tagName"/> is <c>null</c>. </exception>
        public bool Remove( [NotNull] string tagName ) {
            if( tagName == null )
                throw new ArgumentNullException( "tagName" );
            NbtTag tag;
            if( !tags.TryGetValue( tagName, out tag ) ) {
                return false;
            }
            if( !tags.Remove( tagName ) ) {
                return false;
            }
            tag.Parent = null;
            return true;
        }


        internal void RenameTag( [NotNull] string oldName, [NotNull] string newName ) {
            if( oldName == null )
                throw new ArgumentNullException( "oldName" );
            if( newName == null )
                throw new ArgumentNullException( "newName" );
            if( oldName == newName )
                return;
            NbtTag tag;
            if( tags.TryGetValue( newName, out tag ) ) {
                throw new ArgumentException( "Cannot rename: a tag with the name already exists in this compound." );
            }
            if( !tags.TryGetValue( oldName, out tag ) ) {
                throw new ArgumentException( "Cannot rename: no tag found to rename." );
            }
            tags.Remove( oldName );
            tags.Add( newName, tag );
        }


        /// <summary> Gets a collection containing all tag names in this NbtCompound. </summary>
        public IEnumerable<string> Names {
            get {
                return tags.Keys;
            }
        }


        /// <summary> Gets a collection containing all tags in this NbtCompound. </summary>
        public IEnumerable<NbtTag> Tags {
            get {
                return tags.Values;
            }
        }


        #region Reading / Writing

        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( Parent != null && readStream.Selector != null && !readStream.Selector( this ) ) {
                SkipTag( readStream );
                return false;
            }

            while( true ) {
                NbtTagType nextTag = readStream.ReadTagType();
                NbtTag newTag;
                switch( nextTag ) {
                    case NbtTagType.End:
                        return true;

                    case NbtTagType.Byte:
                        newTag = new NbtByte();
                        break;

                    case NbtTagType.Short:
                        newTag = new NbtShort();
                        break;

                    case NbtTagType.Int:
                        newTag = new NbtInt();
                        break;

                    case NbtTagType.Long:
                        newTag = new NbtLong();
                        break;

                    case NbtTagType.Float:
                        newTag = new NbtFloat();
                        break;

                    case NbtTagType.Double:
                        newTag = new NbtDouble();
                        break;

                    case NbtTagType.ByteArray:
                        newTag = new NbtByteArray();
                        break;

                    case NbtTagType.String:
                        newTag = new NbtString();
                        break;

                    case NbtTagType.List:
                        newTag = new NbtList();
                        break;

                    case NbtTagType.Compound:
                        newTag = new NbtCompound();
                        break;

                    case NbtTagType.IntArray:
                        newTag = new NbtIntArray();
                        break;

                    default:
                        throw new NbtFormatException( "Unsupported tag type found in NBT_Compound: " + nextTag );
                }
                newTag.Parent = this;
                newTag.Name = readStream.ReadString();
                if( newTag.ReadTag( readStream ) ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    // newTag.Name is never null
                    tags.Add( newTag.Name, newTag );
                    // ReSharper restore AssignNullToNotNullAttribute
                }
            }
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            while( true ) {
                NbtTagType nextTag = readStream.ReadTagType();
                NbtTag newTag;
                switch( nextTag ) {
                    case NbtTagType.End:
                        return;

                    case NbtTagType.Byte:
                        newTag = new NbtByte();
                        break;

                    case NbtTagType.Short:
                        newTag = new NbtShort();
                        break;

                    case NbtTagType.Int:
                        newTag = new NbtInt();
                        break;

                    case NbtTagType.Long:
                        newTag = new NbtLong();
                        break;

                    case NbtTagType.Float:
                        newTag = new NbtFloat();
                        break;

                    case NbtTagType.Double:
                        newTag = new NbtDouble();
                        break;

                    case NbtTagType.ByteArray:
                        newTag = new NbtByteArray();
                        break;

                    case NbtTagType.String:
                        newTag = new NbtString();
                        break;

                    case NbtTagType.List:
                        newTag = new NbtList();
                        break;

                    case NbtTagType.Compound:
                        newTag = new NbtCompound();
                        break;

                    case NbtTagType.IntArray:
                        newTag = new NbtIntArray();
                        break;

                    default:
                        throw new NbtFormatException( "Unsupported tag type found in NBT_Compound: " + nextTag );
                }
                readStream.SkipString();
                newTag.SkipTag( readStream );
            }
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Compound );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }

            WriteData( writeStream );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            foreach( NbtTag tag in tags.Values ) {
                tag.WriteTag( writeStream, true );
            }
            writeStream.Write( NbtTagType.End );
        }

        #endregion


        #region Implementation of IEnumerable<NbtTag>

        /// <summary> Returns an enumerator that iterates through all tags in this NbtCompound. </summary>
        /// <returns> An IEnumerator&gt;NbtTag&lt; that can be used to iterate through the collection. </returns>
        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.Values.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.Values.GetEnumerator();
        }

        #endregion


        #region Implementation of ICollection<NbtTag>

        /// <summary> Adds a tag to this NbtCompound. </summary>
        /// <param name="newTag"> The object to add to this NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="newTag"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If the given tag is unnamed;
        /// or if a tag with the given name already exists in this NbtCompound. </exception>
        public void Add( [NotNull] NbtTag newTag ) {
            if( newTag == null ) {
                throw new ArgumentNullException( "newTag" );
            } else if( newTag == this ) {
                throw new ArgumentException( "Cannot add tag to self" );
            } else if( newTag.Name == null ) {
                throw new ArgumentException( "Only named tags are allowed in compound tags." );
            } else if( newTag.Parent != null ) {
                throw new ArgumentException( "A tag may only be added to one compound/list at a time." );
            }
            tags.Add( newTag.Name, newTag );
            newTag.Parent = this;
        }


        /// <summary> Removes all tags from this NbtCompound. </summary>
        public void Clear() {
            foreach( NbtTag tag in tags.Values ) {
                tag.Parent = null;
            }
            tags.Clear();
        }


        /// <summary> Determines whether this NbtCompound contains a specific NbtTag.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag is found; otherwise, false. </returns>
        /// <param name="tag"> The object to locate in this NbtCompound. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tag"/> is <c>null</c>. </exception>
        [Pure]
        public bool Contains( [NotNull] NbtTag tag ) {
            if( tag == null )
                throw new ArgumentNullException( "tag" );
            return tags.ContainsValue( tag );
        }


        /// <summary> Copies the tags of the NbtCompound to an array, starting at a particular array index. </summary>
        /// <param name="array"> The one-dimensional array that is the destination of the tag copied from NbtCompound.
        /// The array must have zero-based indexing. </param>
        /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="array"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> arrayIndex is less than 0. </exception>
        /// <exception cref="ArgumentException"> Given array is multidimensional; arrayIndex is equal to or greater than the length of array;
        /// the number of tags in this NbtCompound is greater than the available space from arrayIndex to the end of the destination array;
        /// or type NbtTag cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.Values.CopyTo( array, arrayIndex );
        }


        /// <summary> Removes the first occurrence of a specific NbtTag from the NbtCompound.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag was successfully removed from the NbtCompound; otherwise, false.
        /// This method also returns false if tag is not found. </returns>
        /// <param name="tag"> The tag to remove from the NbtCompound. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tag"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If the given tag is unnamed </exception>
        public bool Remove( [NotNull] NbtTag tag ) {
            if( tag == null )
                throw new ArgumentNullException( "tag" );
            if( tag.Name == null )
                throw new ArgumentException( "Trying to remove an unnamed tag." );
            NbtTag maybeItem;
            if( tags.TryGetValue( tag.Name, out maybeItem ) ) {
                if( maybeItem == tag && tags.Remove( tag.Name ) ) {
                    tag.Parent = null;
                    return true;
                }
            }
            return false;
        }


        /// <summary> Gets the number of tags contained in the NbtCompound. </summary>
        /// <returns> The number of tags contained in the NbtCompound. </returns>
        public int Count {
            get {
                return tags.Count;
            }
        }


        bool ICollection<NbtTag>.IsReadOnly {
            get {
                return false;
            }
        }

        #endregion


        #region Implementation of ICollection

        void ICollection.CopyTo( Array array, int index ) {
            CopyTo( (NbtTag[])array, index );
        }


        object ICollection.SyncRoot {
            get {
                return (tags as ICollection).SyncRoot;
            }
        }


        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }

        #endregion


        // Hiding NbtTag[int]:NbtTag
        new NbtTag this[int tagIndex] {
            get {
                throw new InvalidOperationException( "Integer indexers only work on NbtList tags." );
            }
            set {
                throw new InvalidOperationException( "Integer indexers only work on NbtList tags." );
            }
        }


        /// <summary> Returns a String that represents the current NbtCompound object and its contents.
        /// Format: TAG_Compound("Name"): { ...contents... } </summary>
        /// <returns> A String that represents the current NbtCompound object and its contents. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, "\t", 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Compound" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries {{", tags.Count );

            if( Count > 0 ) {
                sb.Append( '\n' );
                foreach( NbtTag tag in tags.Values ) {
                    tag.PrettyPrint( sb, indentString, indentLevel + 1 );
                    sb.Append( '\n' );
                }
                for( int i = 0; i < indentLevel; i++ ) {
                    sb.Append( indentString );
                }
            }
            sb.Append( '}' );
        }
    }


    /// <summary> A tag containing a double-precision floating point number. </summary>
    public sealed class NbtDouble : NbtTag {
        /// <summary> Type of this tag (Double). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Double;
            }
        }

        /// <summary> Value/payload of this tag (a double-precision floating point number). </summary>
        public double Value { get; set; }


        /// <summary> Creates an unnamed NbtDouble tag with the default value of 0. </summary>
        public NbtDouble() { }


        /// <summary> Creates an unnamed NbtDouble tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtDouble( double value )
            : this( null, value ) { }


        /// <summary> Creates an NbtDouble tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtDouble( [CanBeNull] string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtDouble tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtDouble( [CanBeNull] string tagName, double value ) {
            Name = tagName;
            Value = value;
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadDouble();
                return false;
            }
            Value = readStream.ReadDouble();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadDouble();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Double );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }


        /// <summary> Returns a String that represents the current NbtDouble object.
        /// Format: TAG_Double("Name"): Value </summary>
        /// <returns> A String that represents the current NbtDouble object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Double" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing a single-precision floating point number. </summary>
    public sealed class NbtFloat : NbtTag {
        /// <summary> Type of this tag (Float). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Float;
            }
        }

        /// <summary> Value/payload of this tag (a single-precision floating point number). </summary>
        public float Value { get; set; }


        /// <summary> Creates an unnamed NbtFloat tag with the default value of 0f. </summary>
        public NbtFloat() { }


        /// <summary> Creates an unnamed NbtFloat tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtFloat( float value )
            : this( null, value ) { }


        /// <summary> Creates an NbtFloat tag with the given name and the default value of 0f. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtFloat( [CanBeNull] string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtFloat tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtFloat( [CanBeNull] string tagName, float value ) {
            Name = tagName;
            Value = value;
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadSingle();
                return false;
            }
            Value = readStream.ReadSingle();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadSingle();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Float );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }


        /// <summary> Returns a String that represents the current NbtFloat object.
        /// Format: TAG_Float("Name"): Value </summary>
        /// <returns> A String that represents the current NbtFloat object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Float" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing a single signed 32-bit integer. </summary>
    public sealed class NbtInt : NbtTag {
        /// <summary> Type of this tag (Int). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Int;
            }
        }

        /// <summary> Value/payload of this tag (a single signed 32-bit integer). </summary>
        public int Value { get; set; }


        /// <summary> Creates an unnamed NbtInt tag with the default value of 0. </summary>
        public NbtInt() { }


        /// <summary> Creates an unnamed NbtInt tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtInt( int value )
            : this( null, value ) { }


        /// <summary> Creates an NbtInt tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtInt( [CanBeNull] string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtInt tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtInt( [CanBeNull] string tagName, int value ) {
            Name = tagName;
            Value = value;
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadInt32();
                return false;
            }
            Value = readStream.ReadInt32();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadInt32();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Int );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }


        /// <summary> Returns a String that represents the current NbtInt object.
        /// Format: TAG_Int("Name"): Value </summary>
        /// <returns> A String that represents the current NbtInt object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Int" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing an array of signed 32-bit integers. </summary>
    public sealed class NbtIntArray : NbtTag {
        /// <summary> Type of this tag (ByteArray). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.IntArray;
            }
        }


        /// <summary> Value/payload of this tag (an array of signed 32-bit integers). May not be <c>null</c>. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        [NotNull]
        public int[] Value {
            get {
                return ints;
            }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                ints = value;
            }
        }

        [NotNull]
        int[] ints;


        /// <summary> Creates an unnamed NbtIntArray tag, containing an empty array of ints. </summary>
        public NbtIntArray()
            : this( null, new int[0] ) { }


        /// <summary> Creates an unnamed NbtIntArray tag, containing the given array of ints. </summary>
        /// <param name="value"> Int array to assign to this tag's Value. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtIntArray( [NotNull] int[] value )
            : this( null, value ) { }


        /// <summary> Creates an NbtIntArray tag with the given name, containing an empty array of ints. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtIntArray( [CanBeNull] string tagName )
            : this( tagName, new int[0] ) { }


        /// <summary> Creates an NbtIntArray tag with the given name, containing the given array of ints. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Int array to assign to this tag's Value. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtIntArray( [CanBeNull] string tagName, [NotNull] int[] value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = (int[])value.Clone();
        }


        /// <summary> Gets or sets an integer at the given index. </summary>
        /// <param name="tagIndex"> The zero-based index of the element to get or set. </param>
        /// <returns> The integer at the specified index. </returns>
        /// <exception cref="IndexOutOfRangeException"> <paramref name="tagIndex"/> is outside the array bounds. </exception>
        public new int this[int tagIndex] {
            get {
                return Value[tagIndex];
            }
            set {
                Value[tagIndex] = value;
            }
        }


        internal override bool ReadTag( NbtBinaryReader readStream ) {
            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative length given in TAG_Int_Array" );
            }

            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.Skip( length * sizeof( int ) );
                return false;
            }

            Value = new int[length];
            for( int i = 0; i < length; i++ ) {
                Value[i] = readStream.ReadInt32();
            }
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative length given in TAG_Int_Array" );
            }
            readStream.Skip( length * sizeof( int ) );
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.IntArray );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value.Length );
            for( int i = 0; i < Value.Length; i++ ) {
                writeStream.Write( Value[i] );
            }
        }


        /// <summary> Returns a String that represents the current NbtIntArray object.
        /// Format: TAG_Int_Array("Name"): [N ints] </summary>
        /// <returns> A String that represents the current NbtIntArray object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Int_Array" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": [{0} ints]", ints.Length );
        }
    }


    /// <summary> A tag containing a list of unnamed tags, all of the same kind. </summary>
    public sealed class NbtList : NbtTag, IList<NbtTag>, IList {
        /// <summary> Type of this tag (List). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.List;
            }
        }

        [NotNull]
        readonly List<NbtTag> tags;


        /// <summary> Gets or sets the tag type of this list. All tags in this NbtTag must be of the same type. </summary>
        /// <exception cref="ArgumentException"> If the given NbtTagType does not match the type of existing list items (for non-empty lists). </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If the given NbtTagType is not among recognized tag types. </exception>
        public NbtTagType ListType {
            get {
                return listType;
            }
            set {
                if( !Enum.IsDefined( typeof( NbtTagType ), value ) ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                foreach( var tag in tags ) {
                    if( tag.TagType != value ) {
                        throw new ArgumentException( "All list items must be of specified tag type." );
                    }
                }
                listType = value;
            }
        }

        NbtTagType listType;


        /// <summary> Creates an unnamed NbtList with empty contents and undefined ListType. </summary>
        public NbtList()
            : this( null, null, NbtTagType.Unknown ) { }


        /// <summary> Creates an NbtList with given name, empty contents, and undefined ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtList( [CanBeNull] string tagName )
            : this( tagName, null, NbtTagType.Unknown ) { }


        /// <summary> Creates an unnamed NbtList with the given contents, and inferred ListType. 
        /// If given tag array is empty, NbtTagType remains Unknown. </summary>
        /// <param name="tags"> Collection of tags to insert into the list. All tags are expected to be of the same type.
        /// ListType is inferred from the first tag. List may be empty, but may not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If given tags are of mixed types. </exception>
        public NbtList( [NotNull] IEnumerable<NbtTag> tags )
            : this( null, tags, NbtTagType.Unknown ) {
            if( tags == null )
                throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an unnamed NbtList with empty contents and an explicitly specified ListType.
        /// If ListType is Unknown, it will be inferred from the type of the first added tag.
        /// Otherwise, all tags added to this list are expected to be of the given type. </summary>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown. </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="givenListType"/> is not a recognized tag type. </exception>
        public NbtList( NbtTagType givenListType )
            : this( null, null, givenListType ) { }


        /// <summary> Creates an NbtList with the given name and contents, and inferred ListType. 
        /// If given tag array is empty, NbtTagType remains Unknown. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="tags"> Collection of tags to insert into the list. All tags are expected to be of the same type.
        /// ListType is inferred from the first tag. List may be empty, but may not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If given tags are of mixed types. </exception>
        public NbtList( [CanBeNull] string tagName, [NotNull] IEnumerable<NbtTag> tags )
            : this( tagName, tags, NbtTagType.Unknown ) {
            if( tags == null )
                throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an unnamed NbtList with the given contents, and an explicitly specified ListType. </summary>
        /// <param name="tags"> Collection of tags to insert into the list.
        /// All tags are expected to be of the same type (matching givenListType).
        /// List may be empty, but may not be <c>null</c>. </param>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown (to infer type from the first element of tags). </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tags"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="givenListType"/> is not a recognized tag type. </exception>
        /// <exception cref="ArgumentException"> If given tags do not match <paramref name="givenListType"/>, or are of mixed types. </exception>
        public NbtList( [NotNull] IEnumerable<NbtTag> tags, NbtTagType givenListType )
            : this( null, tags, givenListType ) {
            if( tags == null )
                throw new ArgumentNullException( "tags" );
        }


        /// <summary> Creates an NbtList with the given name, empty contents, and an explicitly specified ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="givenListType"> Name to assign to this tag.
        /// If givenListType is Unknown, ListType will be infered from the first tag added to this NbtList. </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="givenListType"/> is not a recognized tag type. </exception>
        public NbtList( [CanBeNull] string tagName, NbtTagType givenListType )
            : this( tagName, null, givenListType ) { }


        /// <summary> Creates an NbtList with the given name and contents, and an explicitly specified ListType. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="tags"> Collection of tags to insert into the list.
        /// All tags are expected to be of the same type (matching givenListType). May be empty or <c>null</c>. </param>
        /// <param name="givenListType"> Name to assign to this tag. May be Unknown (to infer type from the first element of tags). </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="givenListType"/> is not a recognized tag type. </exception>
        /// <exception cref="ArgumentException"> If given tags do not match <paramref name="givenListType"/>, or are of mixed types. </exception>
        public NbtList( [CanBeNull] string tagName, [CanBeNull] IEnumerable<NbtTag> tags, NbtTagType givenListType ) {
            Name = tagName;
            this.tags = new List<NbtTag>();
            listType = givenListType;

            if( !Enum.IsDefined( typeof( NbtTagType ), givenListType ) ) {
                throw new ArgumentOutOfRangeException( "givenListType" );
            }

            if( tags == null )
                return;
            foreach( NbtTag tag in tags ) {
                Add( tag );
            }
        }


        /// <summary> Gets or sets the tag at the specified index. </summary>
        /// <returns> The tag at the specified index. </returns>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="tagIndex"/> is not a valid index in the NbtList. </exception>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> Given tag's type does not match ListType. </exception>
        [NotNull]
        public override NbtTag this[int tagIndex] {
            get {
                return tags[tagIndex];
            }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                } else if( value.Parent != null ) {
                    throw new ArgumentException( "A tag may only be added to one compound/list at a time." );
                }
                if( listType == NbtTagType.Unknown ) {
                    listType = value.TagType;
                } else if( value.TagType != listType ) {
                    throw new ArgumentException( "Items must be of type " + listType );
                }
                tags[tagIndex] = value;
                value.Parent = this;
            }
        }


        /// <summary> Gets or sets the tag with the specified name. </summary>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <typeparam name="T"> Type to cast the result to. Must derive from NbtTag. </typeparam>
        /// <returns> The tag with the specified key. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="tagIndex"/> is not a valid index in the NbtList. </exception>
        /// <exception cref="InvalidCastException"> If tag could not be cast to the desired tag. </exception>
        [NotNull, Pure]
        public T Get<T>( int tagIndex ) where T : NbtTag {
            return (T)tags[tagIndex];
        }


        /// <summary> Adds all tags from the specified collection to the end of this NbtList. </summary>
        /// <param name="newTags"> The collection whose elements should be added to this NbtList. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="newTags"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If given tags do not match ListType, or are of mixed types. </exception>
        public void AddRange( [NotNull] IEnumerable<NbtTag> newTags ) {
            if( newTags == null )
                throw new ArgumentNullException( "newTags" );
            foreach( NbtTag tag in newTags ) {
                Add( tag );
            }
        }


        /// <summary> Copies all tags in this NbtList to an array. </summary>
        /// <returns> Array of NbtTags. </returns>
        [NotNull, Pure]
        // ReSharper disable ReturnTypeCanBeEnumerable.Global
        public NbtTag[] ToArray() {
            // ReSharper restore ReturnTypeCanBeEnumerable.Global
            return tags.ToArray();
        }


        /// <summary> Copies all tags in this NbtList to an array, and casts it to the desired type. </summary>
        /// <typeparam name="T"> Type to cast every member of NbtList to. Must derive from NbtTag. </typeparam>
        /// <returns> Array of NbtTags cast to the desired type. </returns>
        /// <exception cref="InvalidCastException"> If contents of this list cannot be cast to the given type. </exception>
        [NotNull, Pure]
        public T[] ToArray<T>() where T : NbtTag {
            T[] result = new T[tags.Count];
            for( int i = 0; i < result.Length; i++ ) {
                result[i] = (T)tags[i];
            }
            return result;
        }


        #region Reading / Writing

        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                SkipTag( readStream );
                return false;
            }

            ListType = readStream.ReadTagType();

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative count given in TAG_List" );
            }

            for( int i = 0; i < length; i++ ) {
                NbtTag newTag;
                switch( ListType ) {
                    case NbtTagType.Byte:
                        newTag = new NbtByte();
                        break;
                    case NbtTagType.Short:
                        newTag = new NbtShort();
                        break;
                    case NbtTagType.Int:
                        newTag = new NbtInt();
                        break;
                    case NbtTagType.Long:
                        newTag = new NbtLong();
                        break;
                    case NbtTagType.Float:
                        newTag = new NbtFloat();
                        break;
                    case NbtTagType.Double:
                        newTag = new NbtDouble();
                        break;
                    case NbtTagType.ByteArray:
                        newTag = new NbtByteArray();
                        break;
                    case NbtTagType.String:
                        newTag = new NbtString();
                        break;
                    case NbtTagType.List:
                        newTag = new NbtList();
                        break;
                    case NbtTagType.Compound:
                        newTag = new NbtCompound();
                        break;
                    case NbtTagType.IntArray:
                        newTag = new NbtIntArray();
                        break;
                    default:
                        // should never happen, since ListType is checked beforehand
                        throw new NbtFormatException( "Unsupported tag type found in NBT_Compound" );
                }
                newTag.Parent = this;
                if( newTag.ReadTag( readStream ) ) {
                    tags.Add( newTag );
                }
            }
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            // read list type, and make sure it's defined
            ListType = readStream.ReadTagType();

            int length = readStream.ReadInt32();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative count given in TAG_List" );
            }

            switch( ListType ) {
                case NbtTagType.Byte:
                    readStream.Skip( length );
                    break;
                case NbtTagType.Short:
                    readStream.Skip( length * sizeof( short ) );
                    break;
                case NbtTagType.Int:
                    readStream.Skip( length * sizeof( int ) );
                    break;
                case NbtTagType.Long:
                    readStream.Skip( length * sizeof( long ) );
                    break;
                case NbtTagType.Float:
                    readStream.Skip( length * sizeof( float ) );
                    break;
                case NbtTagType.Double:
                    readStream.Skip( length * sizeof( double ) );
                    break;
                default:
                    for( int i = 0; i < length; i++ ) {
                        switch( listType ) {
                            case NbtTagType.ByteArray:
                                new NbtByteArray().SkipTag( readStream );
                                break;
                            case NbtTagType.String:
                                readStream.SkipString();
                                break;
                            case NbtTagType.List:
                                new NbtList().SkipTag( readStream );
                                break;
                            case NbtTagType.Compound:
                                new NbtCompound().SkipTag( readStream );
                                break;
                            case NbtTagType.IntArray:
                                new NbtIntArray().SkipTag( readStream );
                                break;
                        }
                    }
                    break;
            }
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.List );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            WriteData( writeStream );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            if( ListType == NbtTagType.Unknown ) {
                throw new NbtFormatException( "NbtList had no elements and an Unknown ListType" );
            }
            writeStream.Write( ListType );
            writeStream.Write( tags.Count );
            foreach( NbtTag tag in tags ) {
                tag.WriteData( writeStream );
            }
        }

        #endregion


        #region Implementation of IEnumerable<NBtTag> and IEnumerable

        /// <summary> Returns an enumerator that iterates through all tags in this NbtList. </summary>
        /// <returns> An IEnumerator&gt;NbtTag&lt; that can be used to iterate through the list. </returns>
        public IEnumerator<NbtTag> GetEnumerator() {
            return tags.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return tags.GetEnumerator();
        }

        #endregion


        #region Implementation of IList<NbtTag> and ICollection<NbtTag>

        /// <summary> Determines the index of a specific tag in this NbtList </summary>
        /// <returns> The index of tag if found in the list; otherwise, -1. </returns>
        /// <param name="tag"> The tag to locate in this NbtList. </param>
        public int IndexOf( [NotNull] NbtTag tag ) {
            if( tag == null )
                throw new ArgumentNullException( "tag" );
            return tags.IndexOf( tag );
        }


        /// <summary> Inserts an item to this NbtList at the specified index. </summary>
        /// <param name="tagIndex"> The zero-based index at which newTag should be inserted. </param>
        /// <param name="newTag"> The tag to insert into this NbtList. </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="tagIndex"/> is not a valid index in this NbtList. </exception>
        /// <exception cref="ArgumentNullException"> <paramref name="newTag"/> is <c>null</c>. </exception>
        public void Insert( int tagIndex, [NotNull] NbtTag newTag ) {
            if( newTag == null )
                throw new ArgumentNullException( "newTag" );
            if( listType == NbtTagType.Unknown ) {
                listType = newTag.TagType;
            } else if( newTag.TagType != listType ) {
                throw new ArgumentException( "Items must be of type " + listType );
            } else if( newTag.Parent != null ) {
                throw new ArgumentException( "A tag may only be added to one compound/list at a time." );
            }
            tags.Insert( tagIndex, newTag );
            newTag.Parent = this;
        }


        /// <summary> Removes a tag at the specified index from this NbtList. </summary>
        /// <param name="index"> The zero-based index of the item to remove. </param>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is not a valid index in the NbtList. </exception>
        public void RemoveAt( int index ) {
            NbtTag tag = this[index];
            tags.RemoveAt( index );
            tag.Parent = null;
        }


        /// <summary> Adds a tag to this NbtList. </summary>
        /// <param name="newTag"> The tag to add to this NbtList. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="newTag"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If <paramref name="newTag"/> does not match ListType. </exception>
        public void Add( [NotNull] NbtTag newTag ) {
            if( newTag == null )
                throw new ArgumentNullException( "newTag" );
            if( listType == NbtTagType.Unknown ) {
                listType = newTag.TagType;
            } else if( newTag.TagType != listType ) {
                throw new ArgumentException( "Items in this list must be of type " + listType + ". Given type: " + newTag.TagType );
            } else if( newTag.Parent != null ) {
                throw new ArgumentException( "A tag may only be added to one compound/list at a time." );
            }
            tags.Add( newTag );
            newTag.Parent = this;
        }


        /// <summary> Removes all tags from this NbtList. </summary>
        public void Clear() {
            for( int i = 0; i < tags.Count; i++ ) {
                tags[i].Parent = null;
            }
            tags.Clear();
        }


        /// <summary> Determines whether this NbtList contains a specific tag. </summary>
        /// <returns> true if given tag is found in this NbtList; otherwise, false. </returns>
        /// <param name="item"> The tag to locate in this NbtList. </param>
        public bool Contains( [NotNull] NbtTag item ) {
            if( item == null )
                throw new ArgumentNullException( "item" );
            return tags.Contains( item );
        }


        /// <summary> Copies the tags of this NbtList to an array, starting at a particular array index. </summary>
        /// <param name="array"> The one-dimensional array that is the destination of the tag copied from NbtList.
        /// The array must have zero-based indexing. </param>
        /// <param name="arrayIndex"> The zero-based index in array at which copying begins. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="array"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> arrayIndex is less than 0. </exception>
        /// <exception cref="ArgumentException"> Given array is multidimensional; arrayIndex is equal to or greater than the length of array;
        /// the number of tags in this NbtList is greater than the available space from arrayIndex to the end of the destination array;
        /// or type NbtTag cannot be cast automatically to the type of the destination array. </exception>
        public void CopyTo( NbtTag[] array, int arrayIndex ) {
            tags.CopyTo( array, arrayIndex );
        }


        /// <summary> Removes the first occurrence of a specific NbtTag from the NbtCompound.
        /// Looks for exact object matches, not name matches. </summary>
        /// <returns> true if tag was successfully removed from this NbtList; otherwise, false.
        /// This method also returns false if tag is not found. </returns>
        /// <param name="tag"> The tag to remove from this NbtList. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="tag"/> is <c>null</c>. </exception>
        public bool Remove( [NotNull] NbtTag tag ) {
            if( tag == null )
                throw new ArgumentNullException( "tag" );
            if( !tags.Remove( tag ) ) {
                return false;
            }
            tag.Parent = null;
            return true;
        }


        /// <summary> Gets the number of tags contained in the NbtList. </summary>
        /// <returns> The number of tags contained in the NbtList. </returns>
        public int Count {
            get {
                return tags.Count;
            }
        }


        bool ICollection<NbtTag>.IsReadOnly {
            get {
                return false;
            }
        }

        #endregion


        #region Implementation of IList and ICollection

        void IList.Remove( object value ) {
            NbtTag val = (NbtTag)value;
            if( tags.Remove( val ) ) {
                val.Parent = null;
            }
        }


        object IList.this[int tagIndex] {
            get {
                return tags[tagIndex];
            }
            set {
                this[tagIndex] = (NbtTag)value;
            }
        }


        int IList.Add( object value ) {
            Add( (NbtTag)value );
            return (tags.Count - 1);
        }


        bool IList.Contains( object value ) {
            return tags.Contains( (NbtTag)value );
        }


        int IList.IndexOf( object value ) {
            return tags.IndexOf( (NbtTag)value );
        }


        void IList.Insert( int index, object value ) {
            Insert( index, (NbtTag)value );
        }


        bool IList.IsFixedSize {
            get {
                return false;
            }
        }


        void ICollection.CopyTo( Array array, int index ) {
            CopyTo( (NbtTag[])array, index );
        }


        object ICollection.SyncRoot {
            get {
                return (tags as ICollection).SyncRoot;
            }
        }


        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }


        bool IList.IsReadOnly {
            get {
                return false;
            }
        }

        #endregion


        // Hiding NbtTag[string]:NbtTag
        new NbtTag this[string tagName] {
            get { throw new InvalidOperationException( "Integer indexers only work on NbtList tags." ); }
            set { throw new InvalidOperationException( "Integer indexers only work on NbtList tags." ); }
        }


        /// <summary> Returns a String that represents the current NbtList object and its contents.
        /// Format: TAG_List("Name"): { ...contents... } </summary>
        /// <returns> A String that represents the current NbtList object and its contents. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, "\t", 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_List" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.AppendFormat( ": {0} entries {{", tags.Count );

            if( Count > 0 ) {
                sb.Append( '\n' );
                foreach( NbtTag tag in tags ) {
                    tag.PrettyPrint( sb, indentString, indentLevel + 1 );
                    sb.Append( '\n' );
                }
                for( int i = 0; i < indentLevel; i++ ) {
                    sb.Append( indentString );
                }
            }
            sb.Append( '}' );
        }
    }


    /// <summary> A tag containing a single signed 64-bit integer. </summary>
    public sealed class NbtLong : NbtTag {
        /// <summary> Type of this tag (Long). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Long;
            }
        }

        /// <summary> Value/payload of this tag (a single signed 64-bit integer). </summary>
        public long Value { get; set; }


        /// <summary> Creates an unnamed NbtLong tag with the default value of 0. </summary>
        public NbtLong() { }


        /// <summary> Creates an unnamed NbtLong tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtLong( long value )
            : this( null, value ) { }


        /// <summary> Creates an NbtLong tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtLong( string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtLong tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtLong( string tagName, long value ) {
            Name = tagName;
            Value = value;
        }


        #region Reading / Writing

        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadInt64();
                return false;
            }
            Value = readStream.ReadInt64();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadInt64();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Long );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtLong object.
        /// Format: TAG_Long("Name"): Value </summary>
        /// <returns> A String that represents the current NbtLong object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Long" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing a single signed 16-bit integer. </summary>
    public sealed class NbtShort : NbtTag {
        /// <summary> Type of this tag (Short). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.Short;
            }
        }

        /// <summary> Value/payload of this tag (a single signed 16-bit integer). </summary>
        public short Value { get; set; }


        /// <summary> Creates an unnamed NbtShort tag with the default value of 0. </summary>
        public NbtShort() { }


        /// <summary> Creates an unnamed NbtShort tag with the given value. </summary>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtShort( short value )
            : this( null, value ) { }


        /// <summary> Creates an NbtShort tag with the given name and the default value of 0. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        public NbtShort( [CanBeNull] string tagName )
            : this( tagName, 0 ) { }


        /// <summary> Creates an NbtShort tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> Value to assign to this tag. </param>
        public NbtShort( [CanBeNull] string tagName, short value ) {
            Name = tagName;
            Value = value;
        }


        #region Reading / Writing

        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.ReadInt16();
                return false;
            }
            Value = readStream.ReadInt16();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.ReadInt16();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.Short );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtShort object.
        /// Format: TAG_Short("Name"): Value </summary>
        /// <returns> A String that represents the current NbtShort object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_Short" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": " );
            sb.Append( Value );
        }
    }


    /// <summary> A tag containing a single string. String is stored in UTF-8 encoding. </summary>
    public sealed class NbtString : NbtTag {
        /// <summary> Type of this tag (String). </summary>
        public override NbtTagType TagType {
            get {
                return NbtTagType.String;
            }
        }

        /// <summary> Value/payload of this tag (a single string). May not be <c>null</c>. </summary>
        [NotNull]
        public string Value {
            get {
                return stringVal;
            }
            set {
                if( value == null ) {
                    throw new ArgumentNullException( "value" );
                }
                stringVal = value;
            }
        }

        [NotNull]
        string stringVal;


        /// <summary> Creates an unnamed NbtString tag with the default value (empty string). </summary>
        public NbtString() { }


        /// <summary> Creates an unnamed NbtString tag with the given value. </summary>
        /// <param name="value"> String value to assign to this tag. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtString( [NotNull] string value )
            : this( null, value ) { }


        /// <summary> Creates an NbtString tag with the given name and value. </summary>
        /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
        /// <param name="value"> String value to assign to this tag. May not be <c>null</c>. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        public NbtString( [CanBeNull] string tagName, [NotNull] string value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            Name = tagName;
            Value = value;
        }


        #region Reading / Writing

        internal override bool ReadTag( NbtBinaryReader readStream ) {
            if( readStream.Selector != null && !readStream.Selector( this ) ) {
                readStream.SkipString();
                return false;
            }
            Value = readStream.ReadString();
            return true;
        }


        internal override void SkipTag( NbtBinaryReader readStream ) {
            readStream.SkipString();
        }


        internal override void WriteTag( NbtBinaryWriter writeStream, bool writeName ) {
            writeStream.Write( NbtTagType.String );
            if( writeName ) {
                if( Name == null )
                    throw new NbtFormatException( "Name is null" );
                writeStream.Write( Name );
            }
            writeStream.Write( Value );
        }


        internal override void WriteData( NbtBinaryWriter writeStream ) {
            writeStream.Write( Value );
        }

        #endregion


        /// <summary> Returns a String that represents the current NbtString object.
        /// Format: TAG_String("Name"): Value </summary>
        /// <returns> A String that represents the current NbtString object. </returns>
        public override string ToString() {
            var sb = new StringBuilder();
            PrettyPrint( sb, null, 0 );
            return sb.ToString();
        }


        internal override void PrettyPrint( StringBuilder sb, string indentString, int indentLevel ) {
            for( int i = 0; i < indentLevel; i++ ) {
                sb.Append( indentString );
            }
            sb.Append( "TAG_String" );
            if( !String.IsNullOrEmpty( Name ) ) {
                sb.AppendFormat( "(\"{0}\")", Name );
            }
            sb.Append( ": \"" );
            sb.Append( Value );
            sb.Append( '"' );
        }
    }


    /// <summary> Base class for different kinds of named binary tags. </summary>
    public abstract class NbtTag {
        /// <summary> Parent compound tag, either NbtList or NbtCompound, if any.
        /// May be <c>null</c> for detached tags. </summary>
        [CanBeNull]
        public NbtTag Parent { get; internal set; }


        /// <summary> Type of this tag. </summary>
        public abstract NbtTagType TagType { get; }


        /// <summary> Returns true if tags of this type have a value attached.
        /// All tags except Compound, List, and End have values. </summary>
        public bool HasValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Compound:
                    case NbtTagType.End:
                    case NbtTagType.List:
                    case NbtTagType.Unknown:
                        return false;
                    default:
                        return true;
                }
            }
        }


        /// <summary> Name of this tag. Immutable, and set by the constructor. May be <c>null</c>. </summary>
        /// <exception cref="ArgumentNullException"> If <paramref name="value"/> is <c>null</c>, and <c>Parent</c> tag is an NbtCompound.
        /// Name of tags inside an <c>NbtCompound</c> may not be null. </exception>
        [CanBeNull]
        public string Name {
            get {
                return name;
            }
            set {
                if( name == value ) {
                    return;
                } else if( Parent == null ) {
                    name = value;
                    return;
                }

                NbtCompound parentAsCompound = Parent as NbtCompound;
                if( parentAsCompound != null ) {
                    if( value == null ) {
                        throw new ArgumentNullException( "value", "Name of tags inside an NbtCompound may not be null." );
                    } else if( parentAsCompound.Contains( value ) ) {
                        throw new ArgumentException(
                            "Cannot rename: a tag with the name already exists in this tag's parent NbtCompound.",
                            "value" );
                    } else if( name != null ) {
                        parentAsCompound.RenameTag( name, value );
                    }
                }

                name = value;
            }
        }

        string name;


        /// <summary> Gets the full name of this tag, including all parent tag names, separated by dots. 
        /// Unnamed tags show up as empty strings. </summary>
        [NotNull]
        public string Path {
            get {
                if( Parent == null ) {
                    return Name ?? "";
                }
                NbtList parentAsList = Parent as NbtList;
                if( parentAsList != null ) {
                    return parentAsList.Path + '[' + parentAsList.IndexOf( this ) + ']';
                } else {
                    return Parent.Path + '.' + Name;
                }
            }
        }


        internal abstract bool ReadTag( NbtBinaryReader readStream );


        internal abstract void SkipTag( NbtBinaryReader readStream );


        internal abstract void WriteTag( [NotNull] NbtBinaryWriter writeReader, bool writeName );


        // WriteData does not write the tag's ID byte or the name
        internal abstract void WriteData( [NotNull] NbtBinaryWriter writeReader );


        #region Shortcuts

        /// <summary> Gets or sets the tag with the specified name. May return <c>null</c>. </summary>
        /// <returns> The tag with the specified key. Null if tag with the given name was not found. </returns>
        /// <param name="tagName"> The name of the tag to get or set. Must match tag's actual name. </param>
        /// <exception cref="InvalidOperationException"> If used on a tag that is not NbtCompound. </exception>
        /// <remarks> ONLY APPLICABLE TO NbtCompound OBJECTS!
        /// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
        public virtual NbtTag this[string tagName] {
            get {
                throw new InvalidOperationException( "String indexers only work on NbtCompound tags." );
            }
            set {
                throw new InvalidOperationException( "String indexers only work on NbtCompound tags." );
            }
        }


        /// <summary> Gets or sets the tag at the specified index. </summary>
        /// <returns> The tag at the specified index. </returns>
        /// <param name="tagIndex"> The zero-based index of the tag to get or set. </param>
        /// <exception cref="ArgumentOutOfRangeException"> tagIndex is not a valid index in this tag. </exception>
        /// <exception cref="ArgumentNullException"> Given tag is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> Given tag's type does not match ListType. </exception>
        /// <exception cref="InvalidOperationException"> If used on a tag that is not NbtList, NbtByteArray, or NbtIntArray. </exception>
        /// <remarks> ONLY APPLICABLE TO NbtList, NbtByteArray, and NbtIntArray OBJECTS!
        /// Included in NbtTag base class for programmers' convenience, to avoid extra type casts. </remarks>
        public virtual NbtTag this[int tagIndex] {
            get {
                throw new InvalidOperationException( "Integer indexers only work on NbtList tags." );
            }
            set {
                throw new InvalidOperationException( "Integer indexers only work on NbtList tags." );
            }
        }


        /// <summary> Returns the value of this tag, cast as a byte.
        /// Only supported by NbtByte tags. </summary>
        /// <exception cref="InvalidCastException"> When used on a tag other than NbtByte. </exception>
        public byte ByteValue {
            get {
                if( TagType == NbtTagType.Byte ) {
                    return ((NbtByte)this).Value;
                } else {
                    throw new InvalidCastException( "Cannot get ByteValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a short (16-bit signed integer).
        /// Only supported by NbtByte and NbtShort. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public short ShortValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value;
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value;
                    default:
                        throw new InvalidCastException( "Cannot get ShortValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as an int (32-bit signed integer).
        /// Only supported by NbtByte, NbtShort, and NbtInt. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public int IntValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value;
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value;
                    case NbtTagType.Int:
                        return ((NbtInt)this).Value;
                    default:
                        throw new InvalidCastException( "Cannot get IntValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a long (64-bit signed integer).
        /// Only supported by NbtByte, NbtShort, NbtInt, and NbtLong. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public long LongValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value;
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value;
                    case NbtTagType.Int:
                        return ((NbtInt)this).Value;
                    case NbtTagType.Long:
                        return ((NbtLong)this).Value;
                    default:
                        throw new InvalidCastException( "Cannot get LongValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a long (64-bit signed integer).
        /// Only supported by NbtFloat and, with loss of precision, by NbtDouble, NbtByte, NbtShort, NbtInt, and NbtLong. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public float FloatValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value;
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value;
                    case NbtTagType.Int:
                        return ((NbtInt)this).Value;
                    case NbtTagType.Long:
                        return ((NbtLong)this).Value;
                    case NbtTagType.Float:
                        return ((NbtFloat)this).Value;
                    case NbtTagType.Double:
                        return (float)((NbtDouble)this).Value;
                    default:
                        throw new InvalidCastException( "Cannot get FloatValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a long (64-bit signed integer).
        /// Only supported by NbtFloat, NbtDouble, and, with loss of precision, by NbtByte, NbtShort, NbtInt, and NbtLong. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public double DoubleValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value;
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value;
                    case NbtTagType.Int:
                        return ((NbtInt)this).Value;
                    case NbtTagType.Long:
                        return ((NbtLong)this).Value;
                    case NbtTagType.Float:
                        return ((NbtFloat)this).Value;
                    case NbtTagType.Double:
                        return ((NbtDouble)this).Value;
                    default:
                        throw new InvalidCastException( "Cannot get DoubleValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a byte array.
        /// Only supported by NbtByteArray tags. </summary>
        /// <exception cref="InvalidCastException"> When used on a tag other than NbtByteArray. </exception>
        public byte[] ByteArrayValue {
            get {
                if( TagType == NbtTagType.ByteArray ) {
                    return ((NbtByteArray)this).Value;
                } else {
                    throw new InvalidCastException( "Cannot get ByteArrayValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as an int array.
        /// Only supported by NbtIntArray tags. </summary>
        /// <exception cref="InvalidCastException"> When used on a tag other than NbtIntArray. </exception>
        public int[] IntArrayValue {
            get {
                if( TagType == NbtTagType.IntArray ) {
                    return ((NbtIntArray)this).Value;
                } else {
                    throw new InvalidCastException( "Cannot get IntArrayValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }


        /// <summary> Returns the value of this tag, cast as a string.
        /// Returns exact value for NbtString, and stringified (using InvariantCulture) value for NbtByte, NbtDouble, NbtFloat, NbtInt, NbtLong, and NbtShort.
        /// Not supported by NbtCompound, NbtList, NbtByteArray, or NbtIntArray. </summary>
        /// <exception cref="InvalidCastException"> When used on an unsupported tag. </exception>
        public string StringValue {
            get {
                switch( TagType ) {
                    case NbtTagType.String:
                        return ((NbtString)this).Value;
                    case NbtTagType.Byte:
                        return ((NbtByte)this).Value.ToString( CultureInfo.InvariantCulture );
                    case NbtTagType.Double:
                        return ((NbtDouble)this).Value.ToString( CultureInfo.InvariantCulture );
                    case NbtTagType.Float:
                        return ((NbtFloat)this).Value.ToString( CultureInfo.InvariantCulture );
                    case NbtTagType.Int:
                        return ((NbtInt)this).Value.ToString( CultureInfo.InvariantCulture );
                    case NbtTagType.Long:
                        return ((NbtLong)this).Value.ToString( CultureInfo.InvariantCulture );
                    case NbtTagType.Short:
                        return ((NbtShort)this).Value.ToString( CultureInfo.InvariantCulture );
                    default:
                        throw new InvalidCastException( "Cannot get StringValue from " + GetCanonicalTagName( TagType ) );
                }
            }
        }

        #endregion


        /// <summary> Returns a canonical (Notchy) name for the given NbtTagType,
        /// e.g. "TAG_Byte_Array" for NbtTagType.ByteArray </summary>
        /// <param name="type"> NbtTagType to name. </param>
        /// <returns> String representing the canonical name of a tag,
        /// or null of given TagType does not have a canonical name (e.g. Unknown). </returns>
        [CanBeNull]
        public static string GetCanonicalTagName( NbtTagType type ) {
            switch( type ) {
                case NbtTagType.Byte:
                    return "TAG_Byte";
                case NbtTagType.ByteArray:
                    return "TAG_Byte_Array";
                case NbtTagType.Compound:
                    return "TAG_Compound";
                case NbtTagType.Double:
                    return "TAG_Double";
                case NbtTagType.End:
                    return "TAG_End";
                case NbtTagType.Float:
                    return "TAG_Float";
                case NbtTagType.Int:
                    return "TAG_Int";
                case NbtTagType.IntArray:
                    return "TAG_Int_Array";
                case NbtTagType.List:
                    return "TAG_List";
                case NbtTagType.Long:
                    return "TAG_Long";
                case NbtTagType.Short:
                    return "TAG_Short";
                case NbtTagType.String:
                    return "TAG_String";
                default:
                    return null;
            }
        }


        /// <summary> Prints contents of this tag, and any child tags, to a string.
        /// Indents the string using multiples of the given indentation string. </summary>
        /// <returns> A string representing contants of this tag, and all child tags (if any). </returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            PrettyPrint( sb, DefaultIndentString, 0 );
            return sb.ToString();
        }


        /// <summary> Prints contents of this tag, and any child tags, to a string.
        /// Indents the string using multiples of the given indentation string. </summary>
        /// <param name="indentString"> String to be used for indentation. </param>
        /// <returns> A string representing contants of this tag, and all child tags (if any). </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="indentString"/> is <c>null</c>. </exception>
        [NotNull]
        public string ToString( [NotNull] string indentString ) {
            if( indentString == null )
                throw new ArgumentNullException( "indentString" );
            StringBuilder sb = new StringBuilder();
            PrettyPrint( sb, indentString, 0 );
            return sb.ToString();
        }


        internal abstract void PrettyPrint( StringBuilder sb, string indentString, int indentLevel );


        /// <summary> String to use for indentation in NbtTag's and NbtFile's ToString() methods by default. </summary>
        /// <exception cref="ArgumentNullException"> <paramref name="value"/> is <c>null</c>. </exception>
        [NotNull]
        public static string DefaultIndentString {
            get {
                return defaultIndentString;
            }
            set {
                if( value == null )
                    throw new ArgumentNullException( "value" );
                defaultIndentString = value;
            }
        }

        static string defaultIndentString;


        static NbtTag() {
            DefaultIndentString = "  ";
        }
    }


    /// <summary> Exception thrown when an operation is attempted on an NbtReader that
    /// cannot recover from a previous parsing error. </summary>
    [Serializable]
    public sealed class InvalidReaderStateException : InvalidOperationException {
        internal InvalidReaderStateException( string message )
            : base( message ) { }
    }


    /// <summary> BinaryReader wrapper that takes care of reading primitives from an NBT stream,
    /// while taking care of endianness, string encoding, and skipping. </summary>
    sealed class NbtBinaryReader : BinaryReader {
        readonly byte[] floatBuffer = new byte[sizeof( float )],
                        doubleBuffer = new byte[sizeof( double )];

        byte[] seekBuffer;
        const int SeekBufferSize = 64 * 1024;
        readonly bool bigEndian;


        public NbtBinaryReader( [NotNull] Stream input, bool bigEndian )
            : base( input ) {
            this.bigEndian = bigEndian;
        }


        public NbtTagType ReadTagType() {
            NbtTagType type = (NbtTagType)ReadByte();
            if( type < NbtTagType.End || type > NbtTagType.IntArray ) {
                throw new NbtFormatException( "NBT tag type out of range: " + (int)type );
            }
            return type;
        }


        public override short ReadInt16() {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                return NbtBinaryWriter.Swap( base.ReadInt16() );
            } else {
                return base.ReadInt16();
            }
        }


        public override int ReadInt32() {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                return NbtBinaryWriter.Swap( base.ReadInt32() );
            } else {
                return base.ReadInt32();
            }
        }


        public override long ReadInt64() {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                return NbtBinaryWriter.Swap( base.ReadInt64() );
            } else {
                return base.ReadInt64();
            }
        }


        public override float ReadSingle() {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                BaseStream.Read( floatBuffer, 0, sizeof( float ) );
                Array.Reverse( floatBuffer );
                return BitConverter.ToSingle( floatBuffer, 0 );
            }
            return base.ReadSingle();
        }


        public override double ReadDouble() {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                BaseStream.Read( doubleBuffer, 0, sizeof( double ) );
                Array.Reverse( doubleBuffer );
                return BitConverter.ToDouble( doubleBuffer, 0 );
            }
            return base.ReadDouble();
        }


        public override string ReadString() {
            short length = ReadInt16();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative string length given!" );
            }
            byte[] stringData = ReadBytes( length );
            return Encoding.UTF8.GetString( stringData );
        }


        public void Skip( int bytesToSkip ) {
            if( bytesToSkip < 0 ) {
                throw new ArgumentOutOfRangeException( "bytesToSkip" );
            } else if( BaseStream.CanSeek ) {
                BaseStream.Position += bytesToSkip;
            } else if( bytesToSkip != 0 ) {
                if( seekBuffer == null )
                    seekBuffer = new byte[SeekBufferSize];
                int bytesDone = 0;
                while( bytesDone < bytesToSkip ) {
                    int readThisTime = BaseStream.Read( seekBuffer, bytesDone, bytesToSkip - bytesDone );
                    if( readThisTime == 0 ) {
                        throw new EndOfStreamException();
                    }
                    bytesDone += readThisTime;
                }
            }
        }


        public void SkipString() {
            short length = ReadInt16();
            if( length < 0 ) {
                throw new NbtFormatException( "Negative string length given!" );
            }
            Skip( length );
        }


        public TagSelector Selector { get; set; }
    }


    /// <summary> BinaryWriter wrapper that takes care of writing primitives to an NBT stream,
    /// while taking care of endianness and string encoding. </summary>
    sealed class NbtBinaryWriter : BinaryWriter {
        readonly bool bigEndian;


        public NbtBinaryWriter( [NotNull] Stream input, bool bigEndian )
            : base( input ) {
            this.bigEndian = bigEndian;
        }


        public void Write( NbtTagType value ) {
            Write( (byte)value );
        }


        public override void Write( short value ) {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                base.Write( Swap( value ) );
            } else {
                base.Write( value );
            }
        }


        public override void Write( int value ) {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                base.Write( Swap( value ) );
            } else {
                base.Write( value );
            }
        }


        public override void Write( long value ) {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                base.Write( Swap( value ) );
            } else {
                base.Write( value );
            }
        }


        public override void Write( float value ) {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                byte[] floatBytes = BitConverter.GetBytes( value );
                Array.Reverse( floatBytes );
                Write( floatBytes );
            } else {
                base.Write( value );
            }
        }


        public override void Write( double value ) {
            if( BitConverter.IsLittleEndian == bigEndian ) {
                byte[] doubleBytes = BitConverter.GetBytes( value );
                Array.Reverse( doubleBytes );
                Write( doubleBytes );
            } else {
                base.Write( value );
            }
        }


        public override void Write( string value ) {
            if( value == null )
                throw new ArgumentNullException( "value" );
            var bytes = Encoding.UTF8.GetBytes( value );
            Write( (short)bytes.Length );
            Write( bytes );
        }



        public static short Swap( short v ) {
            return (short)((v >> 8) & 0x00FF |
                            (v << 8) & 0xFF00);
        }


        public static int Swap( int v ) {
            uint v2 = (uint)v;
            return (int)((v2 >> 24) & 0x000000FF |
                          (v2 >> 8) & 0x0000FF00 |
                          (v2 << 8) & 0x00FF0000 |
                          (v2 << 24) & 0xFF000000);
        }


        public static long Swap( long v ) {
            return (Swap( (int)v ) & uint.MaxValue) << 32 |
                     Swap( (int)(v >> 32) ) & uint.MaxValue;
        }
    }


    /// <summary> Compression method used for loading/saving NBT files. </summary>
    public enum NbtCompression {
        /// <summary> Automatically detect file compression. Not a valid format for saving. </summary>
        AutoDetect,

        /// <summary> No compression. </summary>
        None,

        /// <summary> Compressed, with GZip header (default). </summary>
        GZip,

        /// <summary> Compressed, with ZLib header (RFC-1950). </summary>
        ZLib
    }


    /// <summary> Represents a complete NBT file. </summary>
    public sealed class NbtFile {
        // buffer used to avoid frequent reads from / writes to compressed streams
        const int WriteBufferSize = 8192;

        /// <summary> Gets the file name used for most recent loading/saving of this file.
        /// May be <c>null</c>, if this <c>NbtFile</c> instance has not been loaded from, or saved to, a file. </summary>
        [CanBeNull]
        public string FileName { get; private set; }


        /// <summary> Gets the compression method used for most recent loading/saving of this file.
        /// Defaults to AutoDetect. </summary>
        public NbtCompression FileCompression { get; private set; }


        /// <summary> Root tag of this file. Must be a named CompoundTag. Defaults to <c>null</c>. </summary>
        /// <exception cref="ArgumentException"> If given tag is unnamed. </exception>
        [NotNull]
        public NbtCompound RootTag {
            get {
                return rootTag;
            }
            set {
                if( value == null )
                    throw new ArgumentNullException( "value" );
                if( value.Name == null )
                    throw new ArgumentException( "Root tag must be named." );
                rootTag = value;
            }
        }

        NbtCompound rootTag;


        /// <summary> Whether new NbtFiles should default to big-endian encoding (default: true). </summary>
        public static bool BigEndianByDefault { get; set; }

        /// <summary> Whether this file should read/write tags in big-endian encoding format. </summary>
        public bool BigEndian { get; set; }


        /// <summary> Gets or sets the default value of <c>BufferSize</c> property. Default is 8192. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> value is negative. </exception>
        public static int DefaultBufferSize {
            get {
                return defaultBufferSize;
            }
            set {
                if( value < 0 )
                    throw new ArgumentOutOfRangeException( "value", value, "DefaultBufferSize cannot be negative." );
                defaultBufferSize = value;
            }
        }

        static int defaultBufferSize;


        /// <summary> Gets or sets the size of internal buffer used for reading files and streams.
        /// Initialized to value of <c>DefaultBufferSize</c> property. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> value is negative. </exception>
        public int BufferSize {
            get {
                return bufferSize;
            }
            set {
                if( value < 0 )
                    throw new ArgumentOutOfRangeException( "value", value, "BufferSize cannot be negative." );
                bufferSize = value;
            }
        }

        int bufferSize;


        #region Constructors

        // static constructor
        static NbtFile() {
            BigEndianByDefault = true;
        }


        /// <summary> Creates an empty NbtFile.
        /// RootTag will be set to an empty <c>NbtCompound</c> with a blank name (""). </summary>
        public NbtFile() {
            BigEndian = BigEndianByDefault;
            BufferSize = DefaultBufferSize;
            RootTag = new NbtCompound( "" );
        }


        /// <summary> Creates a new NBT file with the given root tag. </summary>
        /// <param name="rootTag"> Compound tag to set as the root tag. May be <c>null</c>. </param>
        /// <exception cref="ArgumentException"> If given <paramref name="rootTag"/> is unnamed. </exception>
        public NbtFile( [NotNull] NbtCompound rootTag )
            : this() {
            if( rootTag == null )
                throw new ArgumentNullException( "rootTag" );
            RootTag = rootTag;
        }


        /// <summary> Loads NBT data from a file using the most common settings.
        /// Automatically detects compression. Assumes the file to be big-endian, and uses default buffer size. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public NbtFile( [NotNull] string fileName )
            : this() {
            if( fileName == null )
                throw new ArgumentNullException( "fileName" );
            LoadFromFile( fileName, NbtCompression.AutoDetect, null );
        }

        #endregion


        #region Loading

        /// <summary> Loads NBT data from a file. Existing <c>RootTag</c> will be replaced. Compression will be auto-detected. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <returns> Number of bytes read from the file. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public int LoadFromFile( [NotNull] string fileName ) {
            return LoadFromFile( fileName, NbtCompression.AutoDetect, null );
        }


        /// <summary> Loads NBT data from a file. Existing <c>RootTag</c> will be replaced. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <param name="selector"> Optional callback to select which tags to load into memory. Root may not be skipped.
        /// No reference is stored to this callback after loading (don't worry about implicitly captured closures). May be <c>null</c>. </param>
        /// <returns> Number of bytes read from the file. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        public int LoadFromFile( [NotNull] string fileName, NbtCompression compression,
                                 [CanBeNull] TagSelector selector ) {
            if( fileName == null )
                throw new ArgumentNullException( "fileName" );

            using( FileStream readFileStream = File.OpenRead( fileName ) ) {
                LoadFromStream( readFileStream, compression, selector );
                FileName = fileName;
                return (int)readFileStream.Position;
            }
        }


        /// <summary> Loads NBT data from a byte array. Existing <c>RootTag</c> will be replaced. <c>FileName</c> will be set to null. </summary>
        /// <param name="buffer"> Stream from which data will be loaded. If <paramref name="compression"/> is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="index"> The index into <paramref name="buffer"/> at which the stream begins. Must not be negative. </param>
        /// <param name="length"> Maximum number of bytes to read from the given buffer. Must not be negative.
        /// An <see cref="EndOfStreamException"/> is thrown if NBT stream is longer than the given length. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <param name="selector"> Optional callback to select which tags to load into memory. Root may not be skipped.
        /// No reference is stored to this callback after loading (don't worry about implicitly captured closures). May be <c>null</c>. </param>
        /// <returns> Number of bytes read from the buffer. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="buffer"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>;
        /// if <paramref name="index"/> or <paramref name="length"/> is less than zero;
        /// if the sum of <paramref name="index"/> and <paramref name="length"/> is greater than the length of <paramref name="buffer"/>. </exception>
        /// <exception cref="EndOfStreamException"> If NBT stream extends beyond the given <paramref name="length"/>. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public int LoadFromBuffer( [NotNull] byte[] buffer, int index, int length, NbtCompression compression,
                                   [CanBeNull] TagSelector selector ) {
            if( buffer == null )
                throw new ArgumentNullException( "buffer" );

            using( MemoryStream ms = new MemoryStream( buffer, index, length ) ) {
                LoadFromStream( ms, compression, selector );
                FileName = null;
                return (int)ms.Position;
            }
        }


        /// <summary> Loads NBT data from a byte array. Existing <c>RootTag</c> will be replaced. <c>FileName</c> will be set to null. </summary>
        /// <param name="buffer"> Stream from which data will be loaded. If <paramref name="compression"/> is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="index"> The index into <paramref name="buffer"/> at which the stream begins. Must not be negative. </param>
        /// <param name="length"> Maximum number of bytes to read from the given buffer. Must not be negative.
        /// An <see cref="EndOfStreamException"/> is thrown if NBT stream is longer than the given length. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <returns> Number of bytes read from the buffer. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="buffer"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>;
        /// if <paramref name="index"/> or <paramref name="length"/> is less than zero;
        /// if the sum of <paramref name="index"/> and <paramref name="length"/> is greater than the length of <paramref name="buffer"/>. </exception>
        /// <exception cref="EndOfStreamException"> If NBT stream extends beyond the given <paramref name="length"/>. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public int LoadFromBuffer( [NotNull] byte[] buffer, int index, int length, NbtCompression compression ) {
            if( buffer == null )
                throw new ArgumentNullException( "buffer" );

            using( MemoryStream ms = new MemoryStream( buffer, index, length ) ) {
                LoadFromStream( ms, compression, null );
                FileName = null;
                return (int)ms.Position;
            }
        }


        /// <summary> Loads NBT data from a stream. Existing <c>RootTag</c> will be replaced </summary>
        /// <param name="stream"> Stream from which data will be loaded. If compression is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <param name="selector"> Optional callback to select which tags to load into memory. Root may not be skipped.
        /// No reference is stored to this callback after loading (don't worry about implicitly captured closures). May be <c>null</c>. </param>
        /// <returns> Number of bytes read from the stream. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="NotSupportedException"> If <paramref name="compression"/> is set to AutoDetect, but the stream is not seekable. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, decompressing failed, or given stream does not support reading. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public int LoadFromStream( [NotNull] Stream stream, NbtCompression compression,
                                   [CanBeNull] TagSelector selector ) {
            if( stream == null )
                throw new ArgumentNullException( "stream" );

            FileName = null;

            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                FileCompression = DetectCompression( stream );
            } else {
                FileCompression = compression;
            }

            long startPosition = stream.Position;

            switch( FileCompression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            LoadFromStreamInternal( new BufferedStream( decStream, bufferSize ), selector );
                        } else {
                            LoadFromStreamInternal( decStream, selector );
                        }
                    }
                    break;

                case NbtCompression.None:
                    LoadFromStreamInternal( stream, selector );
                    break;

                case NbtCompression.ZLib:
                    if( stream.ReadByte() != 0x78 ) {
                        throw new InvalidDataException( "Incorrect ZLib header. Expected 0x78 0x9C" );
                    }
                    stream.ReadByte();
                    using( var decStream = new DeflateStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            LoadFromStreamInternal( new BufferedStream( decStream, bufferSize ), selector );
                        } else {
                            LoadFromStreamInternal( decStream, selector );
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }

            return (int)(stream.Position - startPosition);
        }


        /// <summary> Loads NBT data from a stream. Existing <c>RootTag</c> will be replaced </summary>
        /// <param name="stream"> Stream from which data will be loaded. If compression is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="compression"> Compression method to use for loading/saving this file. </param>
        /// <returns> Number of bytes read from the stream. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="NotSupportedException"> If <paramref name="compression"/> is set to AutoDetect, but the stream is not seekable. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, decompressing failed, or given stream does not support reading. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public int LoadFromStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null )
                throw new ArgumentNullException( "stream" );

            FileName = null;
            FileCompression = compression;

            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                compression = DetectCompression( stream );
            }

            long startPosition = stream.Position;

            switch( compression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            LoadFromStreamInternal( new BufferedStream( decStream, bufferSize ), null );
                        } else {
                            LoadFromStreamInternal( decStream, null );
                        }
                    }
                    break;

                case NbtCompression.None:
                    LoadFromStreamInternal( stream, null );
                    break;

                case NbtCompression.ZLib:
                    if( stream.ReadByte() != 0x78 ) {
                        throw new InvalidDataException( "Incorrect ZLib header. Expected 0x78 0x9C" );
                    }
                    stream.ReadByte();
                    using( var decStream = new DeflateStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            LoadFromStreamInternal( new BufferedStream( decStream, bufferSize ), null );
                        } else {
                            LoadFromStreamInternal( decStream, null );
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }

            return (int)(stream.Position - startPosition);
        }


        static NbtCompression DetectCompression( Stream stream ) {
            NbtCompression compression;
            if( !stream.CanSeek ) {
                throw new NotSupportedException( "Cannot auto-detect compression on a stream that's not seekable." );
            }
            int firstByte = stream.ReadByte();
            switch( firstByte ) {
                case -1:
                    throw new EndOfStreamException();

                case (byte)NbtTagType.Compound: // 0x0A
                    compression = NbtCompression.None;
                    break;

                case 0x1F:
                    // gzip magic number
                    compression = NbtCompression.GZip;
                    break;

                case 0x78:
                    // zlib header
                    compression = NbtCompression.ZLib;
                    break;

                default:
                    throw new InvalidDataException( "Could not auto-detect compression format." );
            }
            stream.Seek( -1, SeekOrigin.Current );
            return compression;
        }


        void LoadFromStreamInternal( [NotNull] Stream stream, [CanBeNull] TagSelector tagSelector ) {
            if( stream == null )
                throw new ArgumentNullException( "stream" );

            // Make sure the first byte in this file is the tag for a TAG_Compound
            if( stream.ReadByte() != (int)NbtTagType.Compound ) {
                throw new NbtFormatException( "Given NBT stream does not start with a TAG_Compound" );
            }
            NbtBinaryReader reader = new NbtBinaryReader( stream, BigEndian ) {
                Selector = tagSelector
            };

            var rootCompound = new NbtCompound( reader.ReadString() );
            rootCompound.ReadTag( reader );
            RootTag = rootCompound;
        }

        #endregion


        #region Saving

        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is <c>null</c>. </summary>
        /// <param name="fileName"> File to write data to. May not be <c>null</c>. </param>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <returns> Number of bytes written to the file. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the <paramref name="compression"/> mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while creating the file. </exception>
        /// <exception cref="UnauthorizedAccessException"> Specified file is read-only, or a permission issue occurred. </exception>
        /// <exception cref="NbtFormatException"> If one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public int SaveToFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null )
                throw new ArgumentNullException( "fileName" );

            using( FileStream saveFile = File.Create( fileName ) ) {
                return SaveToStream( saveFile, compression );
            }
        }


        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is <c>null</c>. </summary>
        /// <param name="buffer"> Buffer to write data to. May not be <c>null</c>. </param>
        /// <param name="index"> The index into <paramref name="buffer"/> at which the stream should begin. </param>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <returns> Number of bytes written to the buffer. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="buffer"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the <paramref name="compression"/> mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>;
        /// if <paramref name="index"/> is less than zero; or if <paramref name="index"/> is greater than the length of <paramref name="buffer"/>. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="UnauthorizedAccessException"> Specified file is read-only, or a permission issue occurred. </exception>
        /// <exception cref="NbtFormatException"> If one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public int SaveToBuffer( [NotNull] byte[] buffer, int index, NbtCompression compression ) {
            if( buffer == null )
                throw new ArgumentNullException( "buffer" );

            using( MemoryStream ms = new MemoryStream( buffer, index, buffer.Length - index ) ) {
                SaveToStream( ms, compression );
                return (int)ms.Position;
            }
        }


        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is <c>null</c>. </summary>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <returns> Byte array containing the serialized NBT data. </returns>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the <paramref name="compression"/> mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="UnauthorizedAccessException"> Specified file is read-only, or a permission issue occurred. </exception>
        /// <exception cref="NbtFormatException"> If one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public byte[] SaveToBuffer( NbtCompression compression ) {
            using( MemoryStream ms = new MemoryStream() ) {
                SaveToStream( ms, compression );
                return ms.ToArray();
            }
        }


        /// <summary> Saves this NBT file to a stream. Nothing is written to stream if RootTag is <c>null</c>. </summary>
        /// <param name="stream"> Stream to write data to. May not be <c>null</c>. </param>
        /// <param name="compression"> Compression mode to use for saving. May not be AutoDetect. </param>
        /// <returns> Number of bytes written to the stream. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentException"> If AutoDetect was given as the <paramref name="compression"/> mode. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="InvalidDataException"> If given stream does not support writing. </exception>
        /// <exception cref="NbtFormatException"> If RootTag is null;
        /// or if RootTag is unnamed;
        /// or if one of the NbtCompound tags contained unnamed tags;
        /// or if an NbtList tag had Unknown list type and no elements. </exception>
        public int SaveToStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null )
                throw new ArgumentNullException( "stream" );

            switch( compression ) {
                case NbtCompression.AutoDetect:
                    throw new ArgumentException( "AutoDetect is not a valid NbtCompression value for saving." );
                case NbtCompression.ZLib:
                case NbtCompression.GZip:
                case NbtCompression.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }

            if( rootTag == null ) {
                throw new NbtFormatException( "Cannot save NbtFile: No root tag." );
            }

            if( rootTag.Name == null ) {
                throw new NbtFormatException(
                    "Cannot save NbtFile: Root tag is not named. Its name may be an empty string, but not null." );
            }

            long startPosition = stream.Position;

            switch( compression ) {
                case NbtCompression.ZLib:
                    stream.WriteByte( 0x78 );
                    stream.WriteByte( 0x01 );
                    int checksum;
                    using( var compressStream = new ZLibStream( stream, CompressionMode.Compress, true ) ) {
                        BufferedStream bufferedStream = new BufferedStream( compressStream, WriteBufferSize );
                        RootTag.WriteTag( new NbtBinaryWriter( bufferedStream, BigEndian ), true );
                        bufferedStream.Flush();
                        checksum = compressStream.Checksum;
                    }
                    byte[] checksumBytes = BitConverter.GetBytes( checksum );
                    if( BitConverter.IsLittleEndian ) {
                        // Adler32 checksum is big-endian
                        Array.Reverse( checksumBytes );
                    }
                    stream.Write( checksumBytes, 0, checksumBytes.Length );
                    break;

                case NbtCompression.GZip:
                    using( var compressStream = new GZipStream( stream, CompressionMode.Compress, true ) ) {
                        // use a buffered stream to avoid gzipping in small increments (which has a lot of overhead)
                        BufferedStream bufferedStream = new BufferedStream( compressStream, WriteBufferSize );
                        RootTag.WriteTag( new NbtBinaryWriter( bufferedStream, BigEndian ), true );
                        bufferedStream.Flush();
                    }
                    break;

                case NbtCompression.None:
                    RootTag.WriteTag( new NbtBinaryWriter( stream, BigEndian ), true );
                    break;
            }

            return (int)(stream.Position - startPosition);
        }

        #endregion


        /// <summary> Reads the root name from the given NBT file. Automatically detects compression. </summary>
        /// <param name="fileName"> Name of the file from which first tag will be read. </param>
        /// <returns> Name of the root tag in the given NBT file. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        [NotNull]
        public static string ReadRootTagName( [NotNull] string fileName ) {
            return ReadRootTagName( fileName, NbtCompression.AutoDetect, BigEndianByDefault, defaultBufferSize );
        }


        /// <summary> Reads the root name from the given NBT file. </summary>
        /// <param name="fileName"> Name of the file from which data will be loaded. </param>
        /// <param name="compression"> Format in which the given file is compressed. </param>
        /// <param name="bigEndian"> Whether the file uses big-endian (default) or little-endian encoding. </param>
        /// <param name="bufferSize"> Buffer size to use for reading, in bytes. Default is 8192. </param>
        /// <returns> Name of the root tag in the given NBT file. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="fileName"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="FileNotFoundException"> If given file was not found. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, or decompressing failed. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="IOException"> If an I/O error occurred while reading the file. </exception>
        [NotNull]
        public static string ReadRootTagName( [NotNull] string fileName, NbtCompression compression, bool bigEndian,
                                              int bufferSize ) {
            if( fileName == null ) {
                throw new ArgumentNullException( "fileName" );
            }
            if( !File.Exists( fileName ) ) {
                throw new FileNotFoundException( "Could not find the given NBT file.",
                                                 fileName );
            }
            if( bufferSize < 0 ) {
                throw new ArgumentOutOfRangeException( "bufferSize", bufferSize, "DefaultBufferSize cannot be negative." );
            }
            using( FileStream readFileStream = File.OpenRead( fileName ) ) {
                return ReadRootTagName( readFileStream, compression, bigEndian, bufferSize );
            }
        }


        /// <summary> Reads the root name from the given stream of NBT data. </summary>
        /// <param name="stream"> Stream from which data will be loaded. If compression is set to AutoDetect, this stream must support seeking. </param>
        /// <param name="compression"> Compression method to use for loading this stream. </param>
        /// <param name="bigEndian"> Whether the stream uses big-endian (default) or little-endian encoding. </param>
        /// <param name="bufferSize"> Buffer size to use for reading, in bytes. Default is 8192. </param>
        /// <returns> Name of the root tag in the given stream. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c>. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If an unrecognized/unsupported value was given for <paramref name="compression"/>. </exception>
        /// <exception cref="NotSupportedException"> If compression is set to AutoDetect, but the stream is not seekable. </exception>
        /// <exception cref="EndOfStreamException"> If file ended earlier than expected. </exception>
        /// <exception cref="InvalidDataException"> If file compression could not be detected, decompressing failed, or given stream does not support reading. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        [NotNull]
        public static string ReadRootTagName( [NotNull] Stream stream, NbtCompression compression, bool bigEndian,
                                              int bufferSize ) {
            if( stream == null ) {
                throw new ArgumentNullException( "stream" );
            }
            if( bufferSize < 0 ) {
                throw new ArgumentOutOfRangeException( "bufferSize", bufferSize, "DefaultBufferSize cannot be negative." );
            }
            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                compression = DetectCompression( stream );
            }

            switch( compression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            return GetRootNameInternal( new BufferedStream( decStream, bufferSize ), bigEndian );
                        } else {
                            return GetRootNameInternal( decStream, bigEndian );
                        }
                    }

                case NbtCompression.None:
                    return GetRootNameInternal( stream, bigEndian );

                case NbtCompression.ZLib:
                    if( stream.ReadByte() != 0x78 ) {
                        throw new InvalidDataException( "Incorrect ZLib header. Expected 0x78 0x9C" );
                    }
                    stream.ReadByte();
                    using( var decStream = new DeflateStream( stream, CompressionMode.Decompress, true ) ) {
                        if( bufferSize > 0 ) {
                            return GetRootNameInternal( new BufferedStream( decStream, bufferSize ), bigEndian );
                        } else {
                            return GetRootNameInternal( decStream, bigEndian );
                        }
                    }

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }
        }


        [NotNull]
        static string GetRootNameInternal( [NotNull] Stream stream, bool bigEndian ) {
            if( stream == null )
                throw new ArgumentNullException( "stream" );
            NbtBinaryReader reader = new NbtBinaryReader( stream, bigEndian );

            if( reader.ReadTagType() != NbtTagType.Compound ) {
                throw new NbtFormatException( "Given NBT stream does not start with a TAG_Compound" );
            }

            return reader.ReadString();
        }


        /// <summary> Prints contents of the root tag, and any child tags, to a string. </summary>
        public override string ToString() {
            return RootTag.ToString( NbtTag.DefaultIndentString );
        }


        /// <summary> Prints contents of the root tag, and any child tags, to a string.
        /// Indents the string using multiples of the given indentation string. </summary>
        /// <param name="indentString"> String to be used for indentation. </param>
        /// <returns> A string representing contants of this tag, and all child tags (if any). </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="indentString"/> is <c>null</c>. </exception>
        public string ToString( [NotNull] string indentString ) {
            return RootTag.ToString( indentString );
        }
    }


    /// <summary> Exception thrown when a format violation is detected while
    /// parsing or serializing an NBT file. </summary>
    [Serializable]
    public sealed class NbtFormatException : Exception {
        internal NbtFormatException( string message )
            : base( message ) { }
    }


    enum NbtParseState {
        AtStreamBeginning,
        AtCompoundBeginning,
        InCompound,
        AtCompoundEnd,
        AtListBeginning,
        InList,
        AtStreamEnd,
        Error
    }


    /// <summary> Represents a reader that provides fast, noncached, forward-only access to NBT data. </summary>
    public class NbtReader {
        NbtParseState state = NbtParseState.AtStreamBeginning;
        readonly NbtBinaryReader reader;
        readonly Stack<NbtReaderNode> nodes = new Stack<NbtReaderNode>();
        readonly long streamStartOffset;
        bool atValue;
        object valueCache;


        /// <summary> Initializes a new instance of the NbtReader class. </summary>
        /// <param name="stream"> Stream to read from. </param>
        /// <remarks> Assumes that data in the stream is Big-Endian encoded. </remarks>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c> </exception>
        public NbtReader( [NotNull] Stream stream )
            : this( stream, true ) { }


        /// <summary> Initializes a new instance of the NbtReader class. </summary>
        /// <param name="stream"> Stream to read from. </param>
        /// <param name="bigEndian"> Whether NBT data is in Big-Endian encoding. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="stream"/> is <c>null</c> </exception>
        public NbtReader( [NotNull] Stream stream, bool bigEndian ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            SkipEndTags = true;
            CacheTagValues = false;
            ParentTagType = NbtTagType.Unknown;
            TagType = NbtTagType.Unknown;
            streamStartOffset = stream.Position;
            reader = new NbtBinaryReader( stream, bigEndian );
        }


        /// <summary> Gets the name of the root tag of this NBT stream. </summary>
        [CanBeNull]
        public string RootName { get; private set; }

        /// <summary> Gets the name of the parent tag. May be null (for root tags and descendants of list elements). </summary>
        [CanBeNull]
        public string ParentName { get; private set; }

        /// <summary> Gets the name of the current tag. May be null (for list elements and end tags). </summary>
        [CanBeNull]
        public string TagName { get; private set; }


        /// <summary> Gets the type of the parent tag. Returns TagType.Unknown if there is no parent tag. </summary>
        public NbtTagType ParentTagType { get; private set; }

        /// <summary> Gets the type of the current tag. </summary>
        public NbtTagType TagType { get; private set; }


        /// <summary> Whether tag that we are currently on is a list element. </summary>
        public bool IsListElement {
            get {
                return (ParentTagType == NbtTagType.List);
            }
        }

        /// <summary> Whether current tag has a value to read. </summary>
        public bool HasValue {
            get {
                switch( TagType ) {
                    case NbtTagType.Compound:
                    case NbtTagType.End:
                    case NbtTagType.List:
                    case NbtTagType.Unknown:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary> Whether current tag has a name. </summary>
        public bool HasName {
            get {
                return (TagName != null);
            }
        }


        /// <summary> Whether the current tag is a Compound. </summary>
        public bool IsCompound {
            get {
                return (TagType == NbtTagType.Compound);
            }
        }

        /// <summary> Whether the current tag is a List. </summary>
        public bool IsList {
            get {
                return (TagType == NbtTagType.List);
            }
        }

        /// <summary> Whether the current tag has length (Lists, ByteArrays, and IntArrays have length).
        /// Compound tags also have length, technically, but it is not known until all child tags are read. </summary>
        public bool HasLength {
            get {
                switch( TagType ) {
                    case NbtTagType.List:
                    case NbtTagType.ByteArray:
                    case NbtTagType.IntArray:
                        return true;
                    default:
                        return false;
                }
            }
        }


        /// <summary> Gets the Stream from which data is being read. </summary>
        [NotNull]
        public Stream BaseStream {
            get {
                return reader.BaseStream;
            }
        }

        /// <summary> Gets the number of bytes from the beginning of the stream to the beginning of this tag. </summary>
        public int TagStartOffset { get; private set; }

        /// <summary> Gets the number of tags read from the stream so far
        /// (including the current tag and all skipped tags). 
        /// If <c>SkipEndTags</c> is <c>false</c>, all end tags are also counted. </summary>
        public int TagsRead { get; private set; }

        /// <summary> Gets the depth of the current tag in the hierarchy.
        /// <c>RootTag</c> is at depth 0, its descendant tags are 1, etc. </summary>
        public int Depth { get; private set; }


        /// <summary> If the current tag is TAG_List, returns type of the list elements. </summary>
        public NbtTagType ListType { get; private set; }

        /// <summary> If the current tag is TAG_List, TAG_Byte_Array, or TAG_Int_Array, returns the number of elements. </summary>
        public int TagLength { get; private set; }

        /// <summary> If the parent tag is TAG_List, returns the number of elements. </summary>
        public int ParentTagLength { get; private set; }

        /// <summary> If the parent tag is TAG_List, returns index of the current tag. </summary>
        public int ListIndex { get; private set; }


        /// <summary> Gets whether this NbtReader instance is in state of error.
        /// No further reading can be done from this instance if a parse error occured. </summary>
        public bool IsInErrorState {
            get { return (state == NbtParseState.Error); }
        }


        /// <summary> Reads the next tag from the stream. </summary>
        /// <returns> true if the next tag was read successfully; false if there are no more tags to read. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public bool ReadToFollowing() {
            switch( state ) {
                case NbtParseState.AtStreamBeginning:
                    // read first tag, make sure it's a compound
                    if( reader.ReadTagType() != NbtTagType.Compound ) {
                        state = NbtParseState.Error;
                        throw new NbtFormatException( "Given NBT stream does not start with a TAG_Compound" );
                    }
                    Depth = 1;
                    TagType = NbtTagType.Compound;
                    // Read root name. Advance to the first inside tag.
                    ReadTagHeader( true );
                    RootName = TagName;
                    return true;

                case NbtParseState.AtCompoundBeginning:
                    GoDown();
                    state = NbtParseState.InCompound;
                    goto case NbtParseState.InCompound;

                case NbtParseState.InCompound:
                    if( atValue ) {
                        SkipValue();
                    }
                    // Read next tag, check if we've hit the end
                    TagStartOffset = (int)(reader.BaseStream.Position - streamStartOffset);
                    TagType = reader.ReadTagType();
                    if( TagType == NbtTagType.End ) {
                        TagName = null;
                        TagsRead++;
                        state = NbtParseState.AtCompoundEnd;
                        if( SkipEndTags ) {
                            TagsRead--;
                            goto case NbtParseState.AtCompoundEnd;
                        } else {
                            return true;
                        }
                    } else {
                        ReadTagHeader( true );
                        return true;
                    }

                case NbtParseState.AtListBeginning:
                    GoDown();
                    ListIndex = -1;
                    TagType = ListType;
                    state = NbtParseState.InList;
                    goto case NbtParseState.InList;

                case NbtParseState.InList:
                    if( atValue ) {
                        SkipValue();
                    }
                    ListIndex++;
                    if( ListIndex >= ParentTagLength ) {
                        GoUp();
                        if( ParentTagType == NbtTagType.List ) {
                            state = NbtParseState.InList;
                            TagType = NbtTagType.List;
                            goto case NbtParseState.InList;
                        } else if( ParentTagType == NbtTagType.Compound ) {
                            state = NbtParseState.InCompound;
                            goto case NbtParseState.InCompound;
                        } else {
                            // This should not happen unless NbtReader is bugged
                            state = NbtParseState.Error;
                            throw new NbtFormatException( InvalidParentTagError );
                        }
                    } else {
                        TagStartOffset = (int)(reader.BaseStream.Position - streamStartOffset);
                        ReadTagHeader( false );
                    }
                    return true;

                case NbtParseState.AtCompoundEnd:
                    GoUp();
                    if( ParentTagType == NbtTagType.List ) {
                        state = NbtParseState.InList;
                        TagType = NbtTagType.Compound;
                        goto case NbtParseState.InList;
                    } else if( ParentTagType == NbtTagType.Compound ) {
                        state = NbtParseState.InCompound;
                        goto case NbtParseState.InCompound;
                    } else if( ParentTagType == NbtTagType.Unknown ) {
                        state = NbtParseState.AtStreamEnd;
                        return false;
                    } else {
                        // This should not happen unless NbtReader is bugged
                        state = NbtParseState.Error;
                        throw new NbtFormatException( InvalidParentTagError );
                    }

                case NbtParseState.AtStreamEnd:
                    // nothing left to read!
                    return false;

                case NbtParseState.Error:
                    // previous call produced a parsing error
                    throw new InvalidReaderStateException( ErroneousStateError );
            }
            return true;
        }


        void ReadTagHeader( bool readName ) {
            TagsRead++;
            TagName = (readName ? reader.ReadString() : null);

            valueCache = null;
            TagLength = 0;
            atValue = false;
            ListType = NbtTagType.Unknown;

            switch( TagType ) {
                case NbtTagType.Byte:
                case NbtTagType.Short:
                case NbtTagType.Int:
                case NbtTagType.Long:
                case NbtTagType.Float:
                case NbtTagType.Double:
                case NbtTagType.String:
                    atValue = true;
                    break;

                case NbtTagType.IntArray:
                case NbtTagType.ByteArray:
                    TagLength = reader.ReadInt32();
                    atValue = true;
                    break;

                case NbtTagType.List:
                    ListType = reader.ReadTagType();
                    TagLength = reader.ReadInt32();
                    state = NbtParseState.AtListBeginning;
                    break;

                case NbtTagType.Compound:
                    state = NbtParseState.AtCompoundBeginning;
                    break;

                default:
                    throw new NbtFormatException( "Trying to read tag of unknown type." );
            }
        }


        // Goes one step down the NBT file's hierarchy, preserving current state
        void GoDown() {
            NbtReaderNode newNode = new NbtReaderNode {
                ListIndex = ListIndex,
                ParentTagLength = ParentTagLength,
                ParentName = ParentName,
                ParentTagType = ParentTagType,
                ListType = ListType
            };
            nodes.Push( newNode );

            ParentName = TagName;
            ParentTagType = TagType;
            ParentTagLength = TagLength;
            ListIndex = 0;
            TagLength = 0;

            Depth++;
        }


        // Goes one step up the NBT file's hierarchy, restoring previous state
        void GoUp() {
            NbtReaderNode oldNode = nodes.Pop();

            ParentName = oldNode.ParentName;
            ParentTagType = oldNode.ParentTagType;
            ParentTagLength = oldNode.ParentTagLength;
            ListIndex = oldNode.ListIndex;
            ListType = oldNode.ListType;
            TagLength = 0;

            Depth--;
        }


        void SkipValue() {
            if( !atValue ) {
                throw new NbtFormatException( NoValueToReadError );
            }
            switch( TagType ) {
                case NbtTagType.Byte:
                    reader.ReadByte();
                    break;

                case NbtTagType.Short:
                    reader.ReadInt16();
                    break;

                case NbtTagType.Float:
                case NbtTagType.Int:
                    reader.ReadInt32();
                    break;

                case NbtTagType.Double:
                case NbtTagType.Long:
                    reader.ReadInt64();
                    break;

                case NbtTagType.ByteArray:
                    reader.Skip( TagLength );
                    break;

                case NbtTagType.IntArray:
                    reader.Skip( sizeof( int ) * TagLength );
                    break;

                case NbtTagType.String:
                    reader.SkipString();
                    break;

                default:
                    throw new InvalidOperationException( NonValueTagError );
            }
            atValue = false;
            valueCache = null;
        }


        /// <summary> Reads until a tag with the specified name is found. 
        /// Returns false if are no more tags to read (end of stream is reached). </summary>
        /// <param name="tagName"> Name of the tag. May be null (to look for next unnamed tag). </param>
        /// <returns> <c>true</c> if a matching tag is found; otherwise <c>false</c>. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidOperationException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public bool ReadToFollowing( [CanBeNull] string tagName ) {
            while( ReadToFollowing() ) {
                if( TagName == tagName ) {
                    return true;
                }
            }
            return false;
        }


        /// <summary> Advances the NbtReader to the next descendant tag with the specified name.
        /// If a matching child tag is not found, the NbtReader is positioned on the end tag. </summary>
        /// <param name="tagName"> Name of the tag you wish to move to. May be null (to look for next unnamed tag). </param>
        /// <returns> <c>true</c> if a matching descendant tag is found; otherwise <c>false</c>. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public bool ReadToDescendant( [CanBeNull] string tagName ) {
            if( state == NbtParseState.Error ) {
                throw new InvalidReaderStateException( ErroneousStateError );
            } else if( state == NbtParseState.AtStreamEnd ) {
                return false;
            }
            int currentDepth = Depth;
            while( ReadToFollowing() ) {
                if( Depth <= currentDepth ) {
                    return false;
                } else if( TagName == tagName ) {
                    return true;
                }
            }
            return false;
        }


        /// <summary> Advances the NbtReader to the next sibling tag, skipping any child tags.
        /// If there are no more siblings, NbtReader is positioned on the tag following the last of this tag's descendants. </summary>
        /// <returns> <c>true</c> if a sibling element is found; otherwise <c>false</c>. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public bool ReadToNextSibling() {
            if( state == NbtParseState.Error ) {
                throw new InvalidReaderStateException( ErroneousStateError );
            } else if( state == NbtParseState.AtStreamEnd ) {
                return false;
            }
            int currentDepth = Depth;
            while( ReadToFollowing() ) {
                if( Depth == currentDepth ) {
                    return true;
                } else if( Depth < currentDepth ) {
                    return false;
                }
            }
            return false;
        }


        /// <summary> Advances the NbtReader to the next sibling tag with the specified name.
        /// If a matching sibling tag is not found, NbtReader is positioned on the tag following the last siblings. </summary>
        /// <param name="tagName"> The name of the sibling tag you wish to move to. </param>
        /// <returns> <c>true</c> if a matching sibling element is found; otherwise <c>false</c>. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidOperationException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public bool ReadToNextSibling( [CanBeNull] string tagName ) {
            while( ReadToNextSibling() ) {
                if( TagName == tagName ) {
                    return true;
                }
            }
            return false;
        }


        /// <summary> Skips current tag, its value/descendants, and any following siblings.
        /// In other words, reads until parent tag's subling. </summary>
        /// <returns> Total number of tags that were skipped. Returns 0 if end of the stream is reached. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public int Skip() {
            if( state == NbtParseState.Error ) {
                throw new InvalidReaderStateException( ErroneousStateError );
            } else if( state == NbtParseState.AtStreamEnd ) {
                return 0;
            }
            int startDepth = Depth;
            int skipped = 0;
            while( ReadToFollowing() && Depth >= startDepth ) {
                skipped++;
            }
            return skipped;
        }


        /// <summary> Reads the entirety of the current tag, including any descendants,
        /// and constructs an NbtTag object of the appropriate type. </summary>
        /// <returns> Constructed NbtTag object;
        /// <c>null</c> if <c>SkipEndTags</c> is <c>true</c> and trying to read an End tag. </returns>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        /// <exception cref="EndOfStreamException"> End of stream has been reached (no more tags can be read). </exception>
        /// <exception cref="InvalidOperationException"> Tag value has already been read, and CacheTagValues is false. </exception>
        public NbtTag ReadAsTag() {
            switch( state ) {
                case NbtParseState.Error:
                    throw new InvalidReaderStateException( ErroneousStateError );

                case NbtParseState.AtStreamEnd:
                    throw new EndOfStreamException();

                case NbtParseState.AtStreamBeginning:
                case NbtParseState.AtCompoundEnd:
                    ReadToFollowing();
                    break;
            }

            NbtTag parent;
            if( TagType == NbtTagType.Compound ) {
                parent = new NbtCompound( TagName );
            } else if( TagType == NbtTagType.List ) {
                parent = new NbtList( TagName, ListType );
            } else if( atValue ) {
                NbtTag result = ReadValueAsTag();
                ReadToFollowing();
                return result;
            } else {
                throw new InvalidOperationException( NoValueToReadError );
            }

            int startingDepth = Depth;
            int lastDepth = Depth;

            do {
                bool end = !ReadToFollowing();
                if( end || Depth < lastDepth ) {
                    // Going up the file tree, or end of document: wrap up
                    while( Depth <= lastDepth && parent.Parent != null ) {
                        parent = parent.Parent;
                        lastDepth--;
                    }
                }
                if( end || Depth <= startingDepth ) break;

                NbtTag thisTag;
                if( TagType == NbtTagType.Compound ) {
                    thisTag = new NbtCompound( TagName );
                    AddToParent( thisTag, parent );
                    parent = thisTag;
                } else if( TagType == NbtTagType.List ) {
                    thisTag = new NbtList( TagName, ListType );
                    AddToParent( thisTag, parent );
                    parent = thisTag;
                } else {
                    thisTag = ReadValueAsTag();
                    AddToParent( thisTag, parent );
                }
                lastDepth = Depth;
            } while( true );

            return parent;
        }


        static void AddToParent( NbtTag thisTag, NbtTag parent ) {
            NbtList parentAsList = parent as NbtList;
            if( parentAsList != null ) {
                parentAsList.Add( thisTag );
            } else {
                NbtCompound parentAsCompound = parent as NbtCompound;
                if( parentAsCompound != null ) {
                    parentAsCompound.Add( thisTag );
                } else {
                    // cannot happen unless NbtReader is bugged
                    throw new NbtFormatException( InvalidParentTagError );
                }
            }
        }


        [NotNull]
        NbtTag ReadValueAsTag() {
            if( !atValue ) {
                throw new InvalidOperationException( NoValueToReadError );
            }
            atValue = false;
            switch( TagType ) {
                case NbtTagType.Byte:
                    return new NbtByte( TagName, reader.ReadByte() );

                case NbtTagType.Short:
                    return new NbtShort( TagName, reader.ReadInt16() );

                case NbtTagType.Int:
                    return new NbtInt( TagName, reader.ReadInt32() );

                case NbtTagType.Long:
                    return new NbtLong( TagName, reader.ReadInt64() );

                case NbtTagType.Float:
                    return new NbtFloat( TagName, reader.ReadSingle() );

                case NbtTagType.Double:
                    return new NbtDouble( TagName, reader.ReadDouble() );

                case NbtTagType.String:
                    return new NbtString( TagName, reader.ReadString() );

                case NbtTagType.ByteArray:
                    return new NbtByteArray( TagName, reader.ReadBytes( TagLength ) );

                case NbtTagType.IntArray:
                    int[] ints = new int[TagLength];
                    for( int i = 0; i < TagLength; i++ ) {
                        ints[i] = reader.ReadInt32();
                    }
                    return new NbtIntArray( TagName, ints );

                default:
                    throw new InvalidOperationException( NonValueTagError );
            }
        }


        /// <summary> Reads the value as an object of the type specified. </summary>
        /// <typeparam name="T"> The type of the value to be returned.
        /// Tag value should be convertible to this type. </typeparam>
        /// <returns> Tag value converted to the requested type. </returns>
        /// <exception cref="EndOfStreamException"> End of stream has been reached (no more tags can be read). </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidOperationException"> Value has already been read, or there is no value to read. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        /// <exception cref="InvalidCastException"> Tag value cannot be converted to the requested type. </exception>
        public T ReadValueAs<T>() {
            return (T)ReadValue();
        }


        /// <summary> Reads the value as an object of the correct type, boxed.
        /// Cannot be called for tags that do not have a single-object value (compound, list, and end tags). </summary>
        /// <returns> Tag value converted to the requested type. </returns>
        /// <exception cref="EndOfStreamException"> End of stream has been reached (no more tags can be read). </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        /// <exception cref="InvalidOperationException"> Value has already been read, or there is no value to read. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        public object ReadValue() {
            if( state == NbtParseState.AtStreamEnd ) {
                throw new EndOfStreamException();
            }
            if( !atValue ) {
                if( cacheTagValues ) {
                    if( valueCache == null ) {
                        throw new InvalidOperationException( "No value to read." );
                    } else {
                        return valueCache;
                    }
                } else {
                    throw new InvalidOperationException( NoValueToReadError );
                }
            }
            valueCache = null;
            atValue = false;
            object value;
            switch( TagType ) {
                case NbtTagType.Byte:
                    value = reader.ReadByte();
                    break;

                case NbtTagType.Short:
                    value = reader.ReadInt16();
                    break;

                case NbtTagType.Float:
                    value = reader.ReadSingle();
                    break;

                case NbtTagType.Int:
                    value = reader.ReadInt32();
                    break;

                case NbtTagType.Double:
                    value = reader.ReadDouble();
                    break;

                case NbtTagType.Long:
                    value = reader.ReadInt64();
                    break;

                case NbtTagType.ByteArray:
                    value = reader.ReadBytes( TagLength );
                    break;

                case NbtTagType.IntArray:
                    int[] intValue = new int[TagLength];
                    for( int i = 0; i < TagLength; i++ ) {
                        intValue[i] = reader.ReadInt32();
                    }
                    value = intValue;
                    break;

                case NbtTagType.String:
                    value = reader.ReadString();
                    break;

                default:
                    throw new InvalidOperationException( NonValueTagError );
            }
            if( cacheTagValues ) {
                valueCache = value;
            } else {
                valueCache = null;
            }
            return value;
        }


        /// <summary> If the current tag is a List, reads all elements of this list as an array.
        /// If any tags/values have already been read from this list, only reads the remaining unread tags/values.
        /// ListType must be a value type (byte, short, int, long, float, double, or string).
        /// Stops reading after the last list element. </summary>
        /// <typeparam name="T"> Element type of the array to be returned.
        /// Tag contents should be convertible to this type. </typeparam>
        /// <returns> List contents converted to an array of the requested type. </returns>
        /// <exception cref="EndOfStreamException"> End of stream has been reached (no more tags can be read). </exception>
        /// <exception cref="InvalidOperationException"> Current tag is not of type List. </exception>
        /// <exception cref="InvalidReaderStateException"> If NbtReader cannot recover from a previous parsing error. </exception>
        /// <exception cref="NbtFormatException"> If an error occured while parsing data in NBT format. </exception>
        public T[] ReadListAsArray<T>() {
            if( state == NbtParseState.AtStreamEnd ) {
                throw new EndOfStreamException();
            } else if( state == NbtParseState.Error ) {
                throw new InvalidReaderStateException( ErroneousStateError );
            }
            if( TagType != NbtTagType.List ) {
                throw new InvalidOperationException( "ReadListAsArray may only be used on List tags." );
            }

            int elementsToRead = TagLength - ListIndex;

            if( ListType == NbtTagType.Byte && typeof( T ) == typeof( byte ) ) {
                TagsRead += TagLength;
                ListIndex = TagLength - 1;
                return (T[])(object)reader.ReadBytes( elementsToRead );
            }

            T[] result = new T[elementsToRead];
            switch( ListType ) {
                case NbtTagType.Byte:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadByte(), typeof( T ) );
                    }
                    break;

                case NbtTagType.Short:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadInt16(), typeof( T ) );
                    }
                    break;

                case NbtTagType.Int:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadInt32(), typeof( T ) );
                    }
                    break;

                case NbtTagType.Long:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadInt64(), typeof( T ) );
                    }
                    break;

                case NbtTagType.Float:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadSingle(), typeof( T ) );
                    }
                    break;

                case NbtTagType.Double:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadDouble(), typeof( T ) );
                    }
                    break;

                case NbtTagType.String:
                    for( int i = 0; i < elementsToRead; i++ ) {
                        result[i] = (T)Convert.ChangeType( reader.ReadString(), typeof( T ) );
                    }
                    break;

                default:
                    throw new InvalidOperationException( "ReadListAsArray may only be used on lists of value types." );
            }
            TagsRead += elementsToRead;
            ListIndex = TagLength - 1;
            return result;
        }


        /// <summary> Parsing option: Whether NbtReader should skip End tags in ReadToFollowing() automatically while parsing.
        /// Default is <c>false</c>. </summary>
        public bool SkipEndTags { get; set; }


        /// <summary> Parsing option: Whether NbtReader should save a copy of the most recently read tag's value.
        /// Unless CacheTagValues is <c>true</c>, tag values can only be read once. Default is <c>false</c>. </summary>
        public bool CacheTagValues {
            get {
                return cacheTagValues;
            }
            set {
                cacheTagValues = value;
                if( !cacheTagValues ) {
                    valueCache = null;
                }
            }
        }

        bool cacheTagValues;


        public override string ToString() {
            return ToString( false );
        }


        /// <summary> Returns a String that represents the current NbtReader object.
        /// Prints current tag's depth, ordinal number, type, name, size (for arrays and lists), and optionally value. </summary>
        /// <param name="includeValue"> If set to <c>true</c>, also reads and prints the current tag's value. </param>
        /// <returns></returns>
        public string ToString( bool includeValue ) {
            StringBuilder sb = new StringBuilder();
            sb.Append( '\t', Depth )
              .Append( '#' )
              .Append( TagsRead )
              .Append( ". " )
              .Append( TagType );
            if( IsList ) {
                sb.Append( '<' )
                  .Append( ListType )
                  .Append( '>' );
            }
            if( HasLength ) {
                sb.Append( '[' )
                  .Append( TagLength )
                  .Append( ']' );
            }
            sb.Append( '\t' )
              .Append( TagName );
            if( includeValue &&
                (atValue || HasValue && cacheTagValues) &&
                TagType != NbtTagType.IntArray &&
                TagType != NbtTagType.ByteArray ) {
                sb.Append( " = " )
                  .Append( ReadValue() );
            }
            return sb.ToString();
        }


        const string NoValueToReadError = "Value aready read, or no value to read.",
                     NonValueTagError = "Trying to read value of a non-value tag.",
                     InvalidParentTagError = "Parent tag is neither a Compound nor a List.",
                     ErroneousStateError = "NbtReader is in an erroneous state!";
    }


    // Represents state of a node in the NBT file tree, used by NbtReader
    class NbtReaderNode {
        public string ParentName;
        public NbtTagType ParentTagType;
        public NbtTagType ListType;
        public int ParentTagLength;
        public int ListIndex;
    }


    /// <summary> Enumeration of named binary tag types, and their corresponding codes. </summary>
    public enum NbtTagType {
        /// <summary> Placeholder TagType used to indicate unknown/undefined tag type in NbtList. </summary>
        Unknown = 0xff,

        /// <summary> TAG_End: This unnamed tag serves no purpose but to signify the end of an open TAG_Compound. </summary>
        End = 0x00,

        /// <summary> TAG_Byte: A single byte. </summary>
        Byte = 0x01,

        /// <summary> TAG_Short: A single signed 16-bit integer. </summary>
        Short = 0x02,

        /// <summary> TAG_Int: A single signed 32-bit integer. </summary>
        Int = 0x03,

        /// <summary> TAG_Long: A single signed 64-bit integer. </summary>
        Long = 0x04,

        /// <summary> TAG_Float: A single IEEE-754 single-precision floating point number. </summary>
        Float = 0x05,

        /// <summary> TAG_Double: A single IEEE-754 double-precision floating point number. </summary>
        Double = 0x06,

        /// <summary> TAG_Byte_Array: A length-prefixed array of bytes. </summary>
        ByteArray = 0x07,

        /// <summary> TAG_String: A length-prefixed UTF-8 string. </summary>
        String = 0x08,

        /// <summary> TAG_List: A list of nameless tags, all of the same type. </summary>
        List = 0x09,

        /// <summary> TAG_Compound: A set of named tags. </summary>
        Compound = 0x0a,

        /// <summary> TAG_Byte_Array: A length-prefixed array of signed 32-bit integers. </summary>
        IntArray = 0x0b
    }


    /// <summary> Delegate used to skip loading certain tags of an NBT stream/file. 
    /// The callback should return "true" for any tag that should be read,and "false" for any tag that should be skipped. </summary>
    /// <param name="tag"> Tag that is being read. Tag's type and name are available,
    /// but the value has not yet been read at this time. Guaranteed to never be <c>null</c>. </param>
    public delegate bool TagSelector( [NotNull] NbtTag tag );


    /// <summary> DeflateStream wrapper that calculates Adler32 checksum of the written data,
    /// to allow writing ZLib header (RFC-1950). </summary>
    sealed class ZLibStream : DeflateStream {
        int adler32A = 1,
            adler32B;

        const int ChecksumModulus = 65521;

        public int Checksum {
            get {
                return ((adler32B * 65536) + adler32A);
            }
        }


        void UpdateChecksum( IList<byte> data, int offset, int length ) {
            for( int counter = 0; counter < length; ++counter ) {
                adler32A = (adler32A + (data[offset + counter])) % ChecksumModulus;
                adler32B = (adler32B + adler32A) % ChecksumModulus;
            }
        }


        public ZLibStream( Stream stream, CompressionMode mode, bool leaveOpen )
            : base( stream, mode, leaveOpen ) { }


        public override void Write( byte[] array, int offset, int count ) {
            UpdateChecksum( array, offset, count );
            base.Write( array, offset, count );
        }
    }
}