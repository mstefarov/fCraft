// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace fCraft {
    /// <summary> Represets a set of map generator parameters.
    /// Provides a way to serialize these parameters to string, and a way to create single-use MapGeneratorState objects. 
    /// Must be mutable, should implement parameter range validation in property setters, and implement ICloneable,
    /// and pass a COPY to MapGeneratorState. </summary>
    public abstract class MapGeneratorParameters : ICloneable {
        /// <summary> Associated IMapGenerator object that created this parameter set. </summary>
        [Browsable( false )]
        public IMapGenerator Generator { get; protected set; }

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
        /// in a format that's expected to be readable by IMapGenerator.CreateParameters(XElement) </summary>
        /// <param name="baseElement"> Element onto which the parameters should be attached. </param>
        public abstract void Save( XElement baseElement );

        /// <summary> Creates MapGeneratorState to create a map with the current parameters and specified dimensions. 
        /// Does NOT start the generation process yet -- that should be done in MapGeneratorState.Generate() </summary>
        public abstract MapGeneratorState CreateGenerator();

        public abstract object Clone();


        protected static bool ReadBool( XElement rootEl, string name, bool defaultVal ) {
            bool val;
            XElement el = rootEl.Element( name );
            if( el != null && Boolean.TryParse( el.Value, out val ) ) {
                return val;
            } else {
                return defaultVal;
            }
        }

        protected static string ReadString( XElement rootEl, string name, string defaultVal ) {
            XElement el = rootEl.Element( name );
            if( el != null ) {
                return el.Value;
            } else {
                return defaultVal;
            }
        }

        protected static int ReadInt( XElement rootEl, string name, int defaultVal ) {
            int val;
            XElement el = rootEl.Element( name );
            if( el != null && Int32.TryParse( el.Value, out val ) ) {
                return val;
            } else {
                return defaultVal;
            }
        }

        protected static double ReadDouble( XElement rootEl, string name, double defaultVal ) {
            double val;
            XElement el = rootEl.Element( name );
            if( el != null && Double.TryParse( el.Value, out val ) ) {
                return val;
            } else {
                return defaultVal;
            }
        }

        protected static TEnum ReadEnum<TEnum>( XElement rootEl, string name, TEnum defaultVal ) {
            TEnum val;
            XElement el = rootEl.Element( name );
            if( el != null && EnumUtil.TryParse( el.Value, out val, true ) ) {
                return val;
            } else {
                return defaultVal;
            }
        }
    }
}