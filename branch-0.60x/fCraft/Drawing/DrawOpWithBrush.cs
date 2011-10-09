// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public abstract class DrawOpWithBrush : DrawOperation, IBrushFactory, IBrush, IBrushInstance {
        public override string DescriptionWithBrush {
            get { return Description; }
        }


        protected DrawOpWithBrush( Player player )
            : base( player ) {
        }


        public abstract bool ReadParams( Command cmd );


        protected abstract Block NextBlock();


        #region IBrushFactory Members

        string IBrushFactory.Name {
            get { return Name; }
        }

        string IBrushFactory.Help {
            get { throw new NotImplementedException(); }
        }

        string[] IBrushFactory.Aliases {
            get { throw new NotImplementedException(); }
        }

        IBrush IBrushFactory.MakeBrush( Player player, Command cmd ) {
            return this;
        }

        #endregion


        #region IBrush Members

        IBrushFactory IBrush.Factory {
            get { return this; }
        }

        string IBrush.Description {
            get { throw new NotImplementedException(); }
        }

        IBrushInstance IBrush.MakeInstance( Player player, Command cmd, DrawOperation op ) {
            if( ReadParams( cmd ) ) {
                return this;
            } else {
                return null;
            }
        }

        #endregion


        #region IBrushInstance Members

        IBrush IBrushInstance.Brush {
            get { return this; }
        }

        string IBrushInstance.InstanceDescription {
            get { return DescriptionWithBrush; }
        }

        bool IBrushInstance.HasAlternateBlock {
            get { return false; }
        }

        bool IBrushInstance.Begin( Player player, DrawOperation op ) {
            return true;
        }

        Block IBrushInstance.NextBlock( DrawOperation op ) {
            return NextBlock();
        }

        void IBrushInstance.End() { }

        #endregion
    }
}