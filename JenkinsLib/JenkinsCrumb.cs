using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JenkinsLib
{
    public class JenkinsCrumb
    {
        public string _class { get; set; }

        [JsonProperty(PropertyName = "crumb")]
        public string Crumb { get; set; }

        [JsonProperty(PropertyName = "crumbRequestField")]
        public string CrumbRequestField { get; set; }
    }
}
