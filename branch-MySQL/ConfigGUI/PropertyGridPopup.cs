using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace fCraft.ConfigGUI {
    public partial class PropertyGridPopup : Form {
        public PropertyGridPopup( string title, object obj ) {
            InitializeComponent();
            pgProperties.SelectedObject = obj;
            pgProperties.PropertySort = PropertySort.NoSort;
        }

        public object SelectedObject {
            get { return pgProperties.SelectedObject; }
            set { pgProperties.SelectedObject = value; }
        }
    }
}
