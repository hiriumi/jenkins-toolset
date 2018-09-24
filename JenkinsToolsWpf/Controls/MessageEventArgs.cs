using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JenkinsToolsetWpf.Controls
{
    public class MessageEventArgs : RoutedEventArgs
    {
        public string Message { get; set; }
        public MessageType MessageType { get; set; }
    }
}
