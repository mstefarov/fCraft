// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    /// <summary> Represents a set of map generator parameters.
    /// Provides a way to serialize these parameters to string, and a way to create single-use MapGeneratorState objects. 
    /// Must be mutable, should implement parameter range validation in property setters, and implement ICloneable,
    /// and pass a COPY to MapGeneratorState. </summary>
    public abstract class MapGeneratorParameters : ICloneable {
        /// <summary> Associated MapGenerator object that created this parameter set. </summary>
        [NotNull]
        [Browsable( false )]
        [XmlIgnore]
        public MapGenerator Generator { get; protected set; }

        /// <summary> Width (X-dimension) of the map being generated. </summary>
        [Browsable( false )]
        public int MapWidth { get; set; }

        /// <summary> Length (Y-dimension) of the map being generated. </summary>
        [Browsable( false )]
        public int MapLength { get; set; }

        /// <summary> Height (Z-dimension) of the map being generated. </summary>
        [Browsable( false )]
        public int MapHeight { get; set; }

        /// <summary> Saves current generation parameters to XML,
        /// in a format that's expected to be readable by MapGenerator.CreateParameters(XElement) </summary>
        /// <param name="baseElement"> Element onto which the parameters should be attached.
        /// Each property will correspond to a child element. </param>
        public virtual void Save( [NotNull] XElement baseElement ) {
            if( baseElement == null ) throw new ArgumentNullException( "baseElement" );
            foreach( PropertyInfo pi in ListProperties() ) {
                baseElement.Add( new XElement( pi.Name, pi.GetValue( this, null ) ) );
            }
        }


        /// <summary> Loads generation parameters from XML.
        /// All read-write public properties, except those with [XmlIgnore] attribute, are considered.
        /// If no corresponding XML element exists, property value is unchanged. </summary>
        /// <param name="baseElement"> Element from which parameters are read.
        /// Each property corresponds to a child element. </param>
        public virtual void LoadProperties( [NotNull] XElement baseElement ) {
            if( baseElement == null ) throw new ArgumentNullException( "baseElement" );
            foreach( PropertyInfo pi in ListProperties() ) {
                XElement el = baseElement.Element( pi.Name );
                if( el == null ) continue;
                TypeConverter tc = TypeDescriptor.GetConverter( pi.PropertyType );
                pi.SetValue( this, tc.ConvertFromString( el.Value ), null );
            }
        }


        public virtual object Clone() {
            object newObject = Activator.CreateInstance( GetType() );
            foreach( PropertyInfo pi in ListProperties() ) {
                object val = pi.GetValue( this, null );
                pi.SetValue( newObject, val, null );
            }
            return newObject;
        }


        /// <summary> Creates MapGeneratorState to create a map with the current parameters and specified dimensions. 
        /// Does NOT start the generation process yet -- that should be done in MapGeneratorState.Generate() </summary>
        [NotNull]
        public abstract MapGeneratorState CreateGenerator();


        static readonly object PropListsLock = new object();
        static readonly Dictionary<Type, PropertyInfo[]> PropLists = new Dictionary<Type, PropertyInfo[]>();

        [NotNull]
        IEnumerable<PropertyInfo> ListProperties() {
            Type thisType = GetType();
            PropertyInfo[] result;
            lock( PropListsLock ) {
                if( !PropLists.TryGetValue( thisType, out result ) ) {
                    PropertyInfo[] properties = GetType().GetProperties( BindingFlags.Instance | BindingFlags.Public );
                    result = properties.Where( pi =>
                                               // make sure it's read-write
                                               pi.CanRead && pi.CanWrite &&
                                               // make sure it's not excluded from serialization
                                               !pi.GetCustomAttributes( typeof( XmlIgnoreAttribute ), true ).Any() &&
                                               // make sure it's not an indexer
                                               pi.GetIndexParameters().Length == 0 ).ToArray();
                    PropLists.Add( thisType, result );
                }
            }
            return result;
        }


        public override string ToString() {
            return Generator.Name;
        }
    }
}