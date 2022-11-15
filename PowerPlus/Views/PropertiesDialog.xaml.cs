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
    /// Interaction logic for PropertiesDialog.xaml
    /// </summary>
    public partial class PropertiesDialog : Window
    {
        public PropertiesDialog()
        {
            InitializeComponent();
        }

        public PathGeometry Icon
        {
            get => (PathGeometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("FileIcon", typeof(PathGeometry), typeof(PropertiesDialog));


        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(PropertiesDialog));

        public string FileExtension
        {
            get { return (string)GetValue(FileExtensionProperty); }
            set { SetValue(FileExtensionProperty, value); }
        }

        public static readonly DependencyProperty FileExtensionProperty =
            DependencyProperty.Register("FileExtension", typeof(string), typeof(PropertiesDialog));

        public string FullPath
        {
            get { return (string)GetValue(FullPathProperty); }
            set { SetValue(FullPathProperty, value); }
        }

        public static readonly DependencyProperty FullPathProperty =
            DependencyProperty.Register("FullPath", typeof(string), typeof(PropertiesDialog));

        public string FileSize
        {
            get { return (string)GetValue(FileSizeProperty); }
            set { SetValue(FileSizeProperty, value); }
        }

        public static readonly DependencyProperty FileSizeProperty =
            DependencyProperty.Register("FileSize", typeof(string), typeof(PropertiesDialog));

        public string CreatedOn
        {
            get { return (string)GetValue(CrwatedOnProperty); }
            set { SetValue(CrwatedOnProperty, value); }
        }

        public static readonly DependencyProperty CrwatedOnProperty =
            DependencyProperty.Register("CreatedOn", typeof(string), typeof(PropertiesDialog));

        public string ModifiedOn
        {
            get { return (string)GetValue(ModifiedOnProperty); }
            set { SetValue(ModifiedOnProperty, value); }
        }

        public static readonly DependencyProperty ModifiedOnProperty =
            DependencyProperty.Register("ModifiedOn", typeof(string), typeof(PropertiesDialog));

        public string AccessedOn
        {
            get { return (string)GetValue(AccessedOnProperty); }
            set { SetValue(AccessedOnProperty, value); }
        }

        public static readonly DependencyProperty AccessedOnProperty =
            DependencyProperty.Register("AccessedOn", typeof(string), typeof(PropertiesDialog));


        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PropertiesDialog));

        public bool IsHidden
        {
            get { return (bool)GetValue(IsHiddenProperty); }
            set { SetValue(IsHiddenProperty, value); }
        }

        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register("IsHidden", typeof(bool), typeof(PropertiesDialog));

    }
}
