using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using JenkinsLib;
using JenkinsToolsetWpf.Controls;
using JenkinsToolsetWpf.Properties;
using Newtonsoft.Json;
using Clipboard = System.Windows.Clipboard;

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    /// <summary>
    ///     Interaction logic for Appearance.xaml
    /// </summary>
    public partial class Urls
    {
        public Urls()
        {
            InitializeComponent();
        }

        public override void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(JenkinsApiCredentials);
                Settings.Default.JenkinsApiCredentials = json;
                Settings.Default.Save();
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        public static readonly DependencyProperty JenkinsApiCredentialsProperty =
            DependencyProperty.Register("JenkinsApiCredentials",
                typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>), typeof(Urls));
        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get
            {
                return (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)
                    GetValue(JenkinsApiCredentialsProperty);
            }

            set
            {
                SetValue(JenkinsApiCredentialsProperty, value);
            }
        }


        private void ColumnHeader_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            

        }

        private void mnuCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstURLs.SelectedItem != null)
                {
                    var item = (KeyValuePair<string, JenkinsCredentialPair>)lstURLs.SelectedItem;
                    Clipboard.SetText(item.Key);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }

        }

        private void mnuDeleteUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstURLs.SelectedItem != null)
                {
                    var item = (KeyValuePair<string, JenkinsCredentialPair>)lstURLs.SelectedItem;
                    JenkinsApiCredentials.Remove(item.Key);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuCopyApiToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstURLs.SelectedItem != null)
                {
                    var item = (KeyValuePair<string, JenkinsCredentialPair>)lstURLs.SelectedItem;
                    Clipboard.SetText(item.Value.ApiToken);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuCopyUsername_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstURLs.SelectedItem != null)
                {
                    var item = (KeyValuePair<string, JenkinsCredentialPair>)lstURLs.SelectedItem;
                    Clipboard.SetText(item.Value.Username);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void mnuNavigateToUrl_Click(object sender, RoutedEventArgs e)
        {
            if (lstURLs.SelectedItem != null)
            {
                var item = (KeyValuePair<string, JenkinsCredentialPair>)lstURLs.SelectedItem;
                Process.Start(new ProcessStartInfo(item.Key));
            }

        }
    }
}