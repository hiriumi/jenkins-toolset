using System.Windows;
using JenkinsLib;
using System.Windows.Controls;

namespace JenkinsToolsetWpf.Controls
{
    public class ContextMenuEventArgs : RoutedEventArgs
    {
        private JenkinsNode _jenkinsNode;
        public JenkinsNode JenkinsNode
        {
            get { return _jenkinsNode ?? (_jenkinsNode = new JenkinsNode()); }
            set { _jenkinsNode = value; }
        }

        private ContextMenu _contextMenu;
        public ContextMenu ContextMenu
        {
            get { return _contextMenu;  }
            set { _contextMenu = value; }
        }

    }
}
