using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using JenkinsLib;
using JenkinsToolsetWpf.Properties;


namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    /// <summary>
    ///     Interaction logic for General.xaml
    /// </summary>
    public partial class General
    {
        public General()
        {
            InitializeComponent();
        }

        public override void SaveSettings()
        {
            Settings.Default.LocalTempDirectory = ctlLocalTempDir.DialogueTextResult;
            Settings.Default.ValidateJenkinsJobXmlWellFormed = (bool) chkValidateXml.IsChecked;
            Settings.Default.AutoRefreshInterval = Convert.ToInt32(txtAutoRefreshInterval.Text);
            //Settings.Default.PreserveFilterText = chkPreserveFilterText.IsChecked.Value;
            Settings.Default.CleanUpOnExit = chkCleanUpOnExit.IsChecked.Value;
            Settings.Default.PreserveLocalXmlChanges = chkPreserveLocalXmlChanges.IsChecked.Value;
            Settings.Default.RetentionMinutes = Convert.ToInt32(txtRetentionMinutes.Text);

        }

        private void btnInitializeUserSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var configFilePath =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                var result =
                    MessageBox.Show($"Are you sure you would like to delete the following file?\r\n{configFilePath}",
                        Settings.Default.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                File.Delete(configFilePath);
                MessageBox.Show($"User setting file at {configFilePath} has been deleted successfully",
                    Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void btnOpenUserSettingsDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir =Path.GetDirectoryName(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                Process.Start(dir);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }


        public static readonly DependencyProperty JenkinsApiCredentialsProperty =
            DependencyProperty.Register("JenkinsApiCredentials", typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>), typeof(General));

        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get
            {
                return (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)GetValue(JenkinsApiCredentialsProperty);
            }
            set { SetValue(JenkinsApiCredentialsProperty, value); }
        }
    }
}