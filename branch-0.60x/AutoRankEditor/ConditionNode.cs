using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoRankEditor {
    class ConditionNode : TreeNode {
        public ConditionField Field { get; set; }
        public ComparisonOperation Op { get; set; }
        public int Value { get; set; }
        public ConditionNode() : base() { }
        public ConditionNode( string conditionDesc )
            : base() {
            Field = EnumExtensions.ConditionFieldFromString( conditionDesc );
        }

        public void UpdateLabel() {
            if( Parent.FirstNode == this ) {
                Text = Field.GetLongString() + ' ' + Op.GetSymbol() + ' ' + Value;
            } else {
                Text = (Parent as GroupNode).Op.GetShortString() + ' ' + Field.GetLongString() + ' ' + Op.GetSymbol() + ' ' + Value;
            }
        }
    }
}
