using System;
using System.Windows.Forms;

namespace fCraft.GUI {
    public partial class DefaultMapGenGui : MapGeneratorGui {
        IMapGeneratorParameters args;
        public DefaultMapGenGui() {
            InitializeComponent();
            ParentChanged += DefaultMapGenGui_ParentChanged;
        }

        Control oldParent;
        void DefaultMapGenGui_ParentChanged( object sender, EventArgs e ) {
            if( oldParent != null ) {
                oldParent.SizeChanged -= Parent_SizeChanged;
            } else {
                Parent_SizeChanged( Parent, EventArgs.Empty );
            }
            if( Parent != null ){
                Parent.SizeChanged += Parent_SizeChanged;
            }
            oldParent = Parent;
        }

        void Parent_SizeChanged( object sender, EventArgs e ) {
            Size = Parent.Size;
        }

        public override void SetParameters( IMapGeneratorParameters generatorParameters ) {
            args = generatorParameters;
            pgGrid.SelectedObject = args;
        }

        public override IMapGeneratorParameters GetParameters() {
            return args;
        }

        public override void OnMapDimensionChange( int width, int length, int height ) {
            args.MapWidth = width;
            args.MapLength = length;
            args.MapHeight = height;
        }
    }
}
