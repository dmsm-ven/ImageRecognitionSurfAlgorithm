using System.Windows;
using System.Windows.Controls;

namespace ImageRecognitionSurfAlgorithm.Controls
{
    /// <summary>
    /// Interaction logic for ImageSourceControl.xaml
    /// </summary>
    public partial class ImageSourceControl : UserControl
    {

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImagePath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register("ImagePath", typeof(string), typeof(ImageSourceControl), new PropertyMetadata(0));


        public ImageSourceControl()
        {
            InitializeComponent();
        }
    }
}
