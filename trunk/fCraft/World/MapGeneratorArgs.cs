using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace fCraft {


    public sealed class MapGeneratorArgs {
        const int FormatVersion = 2;

        public MapGenTheme theme;
        public int seed, dimX, dimY, dimH, maxHeight, maxDepth, waterLevel;
        public bool addWater;

        public bool matchWaterCoverage;
        public float waterCoverage;
        public int raisedCorners, loweredCorners, midPoint;
        public float bias;
        public bool useBias;

        public int detailScale, featureScale;
        public float roughness;
        public bool layeredHeightmap, marbledHeightmap, invertHeightmap;

        public bool addTrees;
        public int treeSpacingMin, treeSpacingMax, treeHeightMin, treeHeightMax;

        public bool addCaves, addOre, addCaveWater, addCaveLava;
        public float caveDensity, caveSize;

        public void Validate() {
            if( raisedCorners < 0 || raisedCorners > 4 || loweredCorners < 0 || raisedCorners > 4 || raisedCorners + loweredCorners > 4 ) {
                throw new ArgumentOutOfRangeException( "raisedCorners and loweredCorners must be between 0 and 4." );
            }

            if( caveDensity <= 0 || caveSize <= 0 ) {
                throw new ArgumentOutOfRangeException( "caveDensity and caveSize must be > 0" );
            }
            // todo: additional validation
        }

        public MapGeneratorArgs() {
            theme = MapGenTheme.Forest;
            seed = (new Random()).Next();
            dimX = 128;
            dimY = 128;
            dimH = 80;
            maxHeight = 20;
            maxDepth = 12;
            waterLevel = 40;
            addWater = true;

            matchWaterCoverage = false;
            waterCoverage = .5f;
            raisedCorners = 0;
            loweredCorners = 0;
            midPoint = 0;
            bias = 0;
            useBias = false;

            detailScale = 7;
            featureScale = 1;
            roughness = .5f;
            layeredHeightmap = false;
            marbledHeightmap = false;
            invertHeightmap = false;

            addTrees = true;
            treeSpacingMin = 7;
            treeSpacingMax = 11;
            treeHeightMin = 5;
            treeHeightMax = 7;

            addCaves = false;
            addCaveLava = false;
            addCaveWater = false;
            addOre = false;
            caveDensity = 2;
            caveSize = 1;
        }

        public MapGeneratorArgs( string fileName ) {
            XDocument doc = XDocument.Load( fileName );
            XElement root = doc.Root;

            XAttribute versionTag = root.Attribute( "version" );
            int version = 0;
            if( versionTag != null && versionTag.Value != null && versionTag.Value.Length > 0 ) {
                version = Int32.Parse( versionTag.Value );
            }

            theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), root.Element( "theme" ).Value, true );
            seed = Int32.Parse( root.Element( "seed" ).Value );
            dimX = Int32.Parse( root.Element( "dimX" ).Value );
            dimY = Int32.Parse( root.Element( "dimY" ).Value );
            dimH = Int32.Parse( root.Element( "dimH" ).Value );
            maxHeight = Int32.Parse( root.Element( "maxHeight" ).Value );
            maxDepth = Int32.Parse( root.Element( "maxDepth" ).Value );
            waterLevel = Int32.Parse( root.Element( "waterLevel" ).Value );
            addWater = Boolean.Parse( root.Element( "addWater" ).Value );

            matchWaterCoverage = Boolean.Parse( root.Element( "matchWaterCoverage" ).Value );
            waterCoverage = float.Parse( root.Element( "waterCoverage" ).Value );
            raisedCorners = Int32.Parse( root.Element( "raisedCorners" ).Value );
            loweredCorners = Int32.Parse( root.Element( "loweredCorners" ).Value );
            midPoint = Int32.Parse( root.Element( "midPoint" ).Value );
            bias = float.Parse( root.Element( "bias" ).Value );
            useBias = Boolean.Parse( root.Element( "useBias" ).Value );

            if( version == 0 ) {
                detailScale = Int32.Parse( root.Element( "minDetailSize" ).Value );
                featureScale = Int32.Parse( root.Element( "maxDetailSize" ).Value );
            } else {
                detailScale = Int32.Parse( root.Element( "detailScale" ).Value );
                featureScale = Int32.Parse( root.Element( "featureScale" ).Value );
            }
            roughness = float.Parse( root.Element( "roughness" ).Value );
            layeredHeightmap = Boolean.Parse( root.Element( "layeredHeightmap" ).Value );
            marbledHeightmap = Boolean.Parse( root.Element( "marbledHeightmap" ).Value );
            invertHeightmap = Boolean.Parse( root.Element( "invertHeightmap" ).Value );

            addTrees = Boolean.Parse( root.Element( "addTrees" ).Value );
            treeSpacingMin = Int32.Parse( root.Element( "treeSpacingMin" ).Value );
            treeSpacingMax = Int32.Parse( root.Element( "treeSpacingMax" ).Value );
            treeHeightMin = Int32.Parse( root.Element( "treeHeightMin" ).Value );
            treeHeightMax = Int32.Parse( root.Element( "treeHeightMax" ).Value );

            if( root.Element( "addCaves" ) != null ) {
                addCaves = Boolean.Parse( root.Element( "addCaves" ).Value );
                addCaveLava = Boolean.Parse( root.Element( "addCaveLava" ).Value );
                addCaveWater = Boolean.Parse( root.Element( "addCaveWater" ).Value );
                addOre = Boolean.Parse( root.Element( "addOre" ).Value );
                caveDensity = float.Parse( root.Element( "caveDensity" ).Value );
                caveSize = float.Parse( root.Element( "caveSize" ).Value );
            }

            Validate();
        }

        const string RootTagName = "fCraftMapGeneratorArgs";
        public void Save( string fileName ) {
            XDocument document = new XDocument();
            XElement root = new XElement( RootTagName );

            root.Add( new XAttribute( "version", FormatVersion ) );

            root.Add( new XElement( "theme", theme ) );
            root.Add( new XElement( "seed", seed ) );
            root.Add( new XElement( "dimX", dimX ) );
            root.Add( new XElement( "dimY", dimY ) );
            root.Add( new XElement( "dimH", dimH ) );
            root.Add( new XElement( "maxHeight", maxHeight ) );
            root.Add( new XElement( "maxDepth", maxDepth ) );
            root.Add( new XElement( "waterLevel", waterLevel ) );
            root.Add( new XElement( "addWater", addWater ) );

            root.Add( new XElement( "matchWaterCoverage", matchWaterCoverage ) );
            root.Add( new XElement( "waterCoverage", waterCoverage ) );
            root.Add( new XElement( "raisedCorners", raisedCorners ) );
            root.Add( new XElement( "loweredCorners", loweredCorners ) );
            root.Add( new XElement( "midPoint", midPoint ) );
            root.Add( new XElement( "bias", bias ) );
            root.Add( new XElement( "useBias", useBias ) );

            root.Add( new XElement( "detailScale", detailScale ) );
            root.Add( new XElement( "featureScale", featureScale ) );
            root.Add( new XElement( "roughness", roughness ) );
            root.Add( new XElement( "layeredHeightmap", layeredHeightmap ) );
            root.Add( new XElement( "marbledHeightmap", marbledHeightmap ) );
            root.Add( new XElement( "invertHeightmap", invertHeightmap ) );

            root.Add( new XElement( "addTrees", addTrees ) );
            root.Add( new XElement( "treeSpacingMin", treeSpacingMin ) );
            root.Add( new XElement( "treeSpacingMax", treeSpacingMax ) );
            root.Add( new XElement( "treeHeightMin", treeHeightMin ) );
            root.Add( new XElement( "treeHeightMax", treeHeightMax ) );

            root.Add( new XElement( "addCaves", addCaves ) );
            root.Add( new XElement( "addCaveLava", addCaveLava ) );
            root.Add( new XElement( "addCaveWater", addCaveWater ) );
            root.Add( new XElement( "addOre", addOre ) );
            root.Add( new XElement( "caveDensity", caveDensity ) );
            root.Add( new XElement( "caveSize", caveSize ) );

            document.Add( root );
            document.Save( fileName );
        }
    }

}
