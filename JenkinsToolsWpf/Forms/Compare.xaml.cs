using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using JenkinsLib;
using JenkinsToolsetWpf.Controls;
using JenkinsToolsetWpf.Properties;
using Newtonsoft.Json;
using Clipboard = System.Windows.Clipboard;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Windows.Forms.Timer;

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    ///     Interaction logic for Compare.xaml
    /// </summary>
    public partial class Compare : Window
    {
        private ObservableConcurrentDictionary<string, JenkinsCredentialPair> _jenkinsApiCredentials;
        private readonly Storyboard _messageAnimator;
        private string _tempDirPath;
        private JobConfigFileWatcher _watcher;

        public Compare()
        {
            InitializeComponent();
            _messageAnimator = FindResource("AnimateMessage") as Storyboard;
        }

        private void ShowMessage(string message, MessageType msgType = MessageType.Information)
        {
            switch (msgType)
            {
                case MessageType.Information:
                    lblNotification.Background = Brushes.RoyalBlue;
                    break;
                case MessageType.Warning:
                    lblNotification.Background = Brushes.DarkOrange;
                    break;
                case MessageType.Error:
                    lblNotification.Background = Brushes.Red;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(msgType), msgType, null);
            }

            lblNotification.Content = message;
            _messageAnimator.Begin();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
            Debug.WriteLine(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming).FilePath);

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                lblNotification.Content = "No message yet";
                _watcher = new JobConfigFileWatcher();

                if (string.IsNullOrEmpty(Settings.Default.LocalTempDirectory))
                {
                    _tempDirPath = Path.Combine(Path.GetTempPath(), Settings.Default.AppName);
                    Settings.Default.LocalTempDirectory = _tempDirPath;
                }
                else
                {
                    _tempDirPath = Settings.Default.LocalTempDirectory;
                }

                // If JenkinsApiCredentials contains JSON data, deserialize it.
                if (!string.IsNullOrEmpty(Settings.Default.JenkinsApiCredentials))
                {
                    try
                    {
                        _jenkinsApiCredentials =
                            JsonConvert.DeserializeObject<ObservableConcurrentDictionary<string, JenkinsCredentialPair>>
                                (Settings.Default.JenkinsApiCredentials);
                    }
                    catch (Exception)
                    {
                        _jenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
                    }
                }
                else
                {
                    _jenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
                    SaveJenkinsCredential(LeftJenkinsJobs);
                    SaveJenkinsCredential(RightJenkinsJobs);
                }

                LeftJenkinsJobs.JenkinsApiCredentials = RightJenkinsJobs.JenkinsApiCredentials = _jenkinsApiCredentials;

                LoadPanes();
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

        private void LoadPanes()
        {
            _watcher?.JenkinsNodesToWatch.Clear();
            LoadLeftPane();
            LoadRightPane();
            lblLastUpdated.Content = $"Last Updated:{DateTime.Now:G}";
        }

        private Tuple<string, string> GetApiCredential(string url)
        {
            if (_jenkinsApiCredentials?.ContainsKey(url) == true)
            {
                var cred = _jenkinsApiCredentials[url];
                return Tuple.Create(cred.Username, cred.ApiToken);
            }

            return null;
        }

        private async void LoadRightPane()
        {
            var jenkinsServer = new JenkinsServer();
            try
            {
                if (Settings.Default.RightJenkinsBaseUrl.Trim() == "http://" ||
                    string.IsNullOrEmpty(Settings.Default.RightJenkinsBaseUrl.Trim()))
                {
                    return;
                }
                jenkinsServer.JenkinsUrl = Settings.Default.RightJenkinsBaseUrl;
                var cred = GetApiCredential(jenkinsServer.JenkinsBaseUrl);
                RightJenkinsJobs.JenkinsUsername = cred?.Item1;
                RightJenkinsJobs.JenkinsApiToken = cred?.Item2;

                jenkinsServer.Username = cred?.Item1;
                jenkinsServer.ApiToken = cred?.Item2;

                RightJenkinsJobs.JenkinsVersion = await jenkinsServer.GetVersion();
                if (!string.IsNullOrEmpty(RightJenkinsJobs.JenkinsUsername) &&
                    !string.IsNullOrEmpty(RightJenkinsJobs.JenkinsApiToken))
                {
                    RightJenkinsJobs.JenkinsNodes = await jenkinsServer.GetJenkinsNodes(RightJenkinsJobs.JenkinsUsername, RightJenkinsJobs.JenkinsApiToken, Settings.Default.Jenkins2FlatView);
                }
                else
                {
                    RightJenkinsJobs.JenkinsNodes = await jenkinsServer.GetJenkinsNodes(string.Empty, string.Empty, Settings.Default.Jenkins2FlatView);
                }
                    

                if (RightJenkinsJobs.JenkinsNodes != null)
                {
                    _watcher.JenkinsNodesToWatch.AddRange(RightJenkinsJobs.JenkinsNodes);
                }
            }
            catch (HttpRequestException exp)
            {
                var msg = "Error occurred when fetching Jenkins job data. Please check the following.\r\n\r\n";
                msg += $"Right Jenkins URL: {RightJenkinsJobs.JenkinsUrl}\r\n";
                msg += $"Right Username: {RightJenkinsJobs.JenkinsUsername}\r\n";
                msg += $"Right API Token: {RightJenkinsJobs.JenkinsApiToken}\r\n\r\n";

                msg += $"Actual error message: {exp.Message}";

                MessageBox.Show(msg, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void LoadLeftPane()
        {
            var jenkinsServer = new JenkinsServer();
            try
            {
                if (Settings.Default.LeftJenkinsBaseUrl.Trim() == "http://" ||
                    string.IsNullOrEmpty(Settings.Default.LeftJenkinsBaseUrl.Trim()))
                {
                    return;
                }
                jenkinsServer.JenkinsUrl = Settings.Default.LeftJenkinsBaseUrl;

                var cred = GetApiCredential(jenkinsServer.JenkinsBaseUrl);
                LeftJenkinsJobs.JenkinsUsername = cred?.Item1;
                LeftJenkinsJobs.JenkinsApiToken = cred?.Item2;

                jenkinsServer.Username = cred?.Item1;
                jenkinsServer.ApiToken = cred?.Item2;

                LeftJenkinsJobs.JenkinsVersion = await jenkinsServer.GetVersion();

                if (!string.IsNullOrEmpty(LeftJenkinsJobs.JenkinsUsername) &&
                    !string.IsNullOrEmpty(LeftJenkinsJobs.JenkinsApiToken))
                {
                    LeftJenkinsJobs.JenkinsNodes = await jenkinsServer.GetJenkinsNodes(LeftJenkinsJobs.JenkinsUsername, LeftJenkinsJobs.JenkinsApiToken, Settings.Default.Jenkins2FlatView);
                }
                else
                {
                    LeftJenkinsJobs.JenkinsNodes = await jenkinsServer.GetJenkinsNodes(string.Empty, string.Empty, Settings.Default.Jenkins2FlatView);
                }
                

                if (LeftJenkinsJobs.JenkinsNodes != null)
                {
                    _watcher.JenkinsNodesToWatch.AddRange(LeftJenkinsJobs.JenkinsNodes);
                }
            }
            catch (HttpRequestException exp)
            {
                string msg = $"Error occurred when fetching Jenkins job data. Please check the following.\r\n\r\n";
                msg += $"Left Jenkins URL: {LeftJenkinsJobs.JenkinsUrl}\r\n";
                msg += $"Left Username: {LeftJenkinsJobs.JenkinsUsername}\r\n";
                msg += $"Left API Token: {LeftJenkinsJobs.JenkinsApiToken}\r\n\r\n";
                msg += $"Actual error message: {exp.Message}";

                MessageBox.Show(msg, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowMessage("Data refreshed successfully");
                LoadPanes();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F5)
                {

                    ShowMessage("Data refreshed successfully");
                    LoadPanes();
                }

            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                Settings.Default.Save();
                //Clean up the temp files
                if (Settings.Default.CleanUpOnExit)
                    _watcher?.CleanTempFiles();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }


        private async void OnCompareJobs(object sender, RoutedEventArgs e)
        {
            await CompareJobs();
        }

        private async void btnCompare_OnClick(object sender, RoutedEventArgs e)
        {
            await CompareJobs();
        }

        private async Task CompareJobs()
        {
            try
            {
                string message;
                var leftJobs = LeftJenkinsJobs.GetSelectedJobs();
                leftJobs.PropagateCredential(LeftJenkinsJobs.JenkinsUsername, LeftJenkinsJobs.JenkinsApiToken);

                var rightJobs = RightJenkinsJobs.GetSelectedJobs();
                rightJobs.PropagateCredential(RightJenkinsJobs.JenkinsUsername, RightJenkinsJobs.JenkinsApiToken);

                if (!ValidateCompareSelection(out message, leftJobs, rightJobs))
                {
                    MessageBox.Show(message, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Asterisk,
                        MessageBoxResult.OK);

                    return;
                }

                JenkinsNode comp1JenkinsNode, comp2JenkinsNode;

                // Only 3 cases can be considered.
                if (leftJobs.Count == 2 && rightJobs.Count == 0)
                {
                    comp1JenkinsNode = leftJobs[0];
                    comp2JenkinsNode = leftJobs[1];
                }
                else if (leftJobs.Count == 1 && rightJobs.Count == 1)
                {
                    comp1JenkinsNode = leftJobs[0];
                    comp2JenkinsNode = rightJobs[0];
                }
                else if (leftJobs.Count == 0 && rightJobs.Count == 2)
                {
                    comp1JenkinsNode = rightJobs[0];
                    comp2JenkinsNode = rightJobs[1];
                }
                else
                {
                    throw new Exception("Unexpected condition has been detected.");
                }

                // Generate the Jenkins job XML files and save them to temporary directory
                if (!Directory.Exists(_tempDirPath))
                    Directory.CreateDirectory(_tempDirPath);

                // If LocalConfigFilePath already exists, then load the file from there. Make sure the file exists there.
                await RefreshLocalXmlConfigFile(comp1JenkinsNode);
                await RefreshLocalXmlConfigFile(comp2JenkinsNode);

                var t = new Thread(delegate()
                {
                    var compareToolProcess = new Process();
                    compareToolProcess.StartInfo.FileName = $"\"{Settings.Default.DiffExePath}\"";
                    compareToolProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                    compareToolProcess.StartInfo.Arguments =
                        $"{Settings.Default.CompareToolSwitches} \"{comp1JenkinsNode.LocalConfigFilePath}\" \"{comp2JenkinsNode.LocalConfigFilePath}\"";
                    compareToolProcess.Start();
                });

                t.Start();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private async Task RefreshLocalXmlConfigFile(JenkinsNode jenkinsNode)
        {
            if (Settings.Default.PreserveLocalXmlChanges)
            {
                if (!string.IsNullOrEmpty(jenkinsNode.LocalConfigFilePath) && File.Exists(jenkinsNode.LocalConfigFilePath))
                {
                    var configFile = new FileInfo(jenkinsNode.LocalConfigFilePath);
                    var timeDiff = DateTime.Now - configFile.LastWriteTime;
                    if (timeDiff.TotalMinutes > Settings.Default.RetentionMinutes)
                    {
                        jenkinsNode.LocalConfigFilePath = GetTemporaryConfigXmlFilePath(jenkinsNode);
                        jenkinsNode.OriginalConfigXml = await jenkinsNode.GetConfigXml();
                        File.WriteAllText(jenkinsNode.LocalConfigFilePath, jenkinsNode.OriginalConfigXml);
                    }
                }
                else
                {
                    jenkinsNode.LocalConfigFilePath = GetTemporaryConfigXmlFilePath(jenkinsNode);
                    jenkinsNode.OriginalConfigXml = await jenkinsNode.GetConfigXml();
                    File.WriteAllText(jenkinsNode.LocalConfigFilePath, jenkinsNode.OriginalConfigXml);
                }
            }
            else
            {
                jenkinsNode.LocalConfigFilePath = GetTemporaryConfigXmlFilePath(jenkinsNode);
                jenkinsNode.OriginalConfigXml = await jenkinsNode.GetConfigXml();
                File.WriteAllText(jenkinsNode.LocalConfigFilePath, jenkinsNode.OriginalConfigXml);
            }
        }

        private string GetTemporaryConfigXmlFilePath(JenkinsNode jenkinsNode)
        {
            string directory = Path.Combine(_tempDirPath, jenkinsNode.JenkinsDomain);

            var uri = new Uri(jenkinsNode.Url);
            for (int i = 0; i < uri.Segments.Length - 2; i++)
            {
                var subDir = uri.Segments[i].Replace("/", "");
                if (!string.IsNullOrEmpty(subDir) && subDir != "job")
                {
                    directory = Path.Combine(directory, subDir);
                }
            }

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return Path.Combine(directory, $"{jenkinsNode.Name}.xml");
        }

        private async void OnEditJobs(object sender, JobActionEventArgs e)
        {
            var jobList = sender as JobList;
            if (string.IsNullOrEmpty(jobList.JenkinsUrl))
            {
                MessageBox.Show("Please enter or select the correct Jenkin server URL.", Settings.Default.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            // make sure the text editor executable exists.
            if (string.IsNullOrEmpty(Settings.Default.TextEditorExePath) ||
                !File.Exists(Settings.Default.TextEditorExePath))
            {
                MessageBox.Show($"Executable for text editor ${Settings.Default.TextEditorExePath} does not exist.",
                    Settings.Default.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (!Directory.Exists(_tempDirPath))
                    Directory.CreateDirectory(_tempDirPath);

                e.JenkinsNodes.PropagateCredential(jobList.JenkinsUsername, jobList.JenkinsApiToken);

                foreach (var jobToChange in e.JenkinsNodes)
                {
                    // If the local copy of the file already exists and it's updated, load the XML from there.
                    await RefreshLocalXmlConfigFile(jobToChange);
                    // Run the text editor process and forget about it...
                    var t = new Thread(() =>
                    {
                        var compareToolProcess = new Process
                        {
                            StartInfo =
                            {
                                FileName = Settings.Default.TextEditorExePath,
                                WindowStyle = ProcessWindowStyle.Normal,
                                Arguments = $"\"{jobToChange.LocalConfigFilePath}\""
                            }
                        };
                        compareToolProcess.Start();
                    });

                    t.Start();
                }
            }
            catch (HttpRequestException exp)
            {
                string msg = $"Jenkins Server returned an error. Please check the following.\r\n\r\n";
                msg += $"Jenkins URL: {jobList.JenkinsUrl}\r\n";
                msg += $"Username: {jobList.JenkinsUsername}\r\n";
                msg += $"API Token: {jobList.JenkinsApiToken}\r\n\r\n";
                msg += $"Actual error message: {exp.Message}";

                MessageBox.Show(msg, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private bool ValidateCompareSelection(out string outputMessage, JenkinsNodeCollection leftJenkinsNodes, JenkinsNodeCollection rightJenkinsNodes)
        {
            outputMessage = string.Empty;
            var selectedJobsSum = leftJenkinsNodes.Count + rightJenkinsNodes.Count;

            if (selectedJobsSum < 2)
            {
                outputMessage = "Less than 2 Jenkins jobs are selected. Please select 2 jobs to compare.";
                return false;
            }

            if (selectedJobsSum > 2)
            {
                outputMessage = "More than 2 Jenkins jobs are selected. Please select 2 jobs to compare.";
                return false;
            }

            return true;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //var settingsWindow = new SettingsWindow();
                var settingsWindow = new NewSettingsWindow {Owner = this};
                settingsWindow.JenkinsApiCredentials = _jenkinsApiCredentials;
                settingsWindow.ShowDialog();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private bool CheckJobList(JobList jobList)
        {
            if (string.IsNullOrEmpty(jobList.JenkinsUrl))
            {
                MessageBox.Show("Please enter or select the correct Jenkin server URL.", Settings.Default.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }


        private async void OnPushJobs(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (!CheckJobList(jobList))
                {
                    return;
                }

                if (e.JenkinsNodes.Count == 0)
                {
                    ShowMessage("Please select at least one changed or newly created job", MessageType.Warning);
                    return;
                }
                else
                {
                    var result = MessageBox.Show(
                        $"You are about to push Jenkins job(s) to the following server.\r\n\r\n{jobList.JenkinsUrl}\r\n\r\nAre you sure you would like to proceed?",
                        Settings.Default.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question,
                        MessageBoxResult.No);

                    if (result == MessageBoxResult.No)
                        return;
                }


                Mouse.OverrideCursor = Cursors.Wait;

                if (e.JenkinsNodes == null || e.JenkinsNodes.Count == 0)
                {
                    ShowMessage("Please select at least one new or updated job in the list.", MessageType.Warning);
                    return;
                }

                // Take care of non folder nodes first.
                var nonFolderNodes = e.JenkinsNodes.Where(n => n.JenkinsNodeType != JenkinsNodeType.Folder).ToArray();
                
                var tasks = new Task[nonFolderNodes.Length];
                var jenkinsServer = new JenkinsServer
                {
                    JenkinsUrl = jobList.JenkinsUrl,
                    Username = jobList.JenkinsUsername,
                    ApiToken = jobList.JenkinsApiToken
                };
                var jobsNotPushed = new StringBuilder();

                for (var i = 0; i < tasks.Length; i++)
                {
                    var jobToPush = nonFolderNodes[i];

                    if (Settings.Default.ValidateJenkinsJobXmlWellFormed)
                    {
                        if (!jobToPush.ValidateJobConfigXml())
                        {
                            jobsNotPushed.AppendLine(jobToPush.Name);
                            continue;
                        }
                    }

                    Task task;

                    if (jobToPush.State == JobState.New)
                    {
                        LocalAsyncTask del = async () =>
                        {
                            await jenkinsServer.CreateNewJob(jobToPush.Name,
                                File.ReadAllText(jobToPush.LocalConfigFilePath));
                        };

                        task = del();
                        tasks[i] = task;
                    }
                    else
                    {
                        LocalAsyncTask del = async () =>
                        {
                            await jobToPush.Update(File.ReadAllText(jobToPush.LocalConfigFilePath));
                        };

                        task = del();
                        tasks[i] = task;
                    }

                    if (task != null && task.IsCompleted)
                    {
                        jobToPush.State = JobState.Orginal;
                        jobToPush.Selected = true;
                    }
                }

                if (tasks.Any(t => t != null))
                {
                    await Task.WhenAll(tasks);
                }

                //Non folder nodes have been taken care of. Now take care of copying folder recursively.
                var folderNodes = e.JenkinsNodes.Where(n => n.JenkinsNodeType == JenkinsNodeType.Folder).ToArray();
                string sourceUsername = "";
                string sourceApiToken = "";
                if (jobList.Pane == JenkinsPane.Left)
                {
                    sourceUsername = RightJenkinsJobs.JenkinsUsername;
                    sourceApiToken = RightJenkinsJobs.JenkinsApiToken;
                }
                else
                {
                    sourceUsername = LeftJenkinsJobs.JenkinsUsername;
                    sourceApiToken = LeftJenkinsJobs.JenkinsApiToken;
                }


                foreach (var folderNode in folderNodes)
                {
                    jenkinsServer.ClearJenkinsNodes();
                    await jenkinsServer.CopyFolder(folderNode.OriginalUrl,
                            jobList.JenkinsUrl,
                            jobList.JenkinsUsername,
                            jobList.JenkinsApiToken,
                            sourceUsername,
                            sourceApiToken);

                    folderNode.State = JobState.Orginal;
                }

                if (jobsNotPushed.Length > 0)
                {
                    MessageBox.Show($"The following job(s) were not pushed due to XML well-formedness validation error.\r\n\r\n{jobsNotPushed}",
                        Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Stop);
                }
                else
                {
                    foreach (var job in e.JenkinsNodes)
                    {
                        job.State = JobState.Orginal;
                        job.Selected = true;

                        if (!_watcher.JenkinsNodesToWatch.Contains(job))
                        {
                            _watcher.JenkinsNodesToWatch.Add(job);
                        }
                    }

                    ShowMessage($"Job(s) and folder(s) pushed to Jenkins server successfully");
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

        private async void OnDeleteJobs(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (!CheckJobList(jobList))
                {
                    return;
                }

                var jobsToDelete = new StringBuilder();
                foreach (var jobChange in e.JenkinsNodes)
                {
                    jobsToDelete.AppendLine(jobChange.Name);
                }

                var result =
                    MessageBox.Show(
                        $"Are you sure you would like to completely delete the following Jenkins jobs from {jobList.JenkinsUrl}?\r\n\r\n{jobsToDelete}",
                        Settings.Default.AppName, MessageBoxButton.YesNo, MessageBoxImage.Stop, MessageBoxResult.No);

                if (result == MessageBoxResult.Yes)
                {
                    var lastConfirmation = MessageBox.Show("Are you really sure?", Settings.Default.AppName,
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                    if (lastConfirmation == MessageBoxResult.Yes)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        var tasks = new Task[e.JenkinsNodes.Count];
                        for (var i = 0; i < e.JenkinsNodes.Count; i++)
                        {
                            var jobToDelete = e.JenkinsNodes[i];
                            LocalAsyncTask del = async () =>
                            {
                                if (jobToDelete.State == JobState.New)
                                {
                                    if (!string.IsNullOrEmpty(jobToDelete.LocalConfigFilePath) && File.Exists(jobToDelete.LocalConfigFilePath) &&
                                        (_tempDirPath == Path.GetDirectoryName(jobToDelete.LocalConfigFilePath)))
                                    {
                                        File.Delete(jobToDelete.LocalConfigFilePath);
                                    }
                                    jobToDelete.OriginalConfigXml = string.Empty;
                                    jobToDelete.LocalConfigFilePath = null;
                                    jobList.MarkJobDeleted(jobToDelete);
                                    _watcher.JenkinsNodesToWatch.Remove(jobToDelete);
                                }
                                else
                                {
                                    await jobToDelete.Delete(jobList.JenkinsUsername, jobList.JenkinsApiToken);
                                    jobList.MarkJobDeleted(jobToDelete);
                                    _watcher.JenkinsNodesToWatch.Remove(jobToDelete);
                                }
                            };

                            var task = del();
                            tasks[i] = task;
                        }

                        await Task.WhenAll(tasks);

                        ShowMessage($"{tasks.Length} Jenkins job(s) deleted successfully");
                    }
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

        private void OnLoadJobs(object sender, RoutedEventArgs e)
        {
            var jobList = sender as JobList;
            if (!CheckJobList(jobList))
            {
                return;
            }
            try
            {
                var selectFileDialogue = new OpenFileDialog
                {
                    Multiselect = true,
                    CheckFileExists = true
                };

                if (selectFileDialogue.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadLocalFiles(jobList, selectFileDialogue.FileNames);
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

        private void OnLoadJobsByDragAndDrop(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (e.JobFiles != null)
                {
                    LoadLocalFiles(jobList, e.JobFiles);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void LoadLocalFiles(JobList jobList, string[] files)
        {
            if (Settings.Default.ClearFilterOnJobCopy)
            {
                ClearFilterText(jobList);
            }

            foreach (var file in files)
            {
                JenkinsNode jenkinsNode;
                var jobName = Path.GetFileNameWithoutExtension(file);
                // if exists, update the existing job
                if (jobList.JobExists(jobName))
                {
                    jenkinsNode = jobList.GetJob(jobName);
                    jenkinsNode.LocalConfigFilePath = file;
                    jenkinsNode.State = JobState.UpdatedLocally;
                    jenkinsNode.Selected = true;
                }
                else
                {
                    jenkinsNode = new JenkinsNode
                    {
                        LocalConfigFilePath = file,
                        State = JobState.New,
                        Name = jobName,
                        Url = $"{jobList.JenkinsUrl}job/{jobName}/",
                        Selected = true
                    };

                    jobList.AddNewJob(jenkinsNode);
                    _watcher.JenkinsNodesToWatch.Add(jenkinsNode);
                }
            }
        }

        private void OnUndoJobChange(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (!CheckJobList(jobList))
                {
                    return;
                }

                if (MessageBox.Show("Are you sure you would like to undo your local changes?", Settings.Default.AppName,
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                foreach (var job in e.JenkinsNodes)
                {
                    switch (job.State)
                    {
                        case JobState.New:
                            if (!string.IsNullOrEmpty(job.LocalConfigFilePath) && File.Exists(job.LocalConfigFilePath) &&
                                (_tempDirPath == Path.GetDirectoryName(job.LocalConfigFilePath)))
                            {
                                File.Delete(job.LocalConfigFilePath);
                            }
                            job.OriginalConfigXml = string.Empty;
                            job.LocalConfigFilePath = null;
                            jobList.MarkJobDeleted(job);
                            _watcher.JenkinsNodesToWatch.Remove(job);
                            break;
                        case JobState.UpdatedLocally:
                            job.State = JobState.Orginal;
                            if (!string.IsNullOrEmpty(job.LocalConfigFilePath) && File.Exists(job.LocalConfigFilePath) &&
                                (_tempDirPath == Path.GetDirectoryName(job.LocalConfigFilePath)))
                            {
                                File.Delete(job.LocalConfigFilePath);
                            }
                            job.OriginalConfigXml = string.Empty;
                            job.LocalConfigFilePath = null;
                            job.Selected = false;
                            break;
                    }
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

        private async void OnCopyJobs(object sender, JobActionEventArgs e)
        {
            try
            {
                await CopyJobs(sender, e, false);
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

        private async void OnCopyJobsByDragAndDrop(object sender, JobActionEventArgs e)
        {
            try
            {
                await CopyJobs(sender, e, true);
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

        private async Task CopyJobs(object sender, JobActionEventArgs e, bool dragAndDrop)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var sourcePane = sender as JobList;
            if (!Directory.Exists(_tempDirPath))
                Directory.CreateDirectory(_tempDirPath);

            JobList targetPane;
            if (dragAndDrop)
            {
                // Drop event is raised in the pane that the items are dropped on. 
                // So we need to flip the source pane and target pane programmatically here.
                targetPane = sourcePane;
                //sourcePane = sourcePane.Pane == JenkinsPane.Left ? RightJenkinsJobs : LeftJenkinsJobs;
            }
            else
            {
                // The regular copy event happens from the source pane. So the target pane is the 
                // other side of the pane.
                targetPane = sourcePane.Pane == JenkinsPane.Left ? RightJenkinsJobs : LeftJenkinsJobs;
            }



            // Start the actual copy process here
            foreach (var sourceJob in e.JenkinsNodes)
            {
                var originalConfigXml = await sourceJob.GetConfigXml();
                JenkinsNode copiedJenkinsNode;
                // If the job name already exists, mark it as update.
                if (targetPane.JobExists(sourceJob.Name))
                {
                    copiedJenkinsNode = targetPane.GetJob(sourceJob.Name);
                    copiedJenkinsNode.LocalConfigFilePath = GetTemporaryConfigXmlFilePath(sourceJob);
                    // This if condition is necessary because when user tries copying job more than once, 
                    // it would mark the new one updated locally. It caused pushing job to Jenkins to error out.
                    if (copiedJenkinsNode.State != JobState.New)
                        copiedJenkinsNode.State = JobState.UpdatedLocally;
                    copiedJenkinsNode.Selected = true;
                    targetPane.ScrollJobIntoView(copiedJenkinsNode);
                }
                else
                {
                    // If it doesn't exist in the target pane, add it as a new job.
                    copiedJenkinsNode = new JenkinsNode
                    {
                        Name = sourceJob.Name,
                        LocalConfigFilePath = GetTemporaryConfigXmlFilePath(sourceJob),
                        State = JobState.New,
                        Url = $"{targetPane.JenkinsUrl}job/{sourceJob.Name}/",
                        Selected = true,
                        Username = targetPane.JenkinsUsername,
                        ApiToken = targetPane.JenkinsApiToken,
                        OriginalUrl = sourceJob.Url,
                        _class = sourceJob._class,
                    };

                    if (sourceJob.JenkinsNodeType != JenkinsNodeType.Folder)
                    {
                        copiedJenkinsNode.Color = "grey";
                    }

                    targetPane.AddNewJob(copiedJenkinsNode);
                }
                File.WriteAllText(copiedJenkinsNode.LocalConfigFilePath, originalConfigXml);
            }

            if (Settings.Default.ClearFilterOnJobCopy)
            {
                ClearFilterText(targetPane);
            }
            else
            {
                ShowMessage($"{e.JenkinsNodes.Count} job(s) copied. Copied jobs may be hidden due to existing filter.\r\nPlease push them to Jenkins server.");
            }
        }

        private void btnCopyToRightPane_Click(object sender, RoutedEventArgs e)
        {
            LeftJenkinsJobs.RaiseCopyJobsEvent(false);
        }

        private void btnCopyToLeftPane_Click(object sender, RoutedEventArgs e)
        {
            RightJenkinsJobs.RaiseCopyJobsEvent(false);
        }

        private void ClearFilterText(JobList targetPane)
        {
            // If filter text is present, users may not see the copied job in the target pane, so
            // empty the filter. Notify user that the filter was emptied but the text was copied 
            // to the clipboard for later use.
            if (!string.IsNullOrEmpty(targetPane.FilterText))
            {
                Clipboard.SetDataObject(targetPane.FilterText);
                targetPane.FilterText = string.Empty;
                ShowMessage(
                    "Cleared the filter text for copied jobs to show.\r\nFilter text has been copied to clipboard for later use.",
                    MessageType.Warning);
            }
        }

        private async void OnRenameJob(object sender, JobActionEventArgs e)
        {
            try
            {
                var job = e.JenkinsNodes.FirstOrDefault();
                var jobList = sender as JobList;

                if (job != null)
                {
                    var inputBox = new InputBox();
                    inputBox.MessageText = $"Please enter a new name to rename {job.Name}";
                    inputBox.TextValue = job.Name;
                    inputBox.InputBoxTitle = "Rename Jenkins Job";
                    inputBox.Owner = this;

                    if ((bool) inputBox.ShowDialog())
                    {
                        if (jobList.JobExists(inputBox.TextValue))
                        {
                            MessageBox.Show(this, $"The Job name \"{inputBox.TextValue}\" already exists",
                                Settings.Default.AppName,
                                MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }

                        Mouse.OverrideCursor = Cursors.Wait;
                        job.Username = jobList.JenkinsUsername;
                        job.ApiToken = jobList.JenkinsApiToken;
                        await job.Rename(inputBox.TextValue.Trim());
                        ShowMessage($"Jenkins job has been renamed to {job.Name}");


                    }
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

        private async void OnDuplicateJob(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (!CheckJobList(jobList))
                {
                    return;
                }


                // ask for the new name first
                var inputBox = new InputBox();
                var jobToDuplicate = e.JenkinsNodes[0];
                inputBox.MessageText = "Please enter a new name for the duplicated Job";
                inputBox.TextValue = e.JenkinsNodes[0].Name;
                inputBox.InputBoxTitle = "Duplicate Jenkins Job";
                inputBox.Owner = this;

                if ((bool) inputBox.ShowDialog())
                {
                    if (jobList.JobExists(inputBox.TextValue))
                    {
                        MessageBox.Show(this, $"The Job name \"{inputBox.TextValue}\" already exists",
                            Settings.Default.AppName,
                            MessageBoxButton.OK, MessageBoxImage.Stop);
                        return;
                    }

                    Mouse.OverrideCursor = Cursors.Wait;

                    // get the source job config xml
                    var configXml = await jobToDuplicate.GetConfigXml();
                    var newJob = new JenkinsNode
                    {
                        State = JobState.New,
                        Name = inputBox.TextValue,
                        Username = jobList.JenkinsUsername,
                        ApiToken = jobList.JenkinsApiToken,
                        Url = $"{jobList.JenkinsUrl}job/{inputBox.TextValue}/",
                        Selected = true
                    };

                    newJob.LocalConfigFilePath = GetTemporaryConfigXmlFilePath(newJob);

                    File.WriteAllText(newJob.LocalConfigFilePath, configXml);

                    jobList.AddNewJob(newJob);
                    _watcher.JenkinsNodesToWatch.Add(newJob);
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

        private async void OnSaveJobConfig(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobsToSave = e.JenkinsNodes;

                if (jobsToSave.Count == 1)
                {
                    var dialogue = new SaveFileDialog();
                    dialogue.Filter = "XML File(*.xml)|*.xml|All(*.*)|*";
                    dialogue.FileName = jobsToSave[0].Name;
                    var result = dialogue.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        File.WriteAllText(dialogue.FileName, await jobsToSave[0].GetConfigXml());
                        Mouse.OverrideCursor = null;
                        ShowMessage($"Jenkins Config file has been saved to \r\n \"{dialogue.FileName}\"");
                    }
                }
                else
                {
                    var dialogue = new FolderBrowserDialog();
                    dialogue.Description = "Please select a directory where the Jenkins jobs will be saved.";
                    var result = dialogue.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        foreach (var job in jobsToSave)
                        {
                            var filePath = Path.Combine(dialogue.SelectedPath, $"{job.Name}.xml");
                            File.WriteAllText(filePath, await job.GetConfigXml());
                        }
                        Mouse.OverrideCursor = null;
                        ShowMessage($"Jenkins Job config files saved to \"{dialogue.SelectedPath}\"");
                    }
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

        private async void OnBuildJob(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;
                if (!CheckJobList(jobList))
                {
                    return;
                }


                if (e.JenkinsNodes.Count == 0)
                {
                    MessageBox.Show("Please select at least one updated Jenkins Job.", Settings.Default.AppName,
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                var sbJobList = new StringBuilder();
                string warningMsg = null;
                foreach (var job in e.JenkinsNodes)
                {
                    if (job.Color != "disabled")
                    {
                        // get the job detail to get parameters
                        var jenkinsServer = new JenkinsServer
                        {
                            JenkinsUrl = jobList.JenkinsUrl,
                            Username = jobList.JenkinsUsername,
                            ApiToken = jobList.JenkinsApiToken
                        };

                        var jobDetail = await jenkinsServer.GetJobDetails(job.Name);
                        sbJobList.AppendLine(job.Name);
                        await job.RunBuild(jobList.JenkinsJobAuthToken, jobDetail.Parameters);
                    }
                    else
                    {
                        warningMsg = "Job(s) were queued but there is at least one disabled job sent to the build queue.\r\n";
                        warningMsg += "Disabled job(s) will be ignored.";
                    }
                }

                if (!string.IsNullOrEmpty(warningMsg))
                {
                    ShowMessage(warningMsg, MessageType.Warning);;
                }
                else
                {
                    ShowMessage($"Queued the following Jenkins job(s) successfully.\r\n{sbJobList}");
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

        private void SaveJenkinsCredential(JobList jobList)
        {
            // if _jenkinsApiCredential is null, too early to do this
            if (_jenkinsApiCredentials == null)
                return;

            if (string.IsNullOrEmpty(jobList.JenkinsUrl))
                return;

            var jenkinsUri = new Uri(jobList.JenkinsUrl);
            string jenkinsBaseUrl = "";
            if (jenkinsUri.Port == 80 || jenkinsUri.Port == 443)
            {
                jenkinsBaseUrl = $"{jenkinsUri.Scheme}://{jenkinsUri.DnsSafeHost}/";
            }
            else
            {
                jenkinsBaseUrl = $"{jenkinsUri.Scheme}://{jenkinsUri.DnsSafeHost}:{jenkinsUri.Port}/";
            }
            
            if (!_jenkinsApiCredentials.ContainsKey(jenkinsBaseUrl))
            {
                var cred = new JenkinsCredentialPair
                {
                    ApiToken = jobList.JenkinsApiToken,
                    Username = jobList.JenkinsUsername
                };
                _jenkinsApiCredentials.Add(jenkinsBaseUrl, cred);

                // Add the new URL to both panes
                if (jobList.Pane == JenkinsPane.Left)
                {
                    RightJenkinsJobs.UrlHistory?.Add(jenkinsBaseUrl);
                }
                else
                {
                    LeftJenkinsJobs.UrlHistory?.Add(jenkinsBaseUrl);
                }
            }
            else
            {
                //update username and API token
                var item = _jenkinsApiCredentials[jenkinsBaseUrl];
                item.Username = jobList.JenkinsUsername;
                item.ApiToken = jobList.JenkinsApiToken;
            }



            var json = JsonConvert.SerializeObject(_jenkinsApiCredentials);
            Settings.Default.JenkinsApiCredentials = json;
            Settings.Default.Save();
        }

        private async void OnUrlRefresh(object sender, RoutedEventArgs e)
        {
            var jobList = sender as JobList;
            if (!CheckJobList(jobList))
            {
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // First, remove the jobs that's about to be replaced from the watch list.
                if (jobList.JenkinsNodes != null)
                {
                    _watcher.JenkinsNodesToWatch.RemoveRange(jobList.JenkinsNodes);
                }

                //If the URL doesn't exist in the dictionary, make sure the API token is wiped.
                if (_jenkinsApiCredentials != null && !_jenkinsApiCredentials.ContainsKey(jobList.JenkinsBaseUrl))
                {
                    jobList.JenkinsApiToken = string.Empty;
                }
                else
                {
                    var cred = GetApiCredential(jobList.JenkinsBaseUrl);
                    jobList.JenkinsUsername = cred?.Item1;
                    jobList.JenkinsApiToken = cred?.Item2;
                }

                var jenkinsServer = new JenkinsServer
                {
                    JenkinsUrl = jobList.JenkinsUrl,
                    Username = jobList.JenkinsUsername,
                    ApiToken = jobList.JenkinsApiToken
                };

                jobList.JenkinsNodes = await jenkinsServer.GetJenkinsNodes();
                jobList.JenkinsVersion = await jenkinsServer.GetVersion();

                // Need to update the job watch list
                _watcher.JenkinsNodesToWatch.AddRange(jobList.JenkinsNodes);

                SaveJenkinsCredential(jobList);
            }
            catch (HttpRequestException exp)
            {
                string msg = $"Error occurred when fetching Jenkins job data. Please check the following.\r\n\r\n";
                msg += $"Jenkins URL: {jobList.JenkinsUrl}\r\n";
                msg += $"Username: {jobList.JenkinsUsername}\r\n";
                msg += $"API Token: {jobList.JenkinsApiToken}\r\n\r\n";
                msg += $"Actual error message: {exp.Message}";

                MessageBox.Show(msg, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void mnuSwitchPanes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var leftJobList = pnlLeft.Children[0] as JobList;
                var rightJobList = pnlRight.Children[0] as JobList;

                pnlLeft.Children.Remove(leftJobList);
                pnlRight.Children.Remove(rightJobList);

                // Switch names
                var tmpLeftName = leftJobList.Name;

                leftJobList.Name = rightJobList.Name;
                rightJobList.Name = tmpLeftName;

                // Add the controls back to the DockPanel
                pnlLeft.Children.Add(rightJobList);
                pnlRight.Children.Add(leftJobList);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void OnJenkinsApiCredentialChanged(object sender, RoutedEventArgs e)
        {
            var jobList = sender as JobList;
            if (jobList != null)
            {
                if (!string.IsNullOrEmpty(jobList.JenkinsUrl) && !string.IsNullOrEmpty(jobList.JenkinsApiToken) &&
                    !string.IsNullOrEmpty(jobList.JenkinsUsername))
                {
                    if (_jenkinsApiCredentials == null)
                    {
                        _jenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
                    }

                    SaveJenkinsCredential(jobList);
                }
            }
        }

        private async void OnEnableJobs(object sender, JobActionEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var jobList = sender as JobList;

                if (e.JenkinsNodes == null || e.JenkinsNodes.Count == 0)
                {
                    return;
                }

                var tasks = new Task[e.JenkinsNodes.Count];
                for (var i = 0; i < e.JenkinsNodes.Count; i++)
                {
                    var job = e.JenkinsNodes[i];
                    tasks[i] = job.EnableDisable(true);
                    
                }

                await Task.WhenAll(tasks);

                if (jobList.Pane == JenkinsPane.Left)
                {
                    LoadLeftPane();
                }
                else
                {
                    LoadRightPane();
                }

                ShowMessage($"{tasks.Length} Jenkins job(s) enabled successfully");
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

        private void OnRunTestBuild(object sender, JobActionEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                if (e.JenkinsNodes == null || e.JenkinsNodes.Count == 0)
                {
                    return;
                }

                var tasks = new Task[e.JenkinsNodes.Count];
                for (var i = 0; i < e.JenkinsNodes.Count; i++)
                {
                    var job = e.JenkinsNodes[i];
                    tasks[i] = job.RunTestBuild();
                }

                Task.WhenAll(tasks);

                ShowMessage($"{tasks.Length} Jenkins job(s) queued for Test Build successfully");

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

        private async void OnDisableJobs(object sender, JobActionEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var jobList = sender as JobList;

                if (e.JenkinsNodes == null || e.JenkinsNodes.Count == 0)
                {
                    return;
                }

                var tasks = new Task[e.JenkinsNodes.Count];

                for (var i = 0; i < e.JenkinsNodes.Count; i++)
                {
                    var job = e.JenkinsNodes[i];
                    tasks[i] = job.EnableDisable(false);   
                }

                await Task.WhenAll(tasks);

                //refresh the pane
                if (jobList.Pane == JenkinsPane.Left)
                {
                    LoadLeftPane();
                }
                else
                {
                    LoadRightPane();
                }

                ShowMessage($"{tasks.Length} Jenkins job(s) disabled successfully");
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

        private void lblNotification_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _messageAnimator.Begin();
        }

        private delegate Task LocalAsyncTask();

        private Timer _timer;

        private void tbAutoRefreshPanes_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer = new Timer {Interval = Settings.Default.AutoRefreshInterval * 1000};
                _timer.Tick += (o, args) =>
                {
                    LoadPanes();
                    ShowMessage("Panes Auto Refreshed");
                };

                _timer.Start();

                ShowMessage($"Auto Refresh Panes Started\r\nInterval {Settings.Default.AutoRefreshInterval} seconds");
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        private void tbAutoRefreshPanes_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                _timer.Stop();
                ShowMessage("Auto Refresh Panes Stopped");
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        private void OnShowMessage(object sender, MessageEventArgs e)
        {
            ShowMessage(e.Message, e.MessageType);
        }

        public static readonly DependencyProperty LastUpdatedDateTimeProperty =
            DependencyProperty.Register("LastUpdatedDateTime",
                typeof(string), typeof(JobList));

        public string LastUpdatedDateTime
        {
            get { return (string) GetValue(LastUpdatedDateTimeProperty); }
            set { SetValue(LastUpdatedDateTimeProperty, value); }
        }

        private void OnBuildHistory(object sender, JobActionEventArgs e)
        {
            try
            {
                var jobList = sender as JobList;

                if (e.JenkinsNodes != null && e.JenkinsNodes.Count > 0 && jobList != null)
                {
                    foreach (var job in e.JenkinsNodes)
                    {
                        var buildHistoryWindow = new BuildHistoryWindow(job, jobList.JenkinsUsername, jobList.JenkinsApiToken);
                        buildHistoryWindow.Show();
                    }
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private enum SplitterPosition
        {
            Left,
            Center,
            Right
        }

        private SplitterPosition _nextSplitterPosition = SplitterPosition.Right;
        private SplitterPosition _previousPosition = SplitterPosition.Center;
        private void btnFullScreen_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (_nextSplitterPosition)
                {
                    case SplitterPosition.Center:
                        colLeft.Width = new GridLength(ActualWidth / 2);
                        if (_previousPosition == SplitterPosition.Left)
                        {
                            _nextSplitterPosition = SplitterPosition.Right;
                        }
                        else
                        {
                            _nextSplitterPosition = SplitterPosition.Left;
                        }
                        break;

                    case SplitterPosition.Left:
                        colLeft.Width = new GridLength(0);
                        _nextSplitterPosition = SplitterPosition.Center;
                        _previousPosition = SplitterPosition.Left;
                        break;
                    case SplitterPosition.Right:
                        colLeft.Width = new GridLength(ActualWidth - 40);
                        _nextSplitterPosition = SplitterPosition.Center;
                        _previousPosition = SplitterPosition.Right;
                        break;
                }
                
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private Size getElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
            {
                source = new HwndSource(new HwndSourceParameters());
                transformToDevice = source.CompositionTarget.TransformToDevice;
            }

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        private async void OnCreateFolder(object sender, MessageEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var jobList = sender as JobList;
                if (jobList != null)
                {
                    var folderName = e.Message;
                    var jenkinsServer = new JenkinsServer
                    {
                        JenkinsUrl = jobList.JenkinsBaseUrl,
                        ApiToken = jobList.JenkinsApiToken,
                        Username = jobList.JenkinsUsername
                    };

                    await jenkinsServer.CreateFolder(jobList.JenkinsUrl, folderName);

                    var jenkinsNode = new JenkinsNode
                    {
                        //LocalConfigFilePath = file,
                        _class = "com.cloudbees.hudson.plugins.folder.Folder",
                        State = JobState.Orginal,
                        Name = e.Message,
                        Url = $"{jobList.JenkinsUrl}job/{folderName}/",
                        Selected = true,
                        Username = jobList.JenkinsUsername,
                        ApiToken = jobList.JenkinsApiToken
                    };

                    jobList.AddNewJob(jenkinsNode);
                    ShowMessage($"New folder \"{folderName}\" created successfully");
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

        private void btnCompareAllJobContents_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {

            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void btnSearchJob_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                showSearchWindow();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void showSearchWindow()
        {
            var searchWindow = new SearchWindow
            {
                LeftPaneUrl = LeftJenkinsJobs.JenkinsUrl,
                RightPaneUrl = RightJenkinsJobs.JenkinsUrl
            };
            searchWindow.Show();
        }


        private void OnXPathReplace(object sender, JobActionEventArgs e)
        {
            var jobList = sender as JobList;

            try
            {
                if (!Directory.Exists(_tempDirPath))
                    Directory.CreateDirectory(_tempDirPath);

                e.JenkinsNodes.PropagateCredential(jobList.JenkinsUsername, jobList.JenkinsApiToken);

                foreach (var jobToChange in e.JenkinsNodes)
                {
                    //await RefreshLocalXmlConfigFile(jobToChange);


                    //// Run the text editor process and forget about it...
                    //var t = new Thread(() =>
                    //{
                    //    var compareToolProcess = new Process
                    //    {
                    //        StartInfo =
                    //        {
                    //            FileName = Settings.Default.TextEditorExePath,
                    //            WindowStyle = ProcessWindowStyle.Normal,
                    //            Arguments = $"\"{jobToChange.LocalConfigFilePath}\""
                    //        }
                    //    };
                    //    compareToolProcess.Start();
                    //});

                    //t.Start();
                }
            }
            catch (HttpRequestException exp)
            {
                string msg = $"Jenkins Server returned an error. Please check the following.\r\n\r\n";
                msg += $"Jenkins URL: {jobList.JenkinsUrl}\r\n";
                msg += $"Username: {jobList.JenkinsUsername}\r\n";
                msg += $"API Token: {jobList.JenkinsApiToken}\r\n\r\n";
                msg += $"Actual error message: {exp.Message}";

                MessageBox.Show(msg, Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }
    }
}