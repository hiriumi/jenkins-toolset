using System;
using System.Windows;
using JenkinsToolsetWpf.Properties;

namespace JenkinsToolsetWpf
{
    internal class ExceptionHandler
    {
        public static void Handle(Exception exp)
        {
            MessageBox.Show(exp.ToString(), Settings.Default.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            //var errorWindow = new ErrorWindow(exp);
            //errorWindow.ShowDialog();
        }
    }
}