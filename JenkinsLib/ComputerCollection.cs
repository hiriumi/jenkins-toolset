using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsLib
{
    public class ComputerCollection : ObservableCollection<Computer>
    {
        public int WindowsNodeCount
        {
            get { return this.Count(node => node.ComputerType == ComputerType.Windows); }
        }

        public int LinuxNodeCount
        {
            get { return this.Count(node => node.ComputerType == ComputerType.Linux); }
        }

        public int MacNodeCount
        {
            get { return this.Count(node => node.ComputerType == ComputerType.Mac); }
        }

        public int UnknownNodeCount
        {
            get { return this.Count(node => node.ComputerType == ComputerType.Unknown); }
        }
    }
}
