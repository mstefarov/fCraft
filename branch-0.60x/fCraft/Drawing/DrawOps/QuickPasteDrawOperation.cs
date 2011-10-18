using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    class QuickPasteDrawOperation : PasteDrawOperation {
        public override string Name {
            get {
                if( Not ) {
                    return "QPasteNot";
                } else {
                    return "QPaste";
                }
            }
        }

        public QuickPasteDrawOperation( Player player, bool not )
            : base( player, not ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            return base.Prepare( new Vector3I[] { marks[0], marks[0] } );
        }
    }
}
