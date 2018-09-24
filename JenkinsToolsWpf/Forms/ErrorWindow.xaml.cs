using System;
using System.Windows;

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    ///     Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        private readonly Exception _exp;

        public ErrorWindow()
        {
            InitializeComponent();
        }

        public ErrorWindow(Exception exp)
        {
            _exp = exp;
            //txtException.Text = exp.ToString();
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtException.Text = _exp.ToString();
        }
    }
}