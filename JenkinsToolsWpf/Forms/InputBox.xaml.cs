using System.Windows;

namespace JenkinsToolsetWpf
{
    /// <summary>
    ///     Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register("TextValue",
            typeof(string), typeof(InputBox));

        public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register("MessageText",
            typeof(string), typeof(InputBox));

        public static readonly DependencyProperty InputBoxTitleProperty = DependencyProperty.Register("InputBoxTitle",
            typeof(string), typeof(InputBox));

        public InputBox()
        {
            InitializeComponent();
        }

        public string TextValue
        {
            get { return (string) GetValue(TextValueProperty); }
            set { SetValue(TextValueProperty, value); }
        }

        public string MessageText
        {
            get { return (string) GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public string InputBoxTitle
        {
            get { return (string) GetValue(InputBoxTitleProperty); }
            set { SetValue(InputBoxTitleProperty, value); }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void _this_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.SelectAll();
            txtInput.Focus();
        }
    }
}