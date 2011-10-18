namespace fCraft.Drawing {
    sealed class QuickPasteDrawOperation : PasteDrawOperation {
        public override string Name {
            get {
                return Not ? "QPasteNot" : "QPaste";
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
