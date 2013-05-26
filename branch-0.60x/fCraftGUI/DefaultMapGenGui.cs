namespace fCraft.GUI {
    public partial class DefaultMapGenGui : MapGeneratorGui {
        IMapGeneratorParameters args;
        public DefaultMapGenGui() {
            InitializeComponent();
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
