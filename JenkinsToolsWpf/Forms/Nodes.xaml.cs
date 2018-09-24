using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using JenkinsLib;
using JenkinsToolsetWpf.Properties;
using Newtonsoft.Json;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    /// Interaction logic for Nodes.xaml
    /// </summary>
    public partial class Nodes : Window
    {
        public Nodes(string baseUrl, string username, string apiToken)
        {
            InitializeComponent();

            BaseUrl = baseUrl;
            Username = username;
            ApiToken = apiToken;
        }

        private async void Nodes_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadData();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        private async Task LoadData()
        {
            cbUrl.SelectedItem = BaseUrl;

            if (!string.IsNullOrEmpty(Settings.Default.JenkinsApiCredentials))
            {
                try
                {
                    JenkinsApiCredentials = JsonConvert
                        .DeserializeObject<ObservableConcurrentDictionary<string, JenkinsCredentialPair>>
                        (Settings.Default.JenkinsApiCredentials);
                }
                catch (Exception)
                {
                    JenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
                }
            }
            else
            {
                JenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
            }

            var jenkinsServer = new JenkinsServer
            {
                JenkinsUrl = BaseUrl,
                Username = Username,
                ApiToken = ApiToken
            };

            Computers = await jenkinsServer.GetAllComputers();
        }

        private async void cbUrl_OnDropDownClosed(object sender, EventArgs e)
        {
            try
            {
                BaseUrl = cbUrl.Text;
                await LoadData();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        #region Properties

        public static readonly DependencyProperty JenkinsApiCredentialsProperty =
            DependencyProperty.Register("JenkinsApiCredentials",
                typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>),
                typeof(Nodes));

        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get
            {
                return (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)
                    GetValue(JenkinsApiCredentialsProperty);
            }
            set { SetValue(JenkinsApiCredentialsProperty, value); }
        }

        public static readonly DependencyProperty BaseUrlProperty = DependencyProperty.Register("BaseUrl",
            typeof(string), typeof(Nodes));
        public string BaseUrl
        {
            get { return (string) GetValue(BaseUrlProperty); }
            set { SetValue(BaseUrlProperty, value); }
        }

        public static readonly DependencyProperty UsernameProperty = DependencyProperty.Register("Username",
            typeof(string), typeof(Nodes));
        public string Username
        {
            get { return (string)GetValue(UsernameProperty); }
            set { SetValue(UsernameProperty, value); }
        }

        public static readonly DependencyProperty ApiTokenProperty = DependencyProperty.Register("ApiToken",
            typeof(string), typeof(Nodes));
        public string ApiToken
        {
            get { return (string)GetValue(ApiTokenProperty); }
            set { SetValue(ApiTokenProperty, value); }
        }


        public static readonly DependencyProperty ComputersProperty = DependencyProperty.Register("Computers",
            typeof(ComputerCollection), typeof(Nodes));

        public ComputerCollection Computers
        {
            get { return (ComputerCollection) GetValue(ComputersProperty); }
            set { SetValue(ComputersProperty, value); }
        }


        #endregion

        private ListSortDirection _lastSortDirection;
        private string _lastSortBy = "displayName";
        private void ColumnHeader_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var newSortDirection = _lastSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                var view = CollectionViewSource.GetDefaultView(lstNodes.ItemsSource);

                var header = sender as GridViewColumnHeader;
                if ((header != null) && view.CanSort)
                {
                    view.SortDescriptions.Clear();
                    var sortBy = string.Empty;
                    switch (header.Name)
                    {
                        case "NodeNameHeader":
                            sortBy = "displayName";
                            break;
                        case "NodeTypeHeader":
                            sortBy = "monitorData.ArchitectureMonitor";
                            break;
                        case "MemorySizeHeader":
                            sortBy = "monitorData.SwapSpaceMonitor.totalPhysicalMemory";
                            break;
                        case "FreeMemorySizeHeader":
                            sortBy = "monitorData.SwapSpaceMonitor.availablePhysicalMemory";
                            break;
                        case "DisplayFreeDiskSizeHeader":
                            sortBy = "monitorData.DiskSpaceMonitor.size";
                            break;
                        case "RootDirHeader":
                            sortBy = "monitorData.DiskSpaceMonitor.path";
                            break;

                    }
                    var sd = new SortDescription(sortBy, newSortDirection);
                    view.SortDescriptions.Add(sd);
                    view.Refresh();

                    _lastSortDirection = newSortDirection;
                    _lastSortBy = sortBy;

                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuOpenNodes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GoToBuilds();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void GoToBuilds()
        {
            var nodes = GetSelectedNodes();
            foreach (var node in nodes)
            {
                Process.Start(new ProcessStartInfo($"{BaseUrl}computer/{node.displayName}"));
            }
        }

        private ComputerCollection GetSelectedNodes()
        {
            var selectedNodes = new ComputerCollection();

            foreach (Computer node in lstNodes.SelectedItems)
                selectedNodes.Add(node);
            return selectedNodes;
        }

        private async void Nodes_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.F5:
                        await LoadData();
                        break;
                    case Key.Escape:
                        Close();
                        break;
                }
                e.Handled = true;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }



    }
}
