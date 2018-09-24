using System.Windows;
using JenkinsLib;

namespace JenkinsToolsetWpf.Controls
{
    public class JobActionEventArgs : RoutedEventArgs
    {
        private JenkinsNodeCollection _jenkinsNodes;

        public JenkinsNodeCollection JenkinsNodes
        {
            get { return _jenkinsNodes ?? (_jenkinsNodes = new JenkinsNodeCollection()); }
            set { _jenkinsNodes = value; }
        }
        public string TargetFolderUrl { get; set; }
        public string[] JobFiles { get; set; }
    }
}