using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window, INotifyPropertyChanged
    {

        public SearchWindow()
        {
            InitializeComponent();
        }

        #region Properties
        public static readonly DependencyProperty LeftPaneUrlProperty =
            DependencyProperty.Register("LeftPaneUrl", typeof(string), typeof(SearchWindow));
        public string LeftPaneUrl
        {
            get { return (string)GetValue(LeftPaneUrlProperty); }
            set { SetValue(LeftPaneUrlProperty, value); }
        }

        public static readonly DependencyProperty RightPaneUrlProperty =
            DependencyProperty.Register("RightPaneUrl", typeof(string), typeof(SearchWindow));

        public event PropertyChangedEventHandler PropertyChanged;

        public string RightPaneUrl
        {
            get { return (string)GetValue(RightPaneUrlProperty); }
            set { SetValue(RightPaneUrlProperty, value); }
        }

        #endregion
    }
}
