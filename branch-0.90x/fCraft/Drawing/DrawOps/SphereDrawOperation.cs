// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    /// <summary> Draw operation that creates a solid sphere. </summary>
    public sealed class SphereDrawOperation : EllipsoidDrawOperation {
        public override string Name {
            get { return "Sphere"; }
        }

        /// <summary> Creates a new SphereDrawOperation for given player. </summary>
        public SphereDrawOperation( [NotNull] Player player )
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