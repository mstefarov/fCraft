// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft {
    /// <summary> Represets a set of map generator parameters.
    /// Provides a way to serialize these parameters to string, and a way to create single-use IMapGeneratorState objects. 
    /// Must be mutable, should implement parameter range validation in property setters, and implement ICloneable,
    /// and pass a COPY to IMapGeneratorState. </summary>
    public interface IMapGeneratorParameters : ICloneable {
        /// <summary> Associated IMapGenerator object that created this parameter set. </summary>
        IMapGenerator Generator { get; }

        /// <summary> Short summary of current parameter set, or name of this template. </summary>
        string SummaryString { get; }

        /// <summary> Saves current generation parameters to a string,
        /// in a format that's expected to be readable by IMapGenerator.CreateParameters(string) </summary>
        string Save();

        /// <summary> Creates IMapGeneratorState to create a map with the current parameters and specified dimensions. 
        /// Does NOT start the generation process yet -- that should be done in IMapGeneratorState.Generate() </summary>
        IMapGeneratorState CreateGenerator( int width, int height, int length );
    }
}