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

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    /// <summary>
    ///     Interaction logic for Appearance.xaml
    /// </summary>
    public partial class URLs
    {
        public URLs()
        {
            InitializeComponent();
        }

        //private void URLs_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(Settings.Default.JenkinsApiCredentials))
        //        {
        //            JenkinsApiCredentials =
        //                JsonConvert.DeserializeObject<ObservableConcurrentDictionary<string, JenkinsCredentialPair>>(
        //                    Settings.Default.JenkinsApiCredentials);
        //        }
        //        else
        //        {
        //            JenkinsApiCredentials = new ObservableConcurrentDictionary<string, JenkinsCredentialPair>();
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        ExceptionHandler.Handle(exp);
        //    }
        //}

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
                typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>), typeof(URLs));
        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get =>
                (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)
                GetValue(JenkinsApiCredentialsProperty);
            set => SetValue(JenkinsApiCredentialsProperty, value);
        }


        private void ColumnHeader_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            

        }

        private void mnuCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            

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
    }
}