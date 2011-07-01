using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoRankEditor {
    class ActionNode : GroupNode {
        public string FromRank { get; set; }
        public string ToRank { get; set; }
        public ActionType Action { get; set; }

        public ActionNode()
            : base( GroupNodeType.OR ) {
            FromRank = "?";
            ToRank = "?";
            UpdateLabel();
        }

        public override void UpdateLabel() {
            Text = String.Format( "Action ({0} {1} to {2})",
                                  Action, FromRank, ToRank );
            foreach( TreeNode node in Nodes ) {
                if( node is GroupNode ) {
                    (node as GroupNode).UpdateLabel();
                } else if( node is ConditionNode ) {
                    (node as ConditionNode).UpdateLabel();
                }
            }
        }
    }
}
