using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace JenkinsLib
{
    public class CustomContractResolver : DefaultContractResolver
    {
        public CustomContractResolver()
        {
            PropertyMappings = new Dictionary<string, string>();
        }

        private Dictionary<string, string> PropertyMappings { get; }

        protected override string ResolvePropertyName(string propertyName)
        {
            string resolvedName;
            var resolved = PropertyMappings.TryGetValue(propertyName, out resolvedName);
            return resolved ? resolvedName : base.ResolvePropertyName(propertyName);
        }
    }
}