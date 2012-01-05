// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    /// <summary> Sphere implementation of the DrawOperation class. </summary>
    public sealed class SphereDrawOperation : EllipsoidDrawOperation {
        public override string Name {
            get { return "Sphere"; }
        }

        /// <summary> Initialises a new intance of SphereDrawOperation, using the specified player. </summary>
        /// <param name="player"> Player who is executing the draw operation. </param>
        public SphereDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].Z - marks[1].Z) * (marks[0].Z - marks[1].Z) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].Z = (short)Math.Round( marks[0].Z - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].Z = (short)Math.Round( marks[0].Z + radius );

            return base.Prepare( marks );
        }
    }
}