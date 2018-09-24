using System;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using JenkinsToolsetWpf.Properties;

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    /// <summary>
    ///     Interaction logic for Appearance.xaml
    /// </summary>
    public partial class Appearance
    {
        public Appearance()
        {
            InitializeComponent();
        }

        public override void SaveSettings()
        {
            try
            {
                Settings.Default.ItemMouseOverBackgroundColor = recItemMouseOverBackgroundColor.Fill.ToString();
                Settings.Default.OddRowColor = recOddRowColor.Fill.ToString();
                Settings.Default.EvenRowColor = recEvenRowColor.Fill.ToString();
                Settings.Default.ListViewSelectionMode = (System.Windows.Controls.SelectionMode)cboListViewSelectionMode.SelectedItem;
                Settings.Default.PreserveFilterText = chkPreserveFilterText.IsChecked.Value;
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void recItemMouseOverBackgroundColor_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var colorDialog = new ColorDialog();
                var result = colorDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var colorHexOrName = colorDialog.Color.IsNamedColor
                        ? colorDialog.Color.Name
                        : $"#{colorDialog.Color.Name}";
                    var color = (Color) ColorConverter.ConvertFromString(colorHexOrName);
                    recItemMouseOverBackgroundColor.Fill = new SolidColorBrush(color);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void recOddRowColor_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var colorDialog = new ColorDialog();
                var result = colorDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var colorHexOrName = colorDialog.Color.IsNamedColor
                        ? colorDialog.Color.Name
                        : $"#{colorDialog.Color.Name}";
                    var color = (Color) ColorConverter.ConvertFromString(colorHexOrName);
                    recOddRowColor.Fill = new SolidColorBrush(color);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }

        private void recEvenRowColor_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var colorDialog = new ColorDialog();
                var result = colorDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    var colorHexOrName = colorDialog.Color.IsNamedColor
                        ? colorDialog.Color.Name
                        : $"#{colorDialog.Color.Name}";
                    var color = (Color) ColorConverter.ConvertFromString(colorHexOrName);
                    recEvenRowColor.Fill = new SolidColorBrush(color);
                }
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
            }
        }
    }
}