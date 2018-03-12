using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CardReaderGui
{
    /// <summary>
    /// Interaction logic for PropertyWindow.xaml
    /// </summary>
    public partial class PropertyWindow : Window
    {
        public static readonly DependencyProperty PropertiesProperty;

        public IList<object> Properties
        {
            get { return (IList<object>)GetValue(PropertiesProperty); }
            set { SetValue(PropertiesProperty, value); }
        }

        static PropertyWindow()
        {
            PropertiesProperty = DependencyProperty.Register("Properties", typeof(IList<object>), typeof(PropertyWindow), new PropertyMetadata(new List<object>()));
        }

        public PropertyWindow()
        {
            InitializeComponent();
        }
    }
}
