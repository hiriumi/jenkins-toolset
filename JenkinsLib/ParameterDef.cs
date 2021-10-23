using System;
using Newtonsoft.Json;

namespace JenkinsLib
{
    [Serializable]
    public class ParameterDef
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }

        [JsonConverter(typeof(ParameterConverter))]
        public string Value { get; set; }

        public string[] choices { get; set; }
    }
}