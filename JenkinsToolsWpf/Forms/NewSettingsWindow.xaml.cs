using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using JenkinsLib;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using JenkinsToolsetWpf.Forms.SettingsPages;
using JenkinsToolsetWpf.Properties;

namespace JenkinsToolsetWpf.Forms
{
    /// <summary>
    ///     Interaction logic for NewSettingsWindow.xaml
    /// </summary>
    public partial class NewSettingsWindow : Window
    {
        private readonly Dictionary<string, BaseSettingsPage> _optionPages;

        public NewSettingsWindow()
        {
            InitializeComponent();
            _optionPages = new Dictionary<string, BaseSettingsPage>();

            var ctlGeneral = new General();
            _optionPages.Add(ctlGeneral.GetType().Name, ctlGeneral);

            var ctlExternalTools = new ExternalTools();
            _optionPages.Add(ctlExternalTools.GetType().Name, ctlExternalTools);

            var ctlAppearance = new Appearance();
            _optionPages.Add(ctlAppearance.GetType().Name, ctlAppearance);

            var ctlUrls = new Urls();
            _optionPages.Add(ctlUrls.GetType().Name, ctlUrls);

            Resources["OptionPages"] = _optionPages;
        }

        private void NewSettingsWindows_Loaded(object sender, RoutedEventArgs e)
        {
            Urls ctlUrls = (Urls)_optionPages["Urls"];
            ctlUrls.JenkinsApiCredentials = JenkinsApiCredentials;

            if (string.IsNullOrEmpty(Settings.Default.OptionPageSelectedItem))
            {
                // Set the content to the first one in the list
                pnlMain.Content = _optionPages[_optionPages.Keys.First()];
                lstOptions.SelectedItem = lstOptions.Items[0];
            }
            else
            {
                foreach (ListBoxItem option in lstOptions.Items)
                {
                    if (Settings.Default.OptionPageSelectedItem == option.Name)
                    {
                        lstOptions.SelectedItem = option;
                        return;
                    }
                }
            }


        }

        private void lstOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstOptions.SelectedItem != null)
            {
                var item = lstOptions.SelectedItem as ListBoxItem;
                // Replace the panel content with the one selected
                pnlMain.Content = _optionPages[item.Name];

                // Keep it in the settings for next load
                Settings.Default.OptionPageSelectedItem = item.Name;
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Saving process here.
                foreach (var p in _optionPages)
                {
                    p.Value.SaveSettings();
                }

                Settings.Default.Save();
                DialogResult = true;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
            finally
            {
                Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        public static readonly DependencyProperty JenkinsApiCredentialsProperty =
            DependencyProperty.Register("JenkinsApiCredentials",
                typeof(ObservableConcurrentDictionary<string, JenkinsCredentialPair>), typeof(NewSettingsWindow));
        public ObservableConcurrentDictionary<string, JenkinsCredentialPair> JenkinsApiCredentials
        {
            get =>
                (ObservableConcurrentDictionary<string, JenkinsCredentialPair>)
                GetValue(JenkinsApiCredentialsProperty);
            set => SetValue(JenkinsApiCredentialsProperty, value);
        }

        private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // I hate to have to write code to bubble up the event just to select an item...
            // Please let me know if you know any better way to do accomplish this.
            var tb = sender as TextBlock;
            tb?.AddHandler(MouseLeftButtonDownEvent, new RoutedEventHandler((o, evnt) => { evnt.Handled = false; }));

            var img = sender as Image;
            img?.AddHandler(MouseLeftButtonDownEvent, new RoutedEventHandler((o, evnt) => { evnt.Handled = false; }));

            var wrp = sender as WrapPanel;
            if (wrp?.Parent is ListBoxItem)
            {
                var selectedItem = (ListBoxItem) wrp.Parent;
                lstOptions.SelectedItem = selectedItem;
            }
        }

    }
}