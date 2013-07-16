// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    /// <summary> Draw operation that handles non-aligned (single-mark) pasting for /Paste and /PasteNot.
    /// Preserves original orientation of the CopyState. </summary>
    sealed class QuickPasteDrawOperation : PasteDrawOperation {
        public override string Name {
            get {
                return Not ? "PasteNot" : "Paste";
            }
        }

        public QuickPasteDrawOperation( Player player, bool not )
            : base( player, not ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            return base.Prepare( new[] { marks[0], marks[0] } );
        }
    }
}
