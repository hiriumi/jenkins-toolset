using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JenkinsLib
{
    public class Build
    {

        [JsonProperty(PropertyName = "actions")]
        public List<BuildAction> Actions { get; set; }
        public object[] artifacts { get; set; }

        [JsonProperty(PropertyName = "building")]
        public bool Building { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "duration")]
        public int Duration { get; set; }

        [JsonProperty(PropertyName = "estimatedDuration")]
        public int EstimatedDuration { get; set; }

        [JsonProperty(PropertyName = "executor")]
        public string Executor { get; set; }

        [JsonProperty(PropertyName = "fullDisplayName")]
        public string FullDisplayName { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "keepLog")]
        public bool KeepLog { get; set; }

        [JsonProperty(PropertyName = "number")]
        public int Number { get; set; }

        [JsonProperty(PropertyName = "queueId")]
        public int QueueId { get; set; }

        [JsonProperty(PropertyName = "result")]
        public string Result { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public string TimeStamp { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "builtOn")]
        public string BuiltOn { get; set; }

        private string _imageFileName;
        public string ImageFileName
        {
            get
            {
                switch (Result)
                {
                    case "SUCCESS":
                        _imageFileName = "../Images/green.gif";
                        break;
                    case "ABORTED":
                        _imageFileName = "../Images/grey.gif";
                        break;
                    case "FAILURE":
                        _imageFileName = "../Images/red.gif";
                        break;
                }

                return _imageFileName;
            }
            set { _imageFileName = value; }
        }

        public string DisplayDuration
        {
            get
            {
                var seconds = Duration / 1000;
                // if less than 1 min
                if (seconds < 60)
                {
                    return $"{seconds} seconds";
                }
                else
                {
                    return $"{seconds / 60} mins {seconds % 60} seconds";
                }
            }
        }

        public string UpstreamProject
        {
            get
            {
                var causeAction = Actions?.FirstOrDefault(a => a.Causes != null);
                var cause = causeAction.Causes.FirstOrDefault(c => !string.IsNullOrEmpty(c.upstreamProject));
                return cause?.upstreamProject;
            }
        }

        public DateTime TimeStampDateTime
        {
            get { return FromUnixTime(Convert.ToDouble(TimeStamp)); }
        }

        public static DateTime FromUnixTime(double unixTimeStamp)
        {
            var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return ret.AddMilliseconds(unixTimeStamp).ToLocalTime();
            //return ret.AddSeconds(unixTimeStamp).ToLocalTime();
        }
        public async Task Delete(string jenkinsUsername, string apiToken)
        {
            var credentialToken = JenkinsServer.GetCredentialToken(jenkinsUsername, apiToken);
            var requestUrl = $"{Url}doDelete";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            request.Headers.Clear();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

        }
    }

    public class BuildAction
    {

        [JsonProperty(PropertyName = "causes")]
        public List<Cause> Causes { get; set; }
    }

    public class Cause
    {
        public string shortDescription { get; set; }
        public string upstreamBuild { get; set; }
        public string upstreamProject { get; set; }
        public string upstreamUrl { get; set; }
    }

}