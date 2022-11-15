using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PowerPlus.Views
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        public RenameDialog()
        {
            InitializeComponent();
        }

        public string OldFolderName
        {
            get { return (string)GetValue(OldFolderNameProperty); }
            set { SetValue(OldFolderNameProperty, value); }
        }

        public static readonly DependencyProperty OldFolderNameProperty =
            DependencyProperty.Register("OldFolderName", typeof(string), typeof(RenameDialog));
    }
}