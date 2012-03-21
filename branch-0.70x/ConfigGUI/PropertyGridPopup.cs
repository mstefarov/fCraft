// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
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
