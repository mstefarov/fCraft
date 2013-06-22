namespace fCraft.GUI {
    public partial class DefaultMapGenGui : MapGeneratorGui {
        MapGeneratorParameters args;

        public DefaultMapGenGui() {
            InitializeComponent();
        }

        public override void SetParameters( MapGeneratorParameters generatorParameters ) {
            args = generatorParameters;
            pgGrid.SelectedObject = args;
        }

        public override MapGeneratorParameters GetParameters() {
            return args;
        }

        public override void OnMapDimensionChange( int width, int length, int height ) {
            args.MapWidth = width;
            args.MapLength = length;
            args.MapHeight = height;
        }
    }
}