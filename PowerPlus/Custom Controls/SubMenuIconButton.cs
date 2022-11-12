using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace PowerPlus.Custom_Controls
{
    public class SubMenuIconButton : Button
    {
        public PathGeometry Icon
        {
            get => (PathGeometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value); 
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(PathGeometry), typeof(SubMenuIconButton));
    }
}
