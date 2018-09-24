using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JenkinsLib
{
    public class JenkinsNodesContractResolver : DefaultContractResolver
    {
        private Dictionary<string, string> PropertyMappings { get; set; }

        public JenkinsNodesContractResolver()
        {
            PropertyMappings = new Dictionary<string, string>
            {
                {"DisplayName", "displayName"},
                {"Icon", "icon"},
                {"ManualLaunchAllowed", "manualLaunchAllowed"},
                {"NumExecutors", "numExecutors"},
                {"Idle", "idle"},
                {"Offline", "offline"},
                {"OfflineCauseReason", "offlineCauseReason"},
                {"TemporarilyOffline", "temporarilyOffline"},
                {"NodeType", "monitorData.hudson.node_monitors.ArchitectureMonitor"}
            };
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            string resolvedName;
            var resolved = PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}