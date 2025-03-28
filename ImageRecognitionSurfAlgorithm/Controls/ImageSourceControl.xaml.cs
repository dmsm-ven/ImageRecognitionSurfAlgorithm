using System.Windows;
using System.Windows.Controls;

namespace ImageRecognitionSurfAlgorithm.Controls
{
    /// <summary>
    /// Interaction logic for ImageSourceControl.xaml
    /// </summary>
    public partial class ImageSourceControl : UserControl
    {
        public string CurrentImagePath
        {
            get { return (string)GetValue(CurrentImagePathProperty); }
            set => SetValue(CurrentImagePathProperty, value);
        }

        // Using a DependencyProperty as the backing store for CurrentImagePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentImagePathProperty =
            DependencyProperty.Register("CurrentImagePath", typeof(string), typeof(ImageSourceControl), new PropertyMetadata("missing"));

        public ImageSourceControl()
        {
            InitializeComponent();
        }
    }
}
