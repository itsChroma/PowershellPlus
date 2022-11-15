using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PowerPlus.Custom_Controls
{
    public class DriveAndFolderButton : RadioButton
    {
        public PathGeometry Icon
        {
            get => (PathGeometry) GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // Dependency Property as backlog store
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(PathGeometry), typeof(DriveAndFolderButton));


        public ICommand UnPinCommand
        {
            get { return (ICommand)GetValue(UnPinCommandProperty); }
            set { SetValue(UnPinCommandProperty, value);}
        }

        // Dependency Property as backlog store
        public static readonly DependencyProperty UnPinCommandProperty =
            DependencyProperty.Register("UnPinCommand", typeof(ICommand), typeof(DriveAndFolderButton));
    }
}