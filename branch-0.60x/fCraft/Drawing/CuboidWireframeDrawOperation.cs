// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft.Drawing {
    public sealed class CuboidWireframeDrawOperation : DrawOperation {
        const int DrawStride = 16;

        public override string Name {
            get { return "CuboidWX"; }
        }

        public override string Description {
            get { return Name; }
        }

        public CuboidWireframeDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Begin( Position[] marks ) {
            if( !base.Begin( marks ) ) return false;

            int hollowVolume = Math.Max( 0, Bounds.WidthX - 2 ) * Math.Max( 0, Bounds.WidthY - 2 ) * Math.Max( 0, Bounds.Height - 2 );
            int sideVolume = Math.Max( 0, Bounds.WidthX - 2 ) * Math.Max( 0, Bounds.WidthY - 2 ) * (Bounds.XMax != Bounds.XMin ? 2 : 1) +
                             Math.Max( 0, Bounds.WidthY - 2 ) * Math.Max( 0, Bounds.Height - 2 ) * (Bounds.YMax != Bounds.YMin ? 2 : 1) +
                             Math.Max( 0, Bounds.Height - 2 ) * Math.Max( 0, Bounds.WidthX - 2 ) * (Bounds.HMax != Bounds.HMin ? 2 : 1);

            BlocksTotalEstimate = Bounds.Volume - hollowVolume - sideVolume;

            coordEnumerator = BlockEnumerator().GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator.MoveNext() ) {
                Coords = coordEnumerator.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        IEnumerable<Vector3I> BlockEnumerator() {
            // Draw cuboid vertices
            yield return new Vector3I( Bounds.XMin, Bounds.YMin, Bounds.HMin );

            if( Bounds.XMin != Bounds.XMax ) yield return new Vector3I( Bounds.XMax, Bounds.YMin, Bounds.HMin );
            if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMax, Bounds.HMin );
            if( Bounds.HMin != Bounds.HMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMin, Bounds.HMax );

            if( Bounds.XMin != Bounds.XMax && Bounds.YMin != Bounds.YMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMax, Bounds.HMin );
            if( Bounds.YMin != Bounds.YMax && Bounds.HMin != Bounds.HMax )
                yield return new Vector3I( Bounds.XMin, Bounds.YMax, Bounds.HMax );
            if( Bounds.HMin != Bounds.HMax && Bounds.XMin != Bounds.XMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMin, Bounds.HMax );

            if( Bounds.XMin != Bounds.XMax && Bounds.YMin != Bounds.YMax && Bounds.HMin != Bounds.HMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMax, Bounds.HMax );

            // Draw edges along the X axis
            if( Bounds.WidthX > 2 ) {
                for( int x = Bounds.XMin + 1; x < Bounds.XMax; x++ ) {
                    yield return new Vector3I( x, Bounds.YMin, Bounds.HMin );
                    if( Bounds.HMin != Bounds.HMax ) yield return new Vector3I( x, Bounds.YMin, Bounds.HMax );
                    if( Bounds.YMin != Bounds.YMax ) {
                        yield return new Vector3I( x, Bounds.YMax, Bounds.HMin );
                        if( Bounds.HMin != Bounds.HMax ) yield return new Vector3I( x, Bounds.YMax, Bounds.HMax );
                    }
                }
            }

            // Draw edges along the Y axis
            if( Bounds.WidthY > 2 ) {
                for( int y = Bounds.YMin + 1; y < Bounds.YMax; y++ ) {
                    yield return new Vector3I( Bounds.XMin, y, Bounds.HMin );
                    if( Bounds.HMin != Bounds.HMax ) yield return new Vector3I( Bounds.XMin, y, Bounds.HMax );
                    if( Bounds.XMin != Bounds.XMax ) {
                        yield return new Vector3I( Bounds.XMax, y, Bounds.HMin );
                        if( Bounds.HMin != Bounds.HMax ) yield return new Vector3I( Bounds.XMax, y, Bounds.HMax );
                    }
                }
            }

            // Draw edges along the H axis
            if( Bounds.Height > 2 ) {
                for( int h = Bounds.HMin + 1; h < Bounds.HMax; h++ ) {
                    yield return new Vector3I( Bounds.XMin, Bounds.YMin, h );
                    if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMax, h );
                    if( Bounds.XMin != Bounds.XMax ) {
                        yield return new Vector3I( Bounds.XMax, Bounds.YMax, h );
                        if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMax, Bounds.YMin, h );
                    }
                }
            }
        }
    }
}