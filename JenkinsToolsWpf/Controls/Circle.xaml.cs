using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JenkinsToolsetWpf.Controls
{
    /// <summary>
    ///     Interaction logic for Circle.xaml
    /// </summary>
    public partial class Circle : UserControl
    {
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register("Diameter",
            typeof(int), typeof(Circle));

        public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register("Animate", typeof(bool),
            typeof(Circle));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush),
            typeof(Circle));

        public Circle()
        {
            InitializeComponent();
        }

        public int Diameter
        {
            get { return (int) GetValue(DiameterProperty); }
            set { SetValue(DiameterProperty, value); }
        }

        public bool Animate
        {
            get { return (bool) GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush) GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
    }
}