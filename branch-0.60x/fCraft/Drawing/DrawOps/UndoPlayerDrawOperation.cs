// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class UndoPlayerDrawOperation : BlockDBDrawOperation {
        public override string Name {
            get { return "UndoPlayer"; }
        }

        public override int ExpectedMarks {
            get { return 0; }
        }

        public override string Description {
            get {
                if( String.IsNullOrEmpty( UndoParamDescription ) ) {
                    return Name;
                } else {
                    return String.Format( "{0}({1})", Name, UndoParamDescription );
                }
            }
        }

        public string UndoParamDescription { get; set; }


        public UndoPlayerDrawOperation( Player player, BlockDBEntry[] entries )
            : base( player ) {
            entriesToUndo = entries;
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;
            Bounds = Map.Bounds;
            return true;
        }
    }
}