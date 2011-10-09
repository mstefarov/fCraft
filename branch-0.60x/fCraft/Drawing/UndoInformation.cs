using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public class UndoState {
        public UndoState( Queue<BlockUpdate> buffer ) {
            Op = null;
            Buffer = buffer;
        }
        public UndoState( DrawOperation op ) {
            Op = op;
            Buffer = new Queue<BlockUpdate>();
        }
        public readonly DrawOperation Op;
        public readonly Queue<BlockUpdate> Buffer;
    }
}
