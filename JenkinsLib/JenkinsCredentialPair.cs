using System;

namespace JenkinsLib
{
    [Serializable]
    public class JenkinsCredentialPair
    {
        public string Username { get; set; }
        public string ApiToken { get; set; }
    }
}