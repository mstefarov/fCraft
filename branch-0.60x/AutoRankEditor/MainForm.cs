using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace AutoRankEditor {
    public partial class MainForm : Form {

        public MainForm() {
            InitializeComponent();
            treeData.Nodes.Add( new ActionNode() );

            cmAddCondition.ItemClicked += cmAddCondition_ItemClicked;
            foreach( ToolStripMenuItem item in cmAddCondition.Items ) {
                if( item.DropDownItems.Count > 0 ) {
                    item.DropDownItemClicked += cmAddCondition_ItemClicked;
                }
            }

            foreach( ActionType actionType in Enum.GetValues( typeof( ActionType ) ) ) {
                cActionType.Items.Add( actionType );
            }

            foreach( string fieldName in EnumExtensions.ConditionFieldNames.Values ) {
                cConditionField.Items.Add( fieldName );
            }

            foreach( GroupNodeType groupOp in Enum.GetValues( typeof( GroupNodeType ) ) ) {
                cGroupOp.Items.Add( groupOp );
            }
        }

        private void treeData_AfterSelect( object sender, TreeViewEventArgs e ) {
            if( e.Node is GroupNode ) {
                bAddGroup.Enabled = true;
                bAddCondition.Enabled = true;
                gEditCondition.Visible = false;
                if( e.Node is ActionNode ) {
                    gEditGroup.Visible = false;
                    gEditAction.Visible = true;
                    cActionType.SelectedIndex = (int)(e.Node as ActionNode).Action;
                    tFromRank.Text = (e.Node as ActionNode).FromRank;
                    tToRank.Text = (e.Node as ActionNode).ToRank;
                } else {
                    gEditGroup.Visible = true;
                    gEditAction.Visible = false;
                    cGroupOp.SelectedIndex = (int)(e.Node as GroupNode).Op;
                }
            } else if( e.Node is ConditionNode ) {
                bAddGroup.Enabled = false;
                bAddCondition.Enabled = false;
                gEditGroup.Visible = false;
                gEditAction.Visible = false;
                gEditCondition.Visible = true;
                cConditionField.SelectedItem = (e.Node as ConditionNode).Field.GetLongString();
                cConditionOp.SelectedIndex = (int)(e.Node as ConditionNode).Op;
                nConditionValue.Value = (e.Node as ConditionNode).Value;
            }
        }


        private void bAddGroup_Click( object sender, EventArgs e ) {
            cmAddGroup.Show( bAddGroup, bAddGroup.Width, 0 );
            cmAddGroup.Items[0].Select();
        }


        private void bAddCondition_Click( object sender, EventArgs e ) {
            GroupNode node = treeData.SelectedNode as GroupNode;
            foreach( ToolStripMenuItem item in cmAddCondition.Items ) {
                if( item.DropDownItems.Count > 0 ) {
                    bool anySubItemsAvailable = false;
                    foreach( ToolStripMenuItem subItem in item.DropDownItems ) {
                        if( CheckIfNodeExists( treeData.SelectedNode, item.Text + " " + subItem.Text ) ) {
                            subItem.Available = false;
                        } else {
                            subItem.Available = true;
                            anySubItemsAvailable = true;
                        }
                    }
                    item.Available = anySubItemsAvailable;
                } else {
                    item.Available = !CheckIfNodeExists( treeData.SelectedNode, item.Text );
                }
            }
            cmAddCondition.Show( bAddCondition, bAddCondition.Width, 0 );
            cmAddCondition.Items[0].Select();
        }

        bool CheckIfNodeExists( TreeNode node, string text ) {
            foreach( TreeNode subNode in node.Nodes ) {
                if( (subNode is ConditionNode) && (subNode as ConditionNode).Field.GetLongString() == text ) {
                    return true;
                }
            }
            return false;
        }

        private void cmAddGroup_ItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            ToolStripMenuItem item = e.ClickedItem as ToolStripMenuItem;
            if( item.DropDownItems.Count > 0 ) return;
            TreeNode parent = treeData.SelectedNode;
            if( parent is GroupNode ) {
                GroupNode newNode = new GroupNode( (GroupNodeType)Enum.Parse( typeof( GroupNodeType ), item.Text ) );
                parent.Nodes.Add( newNode );
                ((GroupNode)parent).UpdateLabel();
                newNode.EnsureVisible();
            }
        }

        private void cmAddCondition_ItemClicked( object sender, ToolStripItemClickedEventArgs e ) {
            ToolStripMenuItem item = e.ClickedItem as ToolStripMenuItem;
            if( item.DropDownItems.Count > 0 ) return;
            GroupNode node = treeData.SelectedNode as GroupNode;
            if( node != null ) {
                string text = item.Text;
                if( item.OwnerItem != null ) {
                    text = item.OwnerItem.Text + ' ' + text;
                }
                ConditionNode newNode = new ConditionNode( text );
                node.Nodes.Add( newNode );
                node.UpdateLabel();
                newNode.EnsureVisible();
            }
        }

        private void bDelete_Click( object sender, EventArgs e ) {
            if( treeData.SelectedNode.Nodes.Count > 0 ) {
                DialogResult result = MessageBox.Show( "Delete this group and all of its conditions?", "",
                                                       MessageBoxButtons.OKCancel );
                if( result != DialogResult.OK ) return;
            }
            TreeNode parent = treeData.SelectedNode.Parent;
            treeData.SelectedNode.Remove();
            if( parent != null ) {
                treeData.SelectedNode = parent;
                ((GroupNode)parent).UpdateLabel();
            } else {
                gEditGroup.Visible = false;
                gEditAction.Visible = false;
                gEditCondition.Visible = false;
            }
        }

        private void cGroupOp_SelectedIndexChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as GroupNode).Op = (GroupNodeType)cGroupOp.SelectedIndex;
            (treeData.SelectedNode as GroupNode).UpdateLabel();
        }

        private void cConditionOp_SelectedIndexChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ConditionNode).Op = (ComparisonOperation)cConditionOp.SelectedIndex;
            (treeData.SelectedNode as ConditionNode).UpdateLabel();
        }

        private void cConditionField_SelectedIndexChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ConditionNode).Field = EnumExtensions.ConditionFieldFromString( cConditionField.SelectedItem.ToString() );
            (treeData.SelectedNode as ConditionNode).UpdateLabel();
        }

        private void nConditionValue_ValueChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ConditionNode).Value = (int)nConditionValue.Value;
            (treeData.SelectedNode as ConditionNode).UpdateLabel();
        }

        private void bAction_Click( object sender, EventArgs e ) {
            ActionNode newNode = new ActionNode();
            treeData.Nodes.Add( newNode);
            treeData.SelectedNode = newNode;
        }

        private void cActionType_SelectedIndexChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ActionNode).Action = (ActionType)cActionType.SelectedIndex;
            (treeData.SelectedNode as ActionNode).UpdateLabel();
        }

        private void tFromRank_TextChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ActionNode).FromRank = tFromRank.Text;
            (treeData.SelectedNode as ActionNode).UpdateLabel();
        }

        private void tToRank_TextChanged( object sender, EventArgs e ) {
            (treeData.SelectedNode as ActionNode).ToRank = tToRank.Text;
            (treeData.SelectedNode as ActionNode).UpdateLabel();
        }
    }


    static class EnumExtensions {
        public static string GetSymbol( this ComparisonOperation op ) {
            switch( op ) {
                case ComparisonOperation.eq:
                    return "=";
                case ComparisonOperation.gt:
                    return ">";
                case ComparisonOperation.gte:
                    return ">=";
                case ComparisonOperation.lt:
                    return "<";
                case ComparisonOperation.lte:
                    return "<=";
                case ComparisonOperation.neq:
                    return "!=";
                default:
                    throw new ArgumentOutOfRangeException( "op" );
            }
        }

        public static Dictionary<ConditionField, string> ConditionFieldNames = new Dictionary<ConditionField, string> {
            {ConditionField.BlocksBuilt,"Blocks Built"},
            {ConditionField.BlocksChanged,"Blocks Built + Deleted"},
            {ConditionField.BlocksDeleted,"Blocks Deleted"},
            {ConditionField.BlocksDrawn,"Blocks Drawn"},
            {ConditionField.LastSeen,"Time Since Last Seen"},
            {ConditionField.MessagesWritten,"Messages Written"},
            {ConditionField.TimeSinceFirstLogin,"Time Since First Join"},
            {ConditionField.TimeSinceLastKick,"Time Since Most Recent Kick"},
            {ConditionField.TimeSinceLastLogin,"Time Since Most Recent Join"},
            {ConditionField.TimeSinceRankChange,"Time Since Most Recent Promotion/Demotion"},
            {ConditionField.TimesKicked,"Number of Times Kicked"},
            {ConditionField.TimesVisited,"Number of Visits"},
            {ConditionField.TotalTime,"Total Time Spent"},
        };

        public static string GetLongString( this ConditionField field ) {
            return ConditionFieldNames[field];
        }

        public static ConditionField ConditionFieldFromString( string text ) {
            foreach( ConditionField field in ConditionFieldNames.Keys ) {
                if( ConditionFieldNames[field] == text ) return field;
            }
            throw new ArgumentOutOfRangeException( "text" );
        }

        public static string GetShortString( this GroupNodeType op ) {
            switch( op ) {
                case GroupNodeType.AND:
                    return "and";
                case GroupNodeType.NAND:
                    return "and not";
                case GroupNodeType.NOR:
                    return "or not";
                case GroupNodeType.OR:
                    return "or";
                default:
                    throw new ArgumentOutOfRangeException( "op" );
            }
        }
    }

    enum GroupNodeType {
        AND,
        OR,
        NAND,
        NOR
    }
}