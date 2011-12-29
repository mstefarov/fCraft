// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    partial class PropertyGridPopup : Form {
        public PropertyGridPopup( string title, object obj ) {
            InitializeComponent();
            pgProperties.SelectedObject = obj;
            pgProperties.PropertySort = PropertySort.NoSort;
            Text = title;
        }

        public object SelectedObject {
            get { return pgProperties.SelectedObject; }
            set { pgProperties.SelectedObject = value; }
        }
    }
}
