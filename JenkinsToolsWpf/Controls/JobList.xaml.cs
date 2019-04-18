using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JenkinsLib;
using JenkinsToolsetWpf.Forms;
using JenkinsToolsetWpf.Properties;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace JenkinsToolsetWpf.Controls
{
    /// <summary>
    ///     Interaction logic for JobList.xaml
    /// </summary>
    public partial class JobList : UserControl, INotifyPropertyChanged
    {
        private bool _ascendingOrder = true;
        private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;
        private string _lastSortBy = "Name";
        private bool _preventDragAndDrop;
        private Point _startPoint;

        public JobList()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var item = e.Source as System.Windows.Controls.ListViewItem;
                var jenkinsNode = item?.DataContext as JenkinsNode;

                if (jenkinsNode != null && jenkinsNode.JenkinsNodeType == JenkinsNodeType.Folder)
                {
                    NavigateToUrl(jenkinsNode.Url);
                }
                else
                {
                    EditJobsHandler(e);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }


        public JenkinsNodeCollection GetSelectedJobs(bool includeFolder = false)
        {
            return GetSelectedJobs(JobState.New | JobState.UpdatedLocally | JobState.Orginal, includeFolder);
        }

        public JenkinsNodeCollection GetSelectedJobs(JobState state, bool includeFolder = false)
        {
            var selectedJobs = new JenkinsNodeCollection();
            foreach (JenkinsNode selectedJob in lstJenkinsJobs.SelectedItems)
            {
                if ((state & selectedJob.State) != 0)
                {
                    selectedJobs.Add(selectedJob);
                }
            }

            return selectedJobs;
        }

        private void _this_Loaded(object sender, RoutedEventArgs e)
        {
            if (Pane == JenkinsPane.Left)
            {
                mnuCopyJobs.Header = "Copy Selected Job(s) to the Right Pane";
                mnuCopyJobs.Icon = new Image
                {
                    Source =
                        new BitmapImage(
                            new Uri(
                                @"pack://application:,,,/JenkinsToolsetWpf;component/Images/arrow_Forward_color_32xLG.png"))
                };

                if (Settings.Default.PreserveFilterText)
                {
                    FilterText = Settings.Default.LeftFilterText;
                }
            }
            else
            {
                mnuCopyJobs.Header = "Copy Selected Job(s) to the Left Pane";
                mnuCopyJobs.Icon = new Image
                {
                    Source =
                        new BitmapImage(
                            new Uri(
                                @"pack://application:,,,/JenkinsToolsetWpf;component/Images/arrow_back_color_32xLG.png"))
                };

                if (Settings.Default.PreserveFilterText)
                {
                    FilterText = Settings.Default.RightFilterText;
                }
            }
        }

        public string JenkinsBaseUrl
        {
            get
            {
                var uri = new Uri(JenkinsUrl);
                string baseUrl;
                if (uri.Port == 80 || uri.Port == 443)
                {
                    baseUrl = $"{uri.Scheme}://{uri.DnsSafeHost}/";
                }
                else
                {
                    baseUrl = $"{uri.Scheme}://{uri.DnsSafeHost}:{uri.Port}/";
                }
                return baseUrl;
            }
        }

        private void mnuGoToJob_Click(object sender, RoutedEventArgs e)
        {
            GoToJobs();
        }

        private void GoToJobs()
        {
            try
            {
                var jobs = GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);
                foreach (var job in jobs)
                {
                    if (Settings.Default.DefaultToConfigurePageWhenOpeningUrl)
                    {
                        Process.Start(new ProcessStartInfo(job.ConfigureUrl));
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo(job.Url));
                    }
                    
                }
                    
                //e.Handled = true;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            DeselectAll();
        }

        private void DeselectAll()
        {
            try
            {
                lstJenkinsJobs.UnselectAll();

                foreach (var job in JenkinsNodes.Where(j => j.Selected))
                    job.Selected = false;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void btnGetApiToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtUsername.Text))
                {
                    RaiseShowMessageEvent("Please enter username to proceed.", MessageType.Error);
                    txtUsername.Focus();
                    return;
                }

                var jenkinsConfigureUri = new Uri(new Uri(JenkinsUrl), $"/user/{JenkinsUsername}/configure");
                Process.Start(new ProcessStartInfo(jenkinsConfigureUri.AbsoluteUri));
                e.Handled = true;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        public void MarkJobDeleted(JenkinsNode deletedJenkinsNode)
        {
            JenkinsNodes.Remove(deletedJenkinsNode);
        }

        public bool JobExists(string jobName)
        {
            return JenkinsNodes.Any(job => job.Name == jobName);
        }

        public void ScrollJobIntoView(JenkinsNode jenkinsNode)
        {
            lstJenkinsJobs.ScrollIntoView(jenkinsNode);
        }

        public JenkinsNode GetJob(string jobName)
        {
            return JenkinsNodes.FirstOrDefault(job => jobName == job.Name);
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        public void AddNewJob(JenkinsNode newJenkinsNode)
        {
            JenkinsNodes.Insert(0, newJenkinsNode);
            lstJenkinsJobs.ScrollIntoView(newJenkinsNode);
        }

        private void mnuCopyJobNames_Click(object sender, RoutedEventArgs e)
        {
            CopyJobNames();
        }

        private void CopyJobNames()
        {
            var selectedJobs = GetSelectedJobs();
            var jobNames = new StringBuilder();
            if (selectedJobs.Count > 0)
            {
                foreach (var job in selectedJobs)
                    jobNames.AppendLine(job.Name);

                Clipboard.SetText(jobNames.ToString());
            }
        }

        private void CopyJobUrls()
        {
            var selectedJobs = GetSelectedJobs();
            var jobUrls = new StringBuilder();
            if (selectedJobs.Count > 0)
            {
                foreach (var job in selectedJobs)
                    jobUrls.AppendLine(job.Url);

                Clipboard.SetText(jobUrls.ToString());
            }
        }

        private void mnuCopyJobUrls_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CopyJobUrls();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void chkLockThisPane_Click(object sender, RoutedEventArgs e)
        {
            var chkBox = sender as CheckBox;
            lstJenkinsJobs.IsEnabled = !(bool) chkBox.IsChecked;
        }

        private void chkShowSelectedJobsOnly_Click(object sender, RoutedEventArgs e)
        {
            var selectedJobs = GetSelectedJobs();
            Debug.WriteLine(selectedJobs.Count);

            var chkBox = sender as CheckBox;
            if (chkBox != null)
                if (chkBox.IsChecked != null && (bool) chkBox.IsChecked)
                    lstJenkinsJobs.Items.Filter = (object o) =>
                    {
                        var job = o as JenkinsNode;
                        Debug.WriteLine($"{job.Name}:{job.Selected}");
                        return job.Selected;
                    };
                else
                    lstJenkinsJobs.Items.Filter = jobCollection_Filter;
        }

        private void cboUrl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(cboUrl.Text.Trim()))
                {
                    RaiseShowMessageEvent("Please enter a Jenkins root URL.", MessageType.Error);
                    cboUrl.Focus();
                    return;
                }

                if (!cboUrl.Text.StartsWith("https://") && !cboUrl.Text.StartsWith("http://"))
                {
                    cboUrl.Text = cboUrl.Text.Insert(0, "http://");
                }
                    
                if (!cboUrl.Text.EndsWith("/"))
                    cboUrl.Text += "/";

                if (UrlHistory != null && !UrlHistory.Contains(cboUrl.Text))
                {
                    txtUsername.Clear();
                    txtApiToken.Clear();
                    UrlHistory.Add(cboUrl.Text);
                    JenkinsUrl = cboUrl.Text;
                }

                if (string.IsNullOrEmpty(txtUsername.Text.Trim()))
                {
                    RaiseShowMessageEvent("Please enter username.", MessageType.Error);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtApiToken.Text.Trim()))
                {
                    RaiseShowMessageEvent("Please enter API Token.", MessageType.Error);
                    txtApiToken.Focus();
                    return;
                }
                
                NavigateToUrl(cboUrl.Text);
            }
        }

        private void NavigateToUrl(string url, bool backButtonUsed = false)
        {
            if (!backButtonUsed)
            {
                // Make sure to put the previous URL at the bottom of the list
                if (UrlHistory != null && UrlHistory.Contains(JenkinsUrl))
                {
                    int index = UrlHistory.IndexOf(JenkinsUrl);
                    UrlHistory.Move(index, UrlHistory.Count - 1);
                }
            }

            JenkinsUrl = url;

            OnPropertyChanged(nameof(JenkinsUrl));
            OnPropertyChanged(nameof(UrlHistory));

            var args = new RoutedEventArgs { RoutedEvent = UrlRefreshEvent };
            RaiseEvent(args);
        }

        public event RoutedEventHandler JenkinsApiCredentialChanged
        {
            add { AddHandler(JenkinsApiCredentialChangedEvent, value); }
            remove { RemoveHandler(JenkinsApiCredentialChangedEvent, value); }
        }

        private void txtApiToken_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            JenkinsApiToken = txtApiToken.Text;
            JenkinsNodes?.PropagateCredential(JenkinsUsername, JenkinsApiToken);
            var args = new RoutedEventArgs {RoutedEvent = JenkinsApiCredentialChangedEvent};
            RaiseEvent(args);
        }

        private void txtUserName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            JenkinsUsername = txtUsername.Text;
            var args = new RoutedEventArgs {RoutedEvent = JenkinsApiCredentialChangedEvent};
            RaiseEvent(args);
        }

        private void cboUrl_OnDropDownClosed(object sender, EventArgs e)
        {
            try
            {
                JenkinsUrl = (string) cboUrl.SelectedItem;
                NavigateToUrl(JenkinsUrl);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuFont_Click(object sender, RoutedEventArgs e)
        {
            var fontDialogue = new FontDialog();
            if (fontDialogue.ShowDialog() == DialogResult.OK)
            {
                lstJenkinsJobs.FontFamily = new FontFamily(fontDialogue.Font.Name);
                lstJenkinsJobs.FontSize = fontDialogue.Font.Size*96.0/72.0;
                lstJenkinsJobs.FontWeight = fontDialogue.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
                lstJenkinsJobs.FontStyle = fontDialogue.Font.Italic ? FontStyles.Italic : FontStyles.Normal;
            }

            // font has been changed, so save the settings.
            // Binding will take care of it.
        }

        private void JobList_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Debug.WriteLine(e.Key);
                switch (e.Key)
                {
                    case Key.A:
                        lstJenkinsJobs.SelectAll();
                        break;
                    case Key.B:
                        RaiseBuildJobEvent();
                        break;
                    case Key.C:
                        CopyJobNames();
                        break;
                    case Key.D:
                        RaiseDuplicateJobEvent();
                        break;
                    case Key.E:
                        RaiseEditJobsEvent();
                        break;
                    case Key.F:
                        txtFilter.SelectAll();
                        txtFilter.Focus();
                        break;
                    case Key.L:
                        RaiseLoadJobsEvent();
                        break;
                    case Key.N:
                        RaiseCreateFolderEvent();
                        break;
                    case Key.O:
                        GoToJobs();
                        break;
                    case Key.P:
                        RaisePushJobEvent();
                        break;
                    case Key.S:
                        RaiseSaveConfigEvent();
                        break;
                    case Key.Z:
                        RaiseUndoJobChangeEvent();
                        break;
                }
            }

            // No modifying key
            switch (e.Key)
            {
                case Key.F2:
                    RaiseRenameEvent();
                    break;
                case Key.Delete:
                    RaiseDeleteJobsEvent();
                    break;
            }
        }

        private void lstJenkinsJobs_OnDragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("jobsToCopy") || (sender == e.Source))
                e.Effects = DragDropEffects.None;
        }

        private void lstJenkinsJobs_OnMouseEnterScrollbar(object sender, MouseEventArgs e)
        {
            _preventDragAndDrop = true;
        }

        private void OnListViewItemMouseEnter(object sender, MouseEventArgs e)
        {
            _preventDragAndDrop = false;
        }

        private void lstJenkinsJobs_OnMouseEnterGridViewColumnHeader(object sender, MouseEventArgs e)
        {
            _preventDragAndDrop = true;
        }

        private void lstJenkinsJobs_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if ((e.LeftButton == MouseButtonState.Pressed) && !_preventDragAndDrop)
            {
                var position = e.GetPosition(null);
                // The following if condition prevents drag and drop mode to kick in 
                // when dragging the scroll bar on the right. Why drag and drop mode kick in when dragging the scroll bar...
                // The framework should take care of it already...
                if ((Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance) ||
                    (Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    var listView = sender as ListView;
                    if (listView?.SelectedItem != null)
                    {
                        var jobsToCopy = new DataObject("jobsToCopy",
                            GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally));

                        jobsToCopy.SetData("dragSource", this);

                        DragDrop.DoDragDrop(listView, jobsToCopy, DragDropEffects.Copy);
                    }
                }
            }
        }

        private void lstJenkinsJobs_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void lstJenkinsJobs_OnDrop(object sender, DragEventArgs e)
        {
            var sourceJobList = e.Data.GetData("dragSource") as JobList;
            if (sourceJobList?.Pane == Pane)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var args = new JobActionEventArgs
                {
                    RoutedEvent = LoadJobsByDragAndDropEvent,
                    JobFiles = e.Data.GetData(DataFormats.FileDrop, true) as string[]
                };
                RaiseEvent(args);
            }
            else
            {
                var selectedJobs = sourceJobList?.GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);
                RaiseCopyJobsEvent(true, selectedJobs);
            }
        }


        private void ColumnHeader_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var newSortDirection = _lastSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            var view = CollectionViewSource.GetDefaultView(lstJenkinsJobs.ItemsSource);

            var header = sender as GridViewColumnHeader;
            if ((header != null) && view.CanSort)
            {
                view.SortDescriptions.Clear();
                var sortBy = string.Empty;
                switch (header.Name)
                {
                    case "JobNameHeader":
                        sortBy = "Name";
                        break;
                    case "UrlHeader":
                        sortBy = "Url";
                        break;
                    case "StatusIcon":
                        sortBy = "ImageFileName";
                        break;
                    case "NodeTypeIcon":
                        sortBy = "NodeTypeImageFileName";
                        break;
                }
                var sd = new SortDescription(sortBy, newSortDirection);
                view.SortDescriptions.Add(sd);
                view.Refresh();

                _lastSortDirection = newSortDirection;
                _lastSortBy = sortBy;
            }
        }

        private void chkRegexOn_Checked(object sender, RoutedEventArgs e)
        {
            RaiseShowMessageEvent("Regex is enabled.\r\nPlease hit Enter key for the items to be filtered.",
                MessageType.Information);
            txtFilter.SelectAll();
            txtFilter.Focus();
        }

        private void chkRegexOn_Unchecked(object sender, RoutedEventArgs e)
        {
            lstJenkinsJobs.Items.Filter = jobCollection_Filter;
            RaiseShowMessageEvent("Regex is disabled.\r\nWildcard (*) is available to filter jobs.",
                MessageType.Information);
            txtFilter.SelectAll();
            txtFilter.Focus();
        }

        #region Event Raisers

        public delegate void JenkinsJobsEventHandler(object sender, JobActionEventArgs e);

        public delegate void ShowMessageEventHandler(object sender, MessageEventArgs e);

        public static readonly RoutedEvent EditJobsEvent = EventManager.RegisterRoutedEvent("EditJobsEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent PushJobEvent = EventManager.RegisterRoutedEvent("PushJobEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent DeleteJobEvent = EventManager.RegisterRoutedEvent("DeleteJobEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent CompareJobsEvent = EventManager.RegisterRoutedEvent("CompareJobsEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(JobList));

        public static readonly RoutedEvent UndoJobChangeEvent = EventManager.RegisterRoutedEvent("UndoJobChangeEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent CopyJobsEvent = EventManager.RegisterRoutedEvent("CopyJobsEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent DuplicateJobEvent = EventManager.RegisterRoutedEvent("DuplicateJobEvent",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent LoadJobsEvent = EventManager.RegisterRoutedEvent("LoadJobsEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(JobList));

        public static readonly RoutedEvent SaveJobConfigEvent = EventManager.RegisterRoutedEvent("SaveJobConfig",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent BuildJobEvent = EventManager.RegisterRoutedEvent("BuildJob",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent UrlRefreshEvent = EventManager.RegisterRoutedEvent("UrlRefresh",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(JobList));

        public static readonly RoutedEvent JenkinsApiCredentialChangedEvent =
            EventManager.RegisterRoutedEvent("JenkinsApiCredChange",
                RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(JobList));

        public static readonly RoutedEvent FontChangeEvent = EventManager.RegisterRoutedEvent("FontChange",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(JobList));

        public static readonly RoutedEvent MonitorJobsEvent = EventManager.RegisterRoutedEvent("MonotorJobs",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent EnableJobsEvent = EventManager.RegisterRoutedEvent("EnableJobs",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent DisableJobsEvent = EventManager.RegisterRoutedEvent("DisableJobs",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent CopyJobsByDragAndDropEvent =
            EventManager.RegisterRoutedEvent("CopyJobsByDragAndDrop",
                RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent LoadJobsByDragAndDropEvent =
            EventManager.RegisterRoutedEvent("LoadJobsByDragAndDrop",
                RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent ShowMessageEvent = EventManager.RegisterRoutedEvent("ShowMessage",
            RoutingStrategy.Bubble, typeof(ShowMessageEventHandler), typeof(JobList));

        public static readonly RoutedEvent RenameJobEvent = EventManager.RegisterRoutedEvent("RenameJob",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent RunTestBuildEvent = EventManager.RegisterRoutedEvent("RunTestBuild",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent BuildHistoryEvent = EventManager.RegisterRoutedEvent("BuildHistory",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public static readonly RoutedEvent CreateFolderEvent = EventManager.RegisterRoutedEvent("CreateFolder",
            RoutingStrategy.Bubble, typeof(ShowMessageEventHandler), typeof(JobList));

        public static readonly RoutedEvent XPathReplaceEvent = EventManager.RegisterRoutedEvent("XPathReplace",
            RoutingStrategy.Bubble, typeof(JenkinsJobsEventHandler), typeof(JobList));

        public event JenkinsJobsEventHandler XPathReplace
        {
            add { AddHandler(XPathReplaceEvent, value); }
            remove { RemoveHandler(XPathReplaceEvent, value); }
        }

        public event ShowMessageEventHandler CreateFolder
        {
            add { AddHandler(CreateFolderEvent, value); }
            remove { RemoveHandler(CreateFolderEvent, value); }
        }

        public event JenkinsJobsEventHandler BuildHistory
        {
            add { AddHandler(BuildHistoryEvent, value); }
            remove { RemoveHandler(BuildHistoryEvent, value); }
        }

        public event JenkinsJobsEventHandler RunTestBuild
        {
            add { AddHandler(RunTestBuildEvent, value); }
            remove { RemoveHandler(RunTestBuildEvent, value); }
        }

        public event JenkinsJobsEventHandler RenameJob
        {
            add { AddHandler(RenameJobEvent, value); }
            remove { RemoveHandler(RenameJobEvent, value); }
        }

        public event ShowMessageEventHandler ShowMessage
        {
            add { AddHandler(EditJobsEvent, value); }
            remove { RemoveHandler(EditJobsEvent, value); }
        }

        public event JenkinsJobsEventHandler EditJobs
        {
            add { AddHandler(EditJobsEvent, value); }
            remove { RemoveHandler(EditJobsEvent, value); }
        }

        public event JenkinsJobsEventHandler MonitorJobs
        {
            add { AddHandler(MonitorJobsEvent, value); }
            remove { RemoveHandler(MonitorJobsEvent, value); }
        }

        public event RoutedEventHandler UrlRefresh
        {
            add { AddHandler(UrlRefreshEvent, value); }
            remove { RemoveHandler(UrlRefreshEvent, value); }
        }

        public event JenkinsJobsEventHandler SaveJobConfig
        {
            add { AddHandler(SaveJobConfigEvent, value); }
            remove { RemoveHandler(SaveJobConfigEvent, value); }
        }

        public event JenkinsJobsEventHandler EnableJobs
        {
            add { AddHandler(EnableJobsEvent, value); }
            remove { RemoveHandler(EnableJobsEvent, value); }
        }

        public event JenkinsJobsEventHandler DisableJobs
        {
            add { AddHandler(DisableJobsEvent, value); }
            remove { RemoveHandler(DisableJobsEvent, value); }
        }

        public event JenkinsJobsEventHandler CopyJobsByDragAndDrop
        {
            add { AddHandler(CopyJobsByDragAndDropEvent, value); }
            remove { RemoveHandler(CopyJobsByDragAndDropEvent, value); }
        }

        public event JenkinsJobsEventHandler LoadJobsByDragAndDrop
        {
            add { AddHandler(LoadJobsByDragAndDropEvent, value); }
            remove { RemoveHandler(LoadJobsByDragAndDropEvent, value); }
        }

        private void mnuBuildHistory_Click(object sender, RoutedEventArgs e)
        {
            var jobs = GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);

            if (jobs.Count == 0)
            {
                MessageBox.Show("Please select at least one published Jenkins job.", Settings.Default.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var args = new JobActionEventArgs
            {
                RoutedEvent = BuildHistoryEvent,
                JenkinsNodes = jobs
            };

            RaiseEvent(args);
        }

        private void mnuEnableJobs_Click(object sender, RoutedEventArgs e)
        {
            var jobs = GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);

            if (jobs.Count == 0)
            {
                MessageBox.Show("Please select at least one published Jenkins job.", Settings.Default.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var args = new JobActionEventArgs
            {
                RoutedEvent = EnableJobsEvent,
                JenkinsNodes = jobs
            };

            RaiseEvent(args);
        }

        private void mnuXPathReplace_Click(object sender, RoutedEventArgs e)
        {
            var jobs = GetSelectedJobs();
            if (jobs.Count == 0)
            {
                RaiseShowMessageEvent("Please select at least one Jenkins job.", MessageType.Warning);
                return;
            }

            var args = new JobActionEventArgs
            {
                RoutedEvent = XPathReplaceEvent,
                JenkinsNodes = jobs
            };

            RaiseEvent(args);
        }

        private void RaiseCreateFolderEvent()
        {
            var folderNameInput = new InputBox
            {
                Title = "Create a New Jenkins Folder",
                MessageText = "Please enter a new folder name"
            };


            bool? result = folderNameInput.ShowDialog();
            if (result != null && (bool)result)
            {
                if (string.IsNullOrEmpty(folderNameInput.TextValue))
                {
                    var msgEventArgs = new MessageEventArgs
                    {
                        RoutedEvent = ShowMessageEvent,
                        Message = "Please enter a new folder name",
                        MessageType = MessageType.Error
                    };
                    RaiseEvent(msgEventArgs);
                    return;
                }

                if (JobExists(folderNameInput.TextValue))
                {
                    var msgEventArgs = new MessageEventArgs
                    {
                        RoutedEvent = ShowMessageEvent,
                        Message = $"Folder \"{folderNameInput.TextValue}\" already exists",
                        MessageType = MessageType.Error
                    };
                    RaiseEvent(msgEventArgs);
                    return;
                }

                var folderName = folderNameInput.TextValue.Trim();
                var args = new MessageEventArgs
                {
                    RoutedEvent = CreateFolderEvent,
                    Message = folderName,
                    MessageType = MessageType.Information
                };

                RaiseEvent(args);
            }  
        }

        private void mnuRename_Click(object sender, RoutedEventArgs e)
        {
            RaiseRenameEvent();
        }

        private void RaiseRenameEvent()
        {
            var jobs = GetSelectedJobs(JobState.Orginal);
            if (jobs.Count == 0)
            {
                RaiseShowMessageEvent("Please select at least one published Jenkins job.", MessageType.Warning);
                return;
            }

            var args = new JobActionEventArgs
            {
                RoutedEvent = RenameJobEvent,
                JenkinsNodes = jobs
            };

            RaiseEvent(args);
        }

        private void mnuDisableJobs_Click(object sender, RoutedEventArgs e)
        {
            var jobs = GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);

            if (jobs.Count == 0)
            {
                RaiseShowMessageEvent("Please select at least one published Jenkins job.", MessageType.Warning);
                return;
            }

            var args = new JobActionEventArgs
            {
                RoutedEvent = DisableJobsEvent,
                JenkinsNodes = jobs
            };

            RaiseEvent(args);
        }

        private void mnuSaveConfigXml_Click(object sender, RoutedEventArgs e)
        {
            RaiseSaveConfigEvent();
        }

        private void RaiseSaveConfigEvent()
        {
            try
            {
                var jobs = GetSelectedJobs();

                if (jobs.Count == 0)
                {
                    MessageBox.Show("Please select at least one Jenkins job.", Settings.Default.AppName,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    return;
                }
                var args = new JobActionEventArgs
                {
                    RoutedEvent = SaveJobConfigEvent,
                    JenkinsNodes = jobs
                };
                RaiseEvent(args);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void EditJobsHandler(RoutedEventArgs e)
        {
            RaiseEditJobsEvent();
        }

        private void RaiseEditJobsEvent()
        {
            try
            {
                var jobsToEdit = GetSelectedJobs();

                if (jobsToEdit.Count == 0)
                    return;

                var args = new JobActionEventArgs
                {
                    RoutedEvent = EditJobsEvent,
                    JenkinsNodes = jobsToEdit
                };

                RaiseEvent(args);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuEditJobs_Click(object sender, RoutedEventArgs e)
        {
            EditJobsHandler(e);
        }

        public event JenkinsJobsEventHandler PushJob
        {
            add { AddHandler(PushJobEvent, value); }
            remove { RemoveHandler(PushJobEvent, value); }
        }

        private void mnuPushJobChange_Click(object sender, RoutedEventArgs e)
        {
            RaisePushJobEvent();
        }

        private void RaisePushJobEvent()
        {
            var jobsToPush = GetSelectedJobs(JobState.New | JobState.UpdatedLocally);
            if (jobsToPush.Count == 0)
                return;

            var pushEventArgs = new JobActionEventArgs
            {
                RoutedEvent = PushJobEvent,
                JenkinsNodes = jobsToPush
            };

            RaiseEvent(pushEventArgs);
        }

        public event JenkinsJobsEventHandler DeleteJob
        {
            add { AddHandler(DeleteJobEvent, value); }
            remove { RemoveHandler(DeleteJobEvent, value); }
        }

        private void mnuDeleteJobs_Click(object sender, RoutedEventArgs e)
        {
            RaiseDeleteJobsEvent();
        }

        private void RaiseDeleteJobsEvent()
        {
            var jobsToDelete = GetSelectedJobs();
            if (jobsToDelete.Count == 0)
                return;
            var args = new JobActionEventArgs
            {
                RoutedEvent = DeleteJobEvent,
                JenkinsNodes = jobsToDelete
            };
            RaiseEvent(args);
        }

        public event RoutedEventHandler CompareJobs
        {
            add { AddHandler(CompareJobsEvent, value); }
            remove { RemoveHandler(CompareJobsEvent, value); }
        }

        private void mnuCompareJobs_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CompareJobsEvent));
        }

        public event JenkinsJobsEventHandler UndoJobChange
        {
            add { AddHandler(UndoJobChangeEvent, value); }
            remove { RemoveHandler(UndoJobChangeEvent, value); }
        }

        private void mnuUndoJobChange_Click(object sender, RoutedEventArgs e)
        {
            RaiseUndoJobChangeEvent();
        }

        private void RaiseUndoJobChangeEvent()
        {
            var jobsToUndo = GetSelectedJobs(JobState.New | JobState.UpdatedLocally);
            var args = new JobActionEventArgs
            {
                RoutedEvent = UndoJobChangeEvent,
                JenkinsNodes = jobsToUndo
            };
            RaiseEvent(args);
        }

        public event JenkinsJobsEventHandler CopyJobs
        {
            add { AddHandler(CopyJobsEvent, value); }
            remove { RemoveHandler(CopyJobsEvent, value); }
        }

        private void mnuCopyJobs_Click(object sender, RoutedEventArgs e)
        {
            RaiseCopyJobsEvent(false);
        }

        public void RaiseCopyJobsEvent(bool dragAndDrop, JenkinsNodeCollection selectedJenkinsNodes = null)
        {
            if (selectedJenkinsNodes == null)
            {
                selectedJenkinsNodes = GetSelectedJobs(JobState.Orginal | JobState.UpdatedLocally);
            }

            var args = new JobActionEventArgs
            {
                JenkinsNodes = selectedJenkinsNodes,
                RoutedEvent = dragAndDrop ? CopyJobsByDragAndDropEvent : CopyJobsEvent
            };

            RaiseEvent(args);
        }

        public event JenkinsJobsEventHandler DuplicateJob
        {
            add { AddHandler(DuplicateJobEvent, value); }
            remove { RemoveHandler(DuplicateJobEvent, value); }
        }

        private void mnuDuplicateJob_Click(object sender, RoutedEventArgs e)
        {
            RaiseDuplicateJobEvent();
        }

        private void RaiseDuplicateJobEvent()
        {
            // Allowing to duplicate an unpushed job for now.
            var jobsToCopy = GetSelectedJobs();
            if (jobsToCopy.Count == 0)
                return;

            var args = new JobActionEventArgs
            {
                RoutedEvent = DuplicateJobEvent,
                JenkinsNodes = jobsToCopy
            };
            RaiseEvent(args);
        }

        public event RoutedEventHandler LoadJobs
        {
            add { AddHandler(LoadJobsEvent, value); }
            remove { RemoveHandler(LoadJobsEvent, value); }
        }

        private void mnuLoadJob_Click(object sender, RoutedEventArgs e)
        {
            RaiseLoadJobsEvent();
        }

        private void RaiseLoadJobsEvent()
        {
            RaiseEvent(new RoutedEventArgs(LoadJobsEvent));
        }

        public event JenkinsJobsEventHandler BuildJob
        {
            add { AddHandler(BuildJobEvent, value); }
            remove { RemoveHandler(BuildJobEvent, value); }
        }

        private void mnuBuildJob_Click(object sender, RoutedEventArgs e)
        {
            RaiseBuildJobEvent();
        }

        private void RaiseBuildJobEvent()
        {
            var jobsToBuild = GetSelectedJobs(JobState.Orginal);

            if (jobsToBuild.Count == 0)
                return;
            var args = new JobActionEventArgs
            {
                RoutedEvent = BuildJobEvent,
                JenkinsNodes = jobsToBuild
            };
            RaiseEvent(args);
        }

        private void RaiseShowMessageEvent(string message, MessageType msgType)
        {
            var args = new MessageEventArgs
            {
                Message = message,
                MessageType = msgType,
                RoutedEvent = ShowMessageEvent
            };

            RaiseEvent(args);
        }

        #endregion

        #region Filter related code

        private string _filterText;
        public string FilterText
        {
            get { return _filterText; }
            set
            {
                _filterText = value;
                if (Settings.Default.PreserveFilterText)
                {
                    if (Pane == JenkinsPane.Left)
                    {
                        Settings.Default.LeftFilterText = _filterText;
                    }
                    else
                    {
                        Settings.Default.RightFilterText = _filterText;
                    }

                    Settings.Default.Save();
                }
                if (!chkRegexOn.IsChecked.Value)
                {
                    OnPropertyChanged("FilterText");
                    if (lstJenkinsJobs.ItemsSource != null)
                        CollectionViewSource.GetDefaultView(lstJenkinsJobs.ItemsSource).Refresh();
                }
            }
        }

        private bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private void txtFilter_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter && chkRegexOn.IsChecked.Value)
            {
                if (!IsValidRegex(FilterText))
                {
                    RaiseShowMessageEvent("Regular expression is invalid.", MessageType.Error);
                    txtFilter.Background = Brushes.Pink;
                    txtFilter.SelectAll();
                    txtFilter.Focus();
                    return;
                }

                try
                {
                    lstJenkinsJobs.Items.Filter = delegate(object o)
                    {
                        var job = o as JenkinsNode;
                        try
                        {
                            txtFilter.Background = Brushes.White;
                            var matches = Regex.IsMatch(job.Name, FilterText, RegexOptions.IgnoreCase);
                            return matches;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    };
                }
                catch (Exception exp)
                {
                    RaiseShowMessageEvent(exp.Message, MessageType.Error);
                    txtFilter.Background = Brushes.Pink;
                }
                finally
                {
                    txtFilter.SelectAll();
                    txtFilter.Focus();
                }
            }

        }

        private void chkShowOnlyUnpushedJobs_Click(object sender, RoutedEventArgs e)
        {
            var chkBox = sender as CheckBox;
            if (chkBox != null)
                if ((bool) chkBox.IsChecked)
                    lstJenkinsJobs.Items.Filter = delegate(object o)
                    {
                        var job = o as JenkinsNode;
                        return (job.State == JobState.New) || (job.State == JobState.UpdatedLocally);
                    };
                else
                    lstJenkinsJobs.Items.Filter = jobCollection_Filter;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private bool jobCollection_Filter(object obj)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                txtFilter.Background = Brushes.White;
                return true;
            }

            var job = obj as JenkinsNode;

            try
            {
                var pattern = "^" + Regex.Escape(FilterText).Replace("\\*", ".*").Replace("\\?", ".");
                var matches = Regex.IsMatch(job.Name, pattern, RegexOptions.IgnoreCase);
                txtFilter.Background = Brushes.White;
                return matches;
            }
            catch
            {
                txtFilter.Background = Brushes.Pink;
                return false;
            }
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty UrlHistoryProperty =
            DependencyProperty.Register("UrlHistory", typeof(ObservableCollection<string>), typeof(JobList));

        public ObservableCollection<string> UrlHistory
        {
            get { return (ObservableCollection<string>)GetValue(UrlHistoryProperty); }
            set { SetValue(UrlHistoryProperty, value); }
        }

        public static readonly DependencyProperty JobsProperty = DependencyProperty.Register("JenkinsNodes",
            typeof(JenkinsNodeCollection), typeof(JobList));

        public JenkinsNodeCollection JenkinsNodes
        {
            get { return (JenkinsNodeCollection) GetValue(JobsProperty); }
            set
            {
                // keep the locally updated jobs in memory
                IEnumerable<JenkinsNode> editedJobs = null;
                if (JenkinsNodes != null)
                {
                    editedJobs = JenkinsNodes.Where(j => j.State == JobState.UpdatedLocally);
                }

                SetValue(JobsProperty, value);

                JenkinsNodes?.PropagateCredential(JenkinsUsername, JenkinsApiToken);

                var view = CollectionViewSource.GetDefaultView(lstJenkinsJobs.ItemsSource);
                if (view != null && view.Filter == null)
                    view.Filter = jobCollection_Filter;

                // Now mark previously selected jobs selected
                if (_selectedJobs != null && _selectedJobs.Count > 0)
                {
                    foreach (var selectedJob in _selectedJobs)
                    {
                        var targetJob = JenkinsNodes.FirstOrDefault(job => job.Name == selectedJob);
                        if (targetJob != null)
                            targetJob.Selected = true;
                    }
                }

                // preserve the updated items
                if (editedJobs != null && editedJobs.Any())
                {
                    foreach (var editedJob in editedJobs)
                    {
                        var targetJob = JenkinsNodes.FirstOrDefault(job => job.Name == editedJob.Name);
                        if (targetJob != null)
                        {
                            targetJob.State = JobState.UpdatedLocally;
                            targetJob.LocalConfigFilePath = editedJob.LocalConfigFilePath;
                        }

                    }
                }

                var sortDirection = new SortDescription(_lastSortBy, _lastSortDirection);
                view?.SortDescriptions.Add(sortDirection);
            }
        }


        public static readonly DependencyProperty JenkinsUrlProperty = DependencyProperty.Register("JenkinsUrl",
            typeof(string), typeof(JobList));

        public string JenkinsUrl
        {
            get { return (string) GetValue(JenkinsUrlProperty); }
            set
            {
                SetValue(JenkinsUrlProperty, value);
            }
        }

        public static readonly DependencyProperty JenkinsVersionProperty = DependencyProperty.Register("JenkinsVersion",
            typeof(string), typeof(JobList));

        public string JenkinsVersion
        {
            get { return (string) GetValue(JenkinsVersionProperty); }
            set { SetValue(JenkinsVersionProperty, value); }
        }

        public string JenkinsDomain => new Uri(JenkinsUrl).DnsSafeHost;

        public static readonly DependencyProperty JenkinsUsernameProperty =
            DependencyProperty.Register("JenkinsUsername", typeof(string), typeof(JobList));

        public string JenkinsUsername
        {
            get { return (string) GetValue(JenkinsUsernameProperty); }
            set { SetValue(JenkinsUsernameProperty, value); }
        }

        public static readonly DependencyProperty JenkinsApiTokenProperty =
            DependencyProperty.Register("JenkinsApiToken", typeof(string), typeof(JobList));

        public string JenkinsApiToken
        {
            get { return (string) GetValue(JenkinsApiTokenProperty); }
            set { SetValue(JenkinsApiTokenProperty, value); }
        }

        public static readonly DependencyProperty PaneProperty = DependencyProperty.Register("Pane",
            typeof(JenkinsPane), typeof(JobList));

        public JenkinsPane Pane
        {
            get { return (JenkinsPane) GetValue(PaneProperty); }
            set { SetValue(PaneProperty, value); }
        }

        public static readonly DependencyProperty JenkinsApiCredentialsProperty =
            DependencyProperty.Register("JenkinsApiCredentials",
                typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>), typeof(JobList));

        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get
            {
                return
                    (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)
                    GetValue(JenkinsApiCredentialsProperty);
            }
            set
            {
                SetValue(JenkinsApiCredentialsProperty, value);
                // Populate the combobox
                if (UrlHistory == null)
                {
                    UrlHistory = new ObservableCollection<string>();
                }

                if (JenkinsApiCredentials != null)
                {
                    //Clean up the deleted URL
                    for (int i = UrlHistory.Count - 1; i >= 0; i--)
                    {
                        if (!JenkinsApiCredentials.ContainsKey(UrlHistory[i]))
                        {
                            UrlHistory.RemoveAt(i);
                            cboUrl.SelectedIndex = 0;
                            OnPropertyChanged(nameof(JenkinsUrl));
                            OnPropertyChanged(nameof(UrlHistory));

                            var args = new RoutedEventArgs { RoutedEvent = UrlRefreshEvent };
                            RaiseEvent(args);

                        }
                    }

                    foreach (var key in JenkinsApiCredentials.Keys)
                    {
                        if (!UrlHistory.Contains(key))
                        {
                            UrlHistory.Add(key);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(JenkinsUrl) && !UrlHistory.Contains(JenkinsUrl))
                {
                    UrlHistory.Add(JenkinsUrl);
                }

                

                //UrlHistory.Sort();

                OnPropertyChanged(nameof(JenkinsUrl));
                OnPropertyChanged(nameof(UrlHistory));

                //cbUrl.SelectedItem = JenkinsUrl;
            }
        }

        public static readonly DependencyProperty JenkinsJobAuthTokenProperty =
            DependencyProperty.Register("JenkinsJobAuthToken",
                typeof(string), typeof(JobList));

        public string JenkinsJobAuthToken
        {
            get { return (string) GetValue(JenkinsJobAuthTokenProperty); }
            set { SetValue(JenkinsJobAuthTokenProperty, value); }
        }

        #endregion

        private List<string> _selectedJobs;

        private void chkJob_Checked(object sender, RoutedEventArgs e)
        {
            var chkJob = sender as CheckBox;
            var job = chkJob?.DataContext as JenkinsNode;
            if (job != null)
            {
                AddRemoveSelectedJob(job.Name);
            }
        }

        private void chkJob_Unchecked(object sender, RoutedEventArgs e)
        {
            var chkJob = sender as CheckBox;
            var job = chkJob?.DataContext as JenkinsNode;
            if (job != null)
            {
                AddRemoveSelectedJob(job.Name, false);
            }
        }

        private void AddRemoveSelectedJob(string jobName, bool add = true)
        {
            if (_selectedJobs == null)
                _selectedJobs = new List<string>();

            var jobNameExists = _selectedJobs.Contains(jobName);

            if (add)
            {
                if (!jobNameExists)
                    _selectedJobs.Add(jobName);
            }
            else
            {
                if (jobNameExists)
                    _selectedJobs.Remove(jobName);
            }
        }

        private void txtFilter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            txtFilter.SelectAll();
            txtFilter.Focus();
            e.Handled = true;
        }

        private void mnuShowNodes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var nodesWindow = new Nodes(JenkinsUrl, txtUsername.Text, txtApiToken.Text);
                nodesWindow.Show();

            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuOpenLocalTempDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dirPath = Path.Combine(Settings.Default.LocalTempDirectory, JenkinsDomain);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                Process.Start("explorer.exe", dirPath);

            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (UrlHistory != null && UrlHistory.Contains(JenkinsUrl))
                {
                    int index = UrlHistory.IndexOf(JenkinsUrl);
                    if (index > 0)
                    {
                        NavigateToUrl(UrlHistory[index - 1], true);
                    }
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }


        private void mnuCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            RaiseCreateFolderEvent();
        }

        private void HandleTextBoxPreviewKeyDown()
        {
            try
            {
                if (string.IsNullOrEmpty(txtUsername.Text.Trim()))
                {
                    RaiseShowMessageEvent("Please enter username.", MessageType.Error);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(txtApiToken.Text.Trim()))
                {
                    RaiseShowMessageEvent("Please enter API Token.", MessageType.Error);
                    txtApiToken.Focus();
                    return;
                }

                NavigateToUrl(cboUrl.Text);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void txtApiToken_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HandleTextBoxPreviewKeyDown();
            }
        }

        private void txtUsername_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HandleTextBoxPreviewKeyDown();
            }
        }
    }
}