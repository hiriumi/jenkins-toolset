using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using JenkinsToolsetWpf.Annotations;
using JenkinsToolsetWpf.Properties;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    /// Interaction logic for BuildHistoryWindow.xaml
    /// </summary>
    public partial class BuildHistoryWindow : Window, INotifyPropertyChanged
    {
        private string _jenkinsUsername;
        private string _apiToken;

        public BuildHistoryWindow(JenkinsNode jenkinsNode, string jenkinsUsername, string apiToken)
        {
            JenkinsNode = jenkinsNode;
            _jenkinsUsername = jenkinsUsername;
            _apiToken = apiToken;
            InitializeComponent();
        }

        public static readonly DependencyProperty JobProperty = DependencyProperty.Register("Job", typeof(JenkinsNode),
            typeof(BuildHistoryWindow));

        public JenkinsNode JenkinsNode
        {
            get { return (JenkinsNode) GetValue(JobProperty); }
            set { SetValue(JobProperty, value); }
        }

        public static readonly DependencyProperty BuildsProperty = DependencyProperty.Register("Builds",
            typeof(BuildCollection), typeof(BuildHistoryWindow));

        public BuildCollection Builds
        {
            get { return (BuildCollection) GetValue(BuildsProperty); }
            set { SetValue(BuildsProperty, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void mnuDeleteBuilds_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var r = MessageBox.Show(this,
                    "Are you sure you would like to delete the selected build history permanently?",
                    Settings.Default.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (r == MessageBoxResult.Yes)
                {
                    var selectedBuilds = GetSelectedBuilds();

                    foreach (var selectedBuild in selectedBuilds)
                    {
                        await selectedBuild.Delete(_jenkinsUsername, _apiToken);
                        Builds.Remove(selectedBuild);
                    }

                    //var tasks = new Task[selectedBuilds.Count];

                    //for (var i = 0; i < selectedBuilds.Count; i++)
                    //{
                    //    var bulidToDelete = selectedBuilds[i];
                    //    tasks[i] = bulidToDelete.Delete(_jenkinsUsername, _apiToken); ;
                    //}

                    //if (tasks.Any(t => t != null))
                    //{
                    //    await Task.WhenAll(tasks);
                    //}
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private BuildCollection GetSelectedBuilds()
        {
            var selectedBuilds = new BuildCollection();
            foreach (Build selectedBuild in lstBuilds.SelectedItems)
                selectedBuilds.Add(selectedBuild);

            return selectedBuilds;
        }

        private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                switch (e.Key)
                {
                    case Key.Escape:
                        Close();
                        break;
                    case Key.F5:
                        await LoadBuilds();
                        break;
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void btnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (
                    MessageBox.Show(this, "Are you sure you want to close all the build history windows?",
                        Settings.Default.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                    MessageBoxResult.Yes)
                {
                    foreach (var window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(BuildHistoryWindow))
                        {
                            ((BuildHistoryWindow) window).Close();
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await LoadBuilds();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadBuilds()
        {
            var builds = await JenkinsNode.GetBuilds();
            if (builds != null)
                Builds = builds;
        }

        private void mnuOpenBuilds_Click(object sender, RoutedEventArgs e)
        {
            GoToBuilds();
        }

        private void GoToBuilds()
        {
            try
            {
                var builds = GetSelectedBuilds();
                foreach (var build in builds)
                {
                    Process.Start(new ProcessStartInfo(build.Url));
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuShowBuiltOnNode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var builds = GetSelectedBuilds();
                foreach (var build in builds)
                {
                    var uri = new Uri(build.Url);
                    var nodeUrl = $"{uri.Scheme}://{uri.Host}/computer/{build.BuiltOn}/";
                    Process.Start(new ProcessStartInfo(nodeUrl));
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private ListSortDirection _lastSortDirection;
        private string _lastSortBy = "displayName";

        private void ColumnHeader_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var newSortDirection = _lastSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                var view = CollectionViewSource.GetDefaultView(lstBuilds.ItemsSource);

                var header = sender as GridViewColumnHeader;
                if ((header != null) && view.CanSort)
                {
                    view.SortDescriptions.Clear();
                    var sortBy = string.Empty;
                    switch (header.Name)
                    {
                        case "BuildNumberHeader":
                            sortBy = "Number";
                            break;
                        case "ResultHeader":
                            sortBy = "Result";
                            break;
                        case "DurationHeader":
                            sortBy = "Duration";
                            break;
                        case "TimeStampHeader":
                            sortBy = "TimeStampDateTime";
                            break;
                        case "BuiltOnHeader":
                            sortBy = "BuiltOn";
                            break;
                        case "UrlHeader":
                            sortBy = "Url";
                            break;
                        case "UpstreamProjectHeader":
                            sortBy = "UpstreamProject";
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

        private void mnuExportToCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Ask for the path where the file is going to be stored.

                string filePath;
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Choose a file location for the CSV file.",
                    Filter = "CSV File(*.csv)|*.csv|All(*.*)|*",
                    FileName = $"{JenkinsNode.Name}_BuildHistory.csv"
                };

                if (saveFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                filePath = saveFileDialog.FileName;

                // Create a DataTable for the CSV export. The GridViewColumnHeader must have property matching Tag.
                var table = new DataTable();

                Debug.WriteLine(lstBuilds.View);

                foreach (var col in ((GridView) lstBuilds.View).Columns)
                {
                    var colHeader = col.Header as GridViewColumnHeader;
                    table.Columns.Add(new DataColumn(colHeader.Tag.ToString()));
                }

                foreach (Build item in lstBuilds.Items)
                {
                    var dr = table.NewRow();
                    foreach (DataColumn col in table.Columns)
                    {
                        var property = item.GetType().GetProperty(col.ColumnName);
                        if (property != null)
                        {
                            dr[col.ColumnName] = property.GetValue(item, null);
                        }
                    }
                    table.Rows.Add(dr);
                }

                table.AcceptChanges();

                CreateCSVFile(table, filePath);

            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CreateCSVFile(DataTable dt, string filePath)
        {
            var sw = new StreamWriter(filePath, false);
            try
            {
                var firstRecord = true;
                foreach (var col in dt.Columns)
                {
                    sw.Write(!firstRecord ? $",{col}" : $"{col}");
                    firstRecord = false;
                }
                sw.Write(sw.NewLine);

                foreach (DataRow dr in dt.Rows)
                {
                    firstRecord = true;
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (!Convert.IsDBNull(dr[col]))
                        {
                            sw.Write(firstRecord ? dr[col].ToString() : $",{dr[col]}");
                        }
                        else
                        {
                            sw.Write(firstRecord ? string.Empty : $",{string.Empty}");
                        }
                        firstRecord = false;
                    }
                    sw.Write(sw.NewLine);
                }

                sw.Close();
            }
            catch
            {
                sw?.Close();
                throw;
            }


        }

        private void createCsvFile(DataTable dt, string filePath)
        {
            var sw = new StreamWriter(filePath, false);

        }
    }
}