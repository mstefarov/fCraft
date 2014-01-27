// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;
using JetBrains.Annotations;
using fCraft.MapGeneration;

namespace fCraft.GUI {
    /// <summary> Class that provides a GUI (UserControl) for adjusting map parameters. </summary>
    /// <remarks>It would make sense for this class to be abstract, but VisualStudio's Designer
    /// does not support controls derived from abstract classes.
    /// See http://stackoverflow.com/questions/2764757/ </remarks>
    public class MapGeneratorGui : UserControl {
        Control oldParent;

        protected MapGeneratorGui() {
            Padding = new Padding( 0 );
            Margin = new Padding( 0 );
            BorderStyle = BorderStyle.None;
        }

        protected override void OnParentChanged( EventArgs e ) {
            if( oldParent != null ) {
                oldParent.SizeChanged -= Parent_SizeChanged;
            } else {
                Parent_SizeChanged( Parent, EventArgs.Empty );
            }
            if( Parent != null ) {
                Parent.SizeChanged += Parent_SizeChanged;
            }
            oldParent = Parent;
            base.OnParentChanged( e );
        }

        void Parent_SizeChanged( object sender, EventArgs e ) {
            if( !DesignMode ) {
                // auto-resize only when not in Designer mode (in VS)
                Size = Parent.Size;
            }
        }


        /// <summary> Reads given generator params, and adjusts GUI to reflect them. </summary>
        /// <param name="generatorParameters"> Given generation parameters. </param>
        public virtual void SetParameters( [NotNull] MapGeneratorParameters generatorParameters ) {
            throw new NotImplementedException( "SetParameters must be overriden in MapGeneratorGui implementations." );
        }


        /// <summary> Creates mapgen parameters based on the current GUI state.
        /// The returned IMapGeneratorParameters must not be modified after being returned. </summary>
        /// <returns> IMapGeneratorParameters object representing GUI's current state. </returns>
        [NotNull]
        public virtual MapGeneratorParameters GetParameters() {
            throw new NotImplementedException( "GetParameters must be overriden in MapGeneratorGui implementations." );
        }


        /// <summary> Called by parent dialog when map dimension NumericUpDown controls have changed.
        /// Used to adjust any settings that may rely on map dimensions for scaling. </summary>
        /// <param name="width"> Map width (horizontal, x dimension), in blocks. </param>
        /// <param name="length"> Map length (horizontal, y dimension), in blocks. </param>
        /// <param name="height"> Map height (vertical, z dimension), in blocks. </param>
        public virtual void OnMapDimensionChange( int width, int length, int height ) {
            throw new NotImplementedException(
                "OnMapDimensionChange must be overriden in MapGeneratorGui implementations." );
        }
    }
}