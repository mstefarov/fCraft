using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {
    public class RealisticMapGenTheme : IEquatable<RealisticMapGenTheme> {
        static readonly Dictionary<MapGenTheme, RealisticMapGenTheme> StandardThemes =
            new Dictionary<MapGenTheme, RealisticMapGenTheme>();

        static RealisticMapGenTheme() {
            StandardThemes.Add( MapGenTheme.Arctic, new RealisticMapGenTheme( MapGenTheme.Arctic ) );
            StandardThemes.Add( MapGenTheme.Desert, new RealisticMapGenTheme( MapGenTheme.Desert ) );
            StandardThemes.Add( MapGenTheme.Forest, new RealisticMapGenTheme( MapGenTheme.Forest ) );
            StandardThemes.Add( MapGenTheme.Hell, new RealisticMapGenTheme( MapGenTheme.Hell ) );
            StandardThemes.Add( MapGenTheme.Swamp, new RealisticMapGenTheme( MapGenTheme.Swamp ) );
        }


        public MapGenTheme Theme { get; set; }

        public Block AirBlock { get; set; }
        public Block WaterSurfaceBlock { get; set; }
        public Block GroundSurfaceBlock { get; set; }
        public Block WaterBlock { get; set; }
        public Block GroundBlock { get; set; }
        public Block SeaFloorBlock { get; set; }
        public Block BedrockBlock { get; set; }
        public Block DeepWaterSurfaceBlock { get; set; }
        public Block CliffBlock { get; set; }
        public Block SnowBlock { get; set; }
        public Block FoliageBlock { get; set; }
        public Block TreeTrunkBlock { get; set; }
        public int GroundThickness { get; set; }
        public int SeaFloorThickness { get; set; }

        public bool IsCustom {
            get { return !StandardThemes.Values.Any( Equals ); }
        }


        public RealisticMapGenTheme( MapGenTheme theme ) {
            Theme = theme;
            GroundThickness = 5;
            SeaFloorThickness = 3;
            AirBlock = Block.Air;
            SnowBlock = Block.White;

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


        public RealisticMapGenTheme( [NotNull] XElement root )
            : this( MapGenTheme.Forest ) {
            if( root == null ) {
                throw new ArgumentNullException( "root" );
            }

            XElement xElement = root.Element( "IsCustom" );
            if( xElement == null ) throw new SerializationException( "No IsCustom tag in RealisticMapGenTheme" );
            bool isCustom = Boolean.Parse( xElement.Value );
            if( isCustom ) {
                Block block;
                xElement = root.Element( "AirBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    AirBlock = block;
                }
                xElement = root.Element( "WaterSurfaceBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    WaterSurfaceBlock = block;
                }
                xElement = root.Element( "GroundSurfaceBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    GroundSurfaceBlock = block;
                }
                xElement = root.Element( "WaterBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    WaterBlock = block;
                }
                xElement = root.Element( "GroundBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    GroundBlock = block;
                }
                xElement = root.Element( "SeaFloorBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    SeaFloorBlock = block;
                }
                xElement = root.Element( "BedrockBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    BedrockBlock = block;
                }
                xElement = root.Element( "DeepWaterSurfaceBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    DeepWaterSurfaceBlock = block;
                }
                xElement = root.Element( "CliffBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    CliffBlock = block;
                }
                xElement = root.Element( "SnowBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    SnowBlock = block;
                }
                xElement = root.Element( "FoliageBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    FoliageBlock = block;
                }
                xElement = root.Element( "TreeTrunkBlock" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out block, true ) ) {
                    TreeTrunkBlock = block;
                }
                xElement = root.Element( "GroundThickness" );
                if( xElement != null ) {
                    GroundThickness = Int32.Parse( xElement.Value );
                }
                xElement = root.Element( "SeaFloorThickness" );
                if( xElement != null ) {
                    SeaFloorThickness = Int32.Parse( xElement.Value );
                }

            } else {
                MapGenTheme theme;
                xElement = root.Element( "MapGenTheme" );
                if( xElement != null && EnumUtil.TryParse( xElement.Value, out theme, true ) ) {
                    Theme = theme;
                }
            }
        }


        [NotNull]
        public XElement Serialize() {
            XElement el = new XElement( "RealisticMapGenTheme" );
            el.Add( new XElement( "IsCustom", IsCustom ) );
            if( IsCustom ) {
                el.Add( new XElement( "AirBlock", AirBlock ) );
                el.Add( new XElement( "WaterSurfaceBlock", WaterSurfaceBlock ) );
                el.Add( new XElement( "GroundSurfaceBlock", GroundSurfaceBlock ) );
                el.Add( new XElement( "WaterBlock", WaterBlock ) );
                el.Add( new XElement( "GroundBlock", GroundBlock ) );
                el.Add( new XElement( "SeaFloorBlock", SeaFloorBlock ) );
                el.Add( new XElement( "BedrockBlock", BedrockBlock ) );
                el.Add( new XElement( "DeepWaterSurfaceBlock", DeepWaterSurfaceBlock ) );
                el.Add( new XElement( "CliffBlock", CliffBlock ) );
                el.Add( new XElement( "SnowBlock", SnowBlock ) );
                el.Add( new XElement( "FoliageBlock", FoliageBlock ) );
                el.Add( new XElement( "TreeTrunkBlock", TreeTrunkBlock ) );
                el.Add( new XElement( "GroundThickness", GroundThickness ) );
                el.Add( new XElement( "SeaFloorThickness", SeaFloorThickness ) );
            } else {
                el.Add( new XElement( "Theme", Theme ) );
            }
            return el;
        }


        #region Equality members

        public override bool Equals( object obj ) {
            if( ReferenceEquals( null, obj ) ) {
                return false;
            }else if( ReferenceEquals( this, obj ) ) {
                return true;
            }else if( obj.GetType() != GetType() ) {
                return false;
            } else {
                return Equals( (RealisticMapGenTheme)obj );
            }
        }


        public bool Equals( RealisticMapGenTheme other ) {
            if( ReferenceEquals( null, other ) ) {
                return false;
            }
            if( ReferenceEquals( this, other ) ) {
                return true;
            }
            return Theme == other.Theme && AirBlock == other.AirBlock && WaterSurfaceBlock == other.WaterSurfaceBlock &&
                   GroundSurfaceBlock == other.GroundSurfaceBlock && WaterBlock == other.WaterBlock &&
                   GroundBlock == other.GroundBlock && SeaFloorBlock == other.SeaFloorBlock &&
                   BedrockBlock == other.BedrockBlock && DeepWaterSurfaceBlock == other.DeepWaterSurfaceBlock &&
                   CliffBlock == other.CliffBlock && SnowBlock == other.SnowBlock && FoliageBlock == other.FoliageBlock &&
                   TreeTrunkBlock == other.TreeTrunkBlock && GroundThickness == other.GroundThickness &&
                   SeaFloorThickness == other.SeaFloorThickness;
        }


        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)Theme;
                hashCode = (hashCode * 397) ^ (int)AirBlock;
                hashCode = (hashCode * 397) ^ (int)WaterSurfaceBlock;
                hashCode = (hashCode * 397) ^ (int)GroundSurfaceBlock;
                hashCode = (hashCode * 397) ^ (int)WaterBlock;
                hashCode = (hashCode * 397) ^ (int)GroundBlock;
                hashCode = (hashCode * 397) ^ (int)SeaFloorBlock;
                hashCode = (hashCode * 397) ^ (int)BedrockBlock;
                hashCode = (hashCode * 397) ^ (int)DeepWaterSurfaceBlock;
                hashCode = (hashCode * 397) ^ (int)CliffBlock;
                hashCode = (hashCode * 397) ^ (int)SnowBlock;
                hashCode = (hashCode * 397) ^ (int)FoliageBlock;
                hashCode = (hashCode * 397) ^ (int)TreeTrunkBlock;
                hashCode = (hashCode * 397) ^ GroundThickness;
                hashCode = (hashCode * 397) ^ SeaFloorThickness;
                return hashCode;
            }
        }

        #endregion
    }
}