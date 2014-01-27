// Part of fCraft | Copyright (c) 2009-2014 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.MapGeneration {
    public class RealisticMapGenBlockTheme : IEquatable<RealisticMapGenBlockTheme>, ICloneable {

        static readonly Dictionary<MapGenTheme, RealisticMapGenBlockTheme> StandardThemes =
            new Dictionary<MapGenTheme, RealisticMapGenBlockTheme>();

        static RealisticMapGenBlockTheme() {
            StandardThemes.Add( MapGenTheme.Arctic,
                                new RealisticMapGenBlockTheme( MapGenTheme.Arctic ) );
            StandardThemes.Add( MapGenTheme.Desert,
                                new RealisticMapGenBlockTheme( MapGenTheme.Desert ) );
            StandardThemes.Add( MapGenTheme.Grass,
                                new RealisticMapGenBlockTheme( MapGenTheme.Grass ) );
            StandardThemes.Add( MapGenTheme.Forest,
                                new RealisticMapGenBlockTheme( MapGenTheme.Forest ) );
            StandardThemes.Add( MapGenTheme.Hell,
                                new RealisticMapGenBlockTheme( MapGenTheme.Hell ) );
            StandardThemes.Add( MapGenTheme.Swamp,
                                new RealisticMapGenBlockTheme( MapGenTheme.Swamp ) );
        }

        public Block AirBlock { get; set; }
        public Block BedrockBlock { get; set; }
        public Block CliffBlock { get; set; }
        public Block DeepWaterSurfaceBlock { get; set; }
        public Block FoliageBlock { get; set; }
        public Block GroundBlock { get; set; }
        public Block GroundSurfaceBlock { get; set; }
        public Block SeaFloorBlock { get; set; }
        public Block SnowBlock { get; set; }
        public Block TreeTrunkBlock { get; set; }
        public Block WaterBlock { get; set; }
        public Block WaterSurfaceBlock { get; set; }

        public int GroundThickness { get; set; }
        public int SeaFloorThickness { get; set; }


        public bool IsCustom {
            get { return !StandardThemes.Values.Any( Equals ); }
        }

        public MapGenTheme Theme {
            get {
                return StandardThemes.Where( pair => pair.Value.Equals( this ) )
                                     .Select( pair => pair.Key )
                                     .FirstOrDefault();
            }
        }

        public MapGenTheme BaseTheme { get; private set; }


        public RealisticMapGenBlockTheme( RealisticMapGenBlockTheme other ) {
            AirBlock = other.AirBlock;
            BedrockBlock = other.BedrockBlock;
            CliffBlock = other.CliffBlock;
            DeepWaterSurfaceBlock = other.DeepWaterSurfaceBlock;
            FoliageBlock = other.FoliageBlock;
            GroundBlock = other.GroundBlock;
            GroundSurfaceBlock = other.GroundSurfaceBlock;
            GroundThickness = other.GroundThickness;
            SeaFloorBlock = other.SeaFloorBlock;
            SeaFloorThickness = other.SeaFloorThickness;
            SnowBlock = other.SnowBlock;
            TreeTrunkBlock = other.TreeTrunkBlock;
            WaterBlock = other.WaterBlock;
            WaterSurfaceBlock = other.WaterSurfaceBlock;
        }


        public RealisticMapGenBlockTheme( MapGenTheme theme ) {
            GroundThickness = 5;
            SeaFloorThickness = 3;
            AirBlock = Block.Air;
            SnowBlock = Block.White;
            BaseTheme = theme;

            switch( theme ) {
                case MapGenTheme.Arctic:
                    WaterSurfaceBlock = Block.Glass;
                    DeepWaterSurfaceBlock = Block.Water;
                    GroundSurfaceBlock = Block.White;
                    WaterBlock = Block.Water;
                    GroundBlock = Block.White;
                    SeaFloorBlock = Block.White;
                    BedrockBlock = Block.Stone;
                    CliffBlock = Block.Stone;
                    GroundThickness = 1;
                    break;

                case MapGenTheme.Desert:
                    WaterSurfaceBlock = Block.Water;
                    DeepWaterSurfaceBlock = Block.Water;
                    GroundSurfaceBlock = Block.Sand;
                    WaterBlock = Block.Water;
                    GroundBlock = Block.Sand;
                    SeaFloorBlock = Block.Sand;
                    BedrockBlock = Block.Stone;
                    CliffBlock = Block.Gravel;
                    break;

                case MapGenTheme.Hell:
                    WaterSurfaceBlock = Block.Lava;
                    DeepWaterSurfaceBlock = Block.Lava;
                    GroundSurfaceBlock = Block.Obsidian;
                    WaterBlock = Block.Lava;
                    GroundBlock = Block.Stone;
                    SeaFloorBlock = Block.Obsidian;
                    BedrockBlock = Block.Stone;
                    CliffBlock = Block.Stone;
                    break;

                case MapGenTheme.Forest:
                case MapGenTheme.Grass:
                    WaterSurfaceBlock = Block.Water;
                    DeepWaterSurfaceBlock = Block.Water;
                    GroundSurfaceBlock = Block.Grass;
                    WaterBlock = Block.Water;
                    GroundBlock = Block.Dirt;
                    SeaFloorBlock = Block.Sand;
                    BedrockBlock = Block.Stone;
                    CliffBlock = Block.Stone;
                    break;

                case MapGenTheme.Swamp:
                    WaterSurfaceBlock = Block.Water;
                    DeepWaterSurfaceBlock = Block.Water;
                    GroundSurfaceBlock = Block.Dirt;
                    WaterBlock = Block.Water;
                    GroundBlock = Block.Dirt;
                    SeaFloorBlock = Block.Leaves;
                    BedrockBlock = Block.Stone;
                    CliffBlock = Block.Stone;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        #region Serialization

        public RealisticMapGenBlockTheme( [NotNull] XElement root )
            : this( MapGenTheme.Forest ) {
            if( root == null ) {
                throw new ArgumentNullException( "root" );
            }

            Block block;
            XElement xElement = root.Element( "AirBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                AirBlock = block;
            }
            xElement = root.Element( "BedrockBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                BedrockBlock = block;
            }
            xElement = root.Element( "CliffBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                CliffBlock = block;
            }
            xElement = root.Element( "DeepWaterSurfaceBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                DeepWaterSurfaceBlock = block;
            }
            xElement = root.Element( "FoliageBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                FoliageBlock = block;
            }
            xElement = root.Element( "GroundBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                GroundBlock = block;
            }
            xElement = root.Element( "GroundSurfaceBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                GroundSurfaceBlock = block;
            }
            xElement = root.Element( "SeaFloorBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                SeaFloorBlock = block;
            }
            xElement = root.Element( "SnowBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                SnowBlock = block;
            }
            xElement = root.Element( "TreeTrunkBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                TreeTrunkBlock = block;
            }
            xElement = root.Element( "WaterBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                WaterBlock = block;
            }
            xElement = root.Element( "WaterSurfaceBlock" );
            if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                WaterSurfaceBlock = block;
            }

            xElement = root.Element( "GroundThickness" );
            if( xElement != null ) {
                GroundThickness = Int32.Parse( xElement.Value );
            }
            xElement = root.Element( "SeaFloorThickness" );
            if( xElement != null ) {
                SeaFloorThickness = Int32.Parse( xElement.Value );
            }
        }


        [NotNull]
        public XElement Serialize() {
            XElement el = new XElement( "RealisticMapGenBlockTheme" );

            el.Add( new XElement( "AirBlock", AirBlock ) );
            el.Add( new XElement( "BedrockBlock", BedrockBlock ) );
            el.Add( new XElement( "CliffBlock", CliffBlock ) );
            el.Add( new XElement( "DeepWaterSurfaceBlock", DeepWaterSurfaceBlock ) );
            el.Add( new XElement( "FoliageBlock", FoliageBlock ) );
            el.Add( new XElement( "GroundBlock", GroundBlock ) );
            el.Add( new XElement( "GroundSurfaceBlock", GroundSurfaceBlock ) );
            el.Add( new XElement( "SeaFloorBlock", SeaFloorBlock ) );
            el.Add( new XElement( "SnowBlock", SnowBlock ) );
            el.Add( new XElement( "TreeTrunkBlock", TreeTrunkBlock ) );
            el.Add( new XElement( "WaterBlock", WaterBlock ) );
            el.Add( new XElement( "WaterSurfaceBlock", WaterSurfaceBlock ) );

            el.Add( new XElement( "GroundThickness", GroundThickness ) );
            el.Add( new XElement( "SeaFloorThickness", SeaFloorThickness ) );

            return el;
        }

        #endregion


        #region Equality members

        public bool Equals( RealisticMapGenBlockTheme other ) {
            if( ReferenceEquals( null, other ) ) {
                return false;
            }
            if( ReferenceEquals( this, other ) ) {
                return true;
            }
            return AirBlock == other.AirBlock && BedrockBlock == other.BedrockBlock &&
                   CliffBlock == other.CliffBlock && DeepWaterSurfaceBlock == other.DeepWaterSurfaceBlock &&
                   FoliageBlock == other.FoliageBlock && GroundBlock == other.GroundBlock &&
                   GroundSurfaceBlock == other.GroundSurfaceBlock && GroundThickness == other.GroundThickness &&
                   SeaFloorThickness == other.SeaFloorThickness && SeaFloorBlock == other.SeaFloorBlock &&
                   SnowBlock == other.SnowBlock && TreeTrunkBlock == other.TreeTrunkBlock &&
                   WaterBlock == other.WaterBlock && WaterSurfaceBlock == other.WaterSurfaceBlock;
        }

        public override bool Equals( object obj ) {
            if( ReferenceEquals( null, obj ) ) {
                return false;
            }
            if( ReferenceEquals( this, obj ) ) {
                return true;
            }
            if( obj.GetType() != GetType() ) {
                return false;
            }
            return Equals( (RealisticMapGenBlockTheme)obj );
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)AirBlock;
                hashCode = (hashCode*397) ^ (int)BedrockBlock;
                hashCode = (hashCode*397) ^ (int)CliffBlock;
                hashCode = (hashCode*397) ^ (int)DeepWaterSurfaceBlock;
                hashCode = (hashCode*397) ^ (int)FoliageBlock;
                hashCode = (hashCode*397) ^ (int)GroundBlock;
                hashCode = (hashCode*397) ^ (int)GroundSurfaceBlock;
                hashCode = (hashCode*397) ^ GroundThickness;
                hashCode = (hashCode*397) ^ SeaFloorThickness;
                hashCode = (hashCode*397) ^ (int)SeaFloorBlock;
                hashCode = (hashCode*397) ^ (int)SnowBlock;
                hashCode = (hashCode*397) ^ (int)TreeTrunkBlock;
                hashCode = (hashCode*397) ^ (int)WaterBlock;
                hashCode = (hashCode*397) ^ (int)WaterSurfaceBlock;
                return hashCode;
            }
        }

        public static bool operator ==( RealisticMapGenBlockTheme left, RealisticMapGenBlockTheme right ) {
            return Equals( left, right );
        }

        public static bool operator !=( RealisticMapGenBlockTheme left, RealisticMapGenBlockTheme right ) {
            return !Equals( left, right );
        }

        #endregion


        public object Clone() {
            return new RealisticMapGenBlockTheme( this );
        }
    }
}