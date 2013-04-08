using System;
using System.ComponentModel;

namespace fCraft {
    interface IMapGenerator {
        string Name { get; }
        Version Version { get; }

        // raised as the map generation progresses (optional)
        event ProgressChangedEventHandler ProgressChanged;

        // whether this generator provides a GUI
        bool ProvidesGui { get; }

        // gets an object representing current generation parameters
        object GetParameters();

        // sets an object representing generation parameters
        void SetParameters( object parameters );

        // starts generation!
        void Generate( int width, int height, int length );
    }
}