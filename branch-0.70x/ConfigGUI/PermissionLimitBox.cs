// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    sealed partial class PermissionLimitBox : UserControl {

        public Permission Permission { get; private set; }

        public string FirstItem { get; private set; }

        public Rank Rank { get; private set; }

        public PermissionLimitBox( string labelText, Permission permission, string firstItem ) {
            InitializeComponent();

            label.Text = labelText;
            label.Left = (comboBox.Left - comboBox.Margin.Left) - (label.Width + label.Margin.Right);

            Permission = permission;
            FirstItem = firstItem;
            RebuildList();

            comboBox.SelectedIndexChanged += OnPermissionLimitChanged;
        }


        void OnPermissionLimitChanged( object sender, EventArgs args ) {
            if( Rank == null ) return;
            Rank rankLimit = MainForm.FindRankByIndex( comboBox.SelectedIndex );
            if( rankLimit == null ) {
                Rank.ResetLimit( Permission );
            } else {
                Rank.SetLimit( Permission, rankLimit );
            }
        }


        public void RebuildList() {
            comboBox.Items.Clear();
            comboBox.Items.Add( FirstItem );
            foreach( Rank rank in RankManager.Ranks ) {
                comboBox.Items.Add( MainForm.ToComboBoxOption( rank ) );
            }
        }


        public void SelectRank( Rank rank ) {
            Rank = rank;
            if( rank == null ) {
                comboBox.SelectedIndex = -1;
                Visible = false;
            } else {
                comboBox.SelectedIndex = GetLimitIndex( rank, Permission );
                Visible = rank.Can( Permission );
            }
        }


        int GetLimitIndex( Rank rank, Permission permission ) {
            if( rank.HasLimitSet( permission ) ) {
                return 0;
            } else {
                return rank.GetLimit( permission ).Index + 1;
            }
        }


        public void PermissionToggled( bool isOn ) {
            Visible = isOn;
        }
    }
}