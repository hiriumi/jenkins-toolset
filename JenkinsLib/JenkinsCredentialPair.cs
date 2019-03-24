using System;

namespace JenkinsLib
{
    [Serializable]
    public class JenkinsCredentialPair : ICloneable
    {
        public string Username { get; set; }
        public string ApiToken { get; set; }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}