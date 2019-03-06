using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JenkinsLib
{
    [Serializable]
    public class JenkinsNode : INotifyPropertyChanged, IJenkinsNode
    {
        private bool _selected;

        private JobState _state = JobState.Orginal;
        private string _color;

        public string ImageFileName
        {
            get
            {
                if (Color != null)
                {
                    var fileName = Color.Replace("blue", "green");
                    return $"../Images/{fileName}.gif";
                }
                return string.Empty;
            }
        }


        private string _nodeTypeImageFileName;
        public string NodeTypeImageFileName
        {
            get
            {
                switch (JenkinsNodeType)
                {
                    case JenkinsNodeType.ExternalJob:
                        _nodeTypeImageFileName = "../Images/externaljob.png";
                        break;
                    case JenkinsNodeType.MatrixProject:
                        _nodeTypeImageFileName = "../Images/externaljob.png";
                        break;
                    case JenkinsNodeType.MultiJobProject:
                        _nodeTypeImageFileName = "../Images/multijobproject.png";
                        break;
                    case JenkinsNodeType.WorkflowMultiBranchProject:
                        _nodeTypeImageFileName = "../Images/pipelinemultibranchproject.png";
                        break;
                    case JenkinsNodeType.MavenModuleSet:
                        _nodeTypeImageFileName = "../Images/mavenproject.png";
                        break;
                    case JenkinsNodeType.WorkflowJob:
                        _nodeTypeImageFileName = "../Images/pipelinejob.png";
                        break;
                    case JenkinsNodeType.FreeStyleProject:
                        _nodeTypeImageFileName = "../Images/freestyleproject.png";
                        break;
                    case JenkinsNodeType.Folder:
                        _nodeTypeImageFileName = "../Images/folder.png";
                        break;

                    case JenkinsNodeType.Unknown:
                        _nodeTypeImageFileName = "../Images/Question_32xLG.png";
                        break;
                }

                return _nodeTypeImageFileName;
            }
        }

        public string _class { get; set; }

        private JenkinsNodeType _jenkinsNodeType = JenkinsNodeType.Unknown;
        public JenkinsNodeType JenkinsNodeType
        {
            get
            {
                if (_jenkinsNodeType != JenkinsNodeType.Unknown)
                {
                    return _jenkinsNodeType;
                }

                switch (_class)
                {
                    case "hudson.model.ExternalJob":
                        _jenkinsNodeType = JenkinsNodeType.ExternalJob;
                        break;
                    case "hudson.matrix.MatrixProject":
                        _jenkinsNodeType = JenkinsNodeType.MatrixProject;
                        break;
                    case "com.tikal.jenkins.plugins.multijob.MultiJobProject":
                        _jenkinsNodeType = JenkinsNodeType.MultiJobProject;
                        break;
                    case "org.jenkinsci.plugins.workflow.multibranch.WorkflowMultiBranchProject":
                        _jenkinsNodeType = JenkinsNodeType.WorkflowMultiBranchProject;
                        break;
                    case "hudson.maven.MavenModuleSet":
                        _jenkinsNodeType = JenkinsNodeType.MavenModuleSet;
                        break;
                    case "org.jenkinsci.plugins.workflow.job.WorkflowJob":
                        _jenkinsNodeType = JenkinsNodeType.WorkflowJob;
                        break;
                    case "hudson.model.FreeStyleProject":
                        _jenkinsNodeType = JenkinsNodeType.FreeStyleProject;
                        break;
                    case "com.cloudbees.hudson.plugins.folder.Folder":
                        _jenkinsNodeType = JenkinsNodeType.Folder;
                        break;
                    default:
                        _jenkinsNodeType = JenkinsNodeType.FreeStyleProject;
                        break;
                }

                return _jenkinsNodeType;
            }
        }

        public string StatusIconUrl
        {
            get { return $"{Url}badge/icon"; }
        }

        public string Username { get; set; }
        public string ApiToken { get; set; }
        public string Name { get; set; }
        public string OriginalUrl { get; set; }
        public string Description { get; set; }

        public string Url { get; set; }

        public string ConfigureUrl => $"{Url}configure";

        private JenkinsServer _jenkinsServer;
        public JenkinsServer JenkinsServer
        {
            get
            {
                if (_jenkinsServer == null)
                {
                    _jenkinsServer = new JenkinsServer();
                }

                return _jenkinsServer;
            }
            set
            {
                _jenkinsServer = value;
            }
        }
        public bool Buildable { get; set; }

        public string DisplayNameOrNull { get; set; }

        public string Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
                OnPropertyChanged(nameof(ImageFileName));
            }
        }

        public bool InQueue { get; set; }

        public Build LastBuild { get; set; }

        public Build LastFailedBuild { get; set; }

        public Build LastStableBuild { get; set; }

        public Build LastSuccessfulBuild { get; set; }

        public int NextBuildNumber { get; set; }
        public List<ParameterDef> Parameters { get; set; }

        public List<JenkinsNode> DownSreamProjects { get; set; }

        public JobState State
        {
            get { return _state; }
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }

        public string LocalConfigFilePath { get; set; }

        public string OriginalConfigXml { get; set; }

        public string JenkinsDomain
        {
            get { return new Uri(Url).DnsSafeHost; }
        }

        public async Task<string> GetConfigXml(string username = "", string apiToken = "")
        {
            string configXml;

            if (!string.IsNullOrEmpty(username))
            {
                Username = username;
            }

            if (!string.IsNullOrEmpty(apiToken))
            {
                ApiToken = apiToken;
            }

            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var requestUrl = Url.EndsWith("/") ? $"{Url}config.xml" : $"{Url}/config.xml";
                var response = await client.GetStringAsync(requestUrl);
                configXml = response;
            }

            return configXml;
        }

        public async Task Update(string updatedJobXml)
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            var requestUrl = Url.EndsWith("/") ? $"{Url}config.xml" : $"{Url}/config.xml";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            request.Headers.Clear();
            request.Content = new StringContent(updatedJobXml, Encoding.UTF8, "application/xml");

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                //client.BaseAddress = new Uri(JenkinsBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

            OriginalConfigXml = updatedJobXml;
        }

        public async Task Rename(string newJobName)
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            var requestUrl = $"{Url}doRename?newName={newJobName}";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Clear();

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                
                var response = await client.SendAsync(request);
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("403"))
                    {
                        throw;
                    }

                }
                

            }

            Name = newJobName;

            // Update the URL. Update only the last part of the URI.
            var uri = new Uri(Url);

            Url = $"{uri.Scheme}://{uri.DnsSafeHost}";
            int i = 0;
            foreach (var s in uri.Segments)
            {
                if (i == uri.Segments.Length - 1)
                {
                    Url += $"{newJobName}/";
                }
                else
                {
                    Url += s;
                }
                i++;
            }

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Url));

        }

        public async Task RunTestBuild()
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            var requestUrl = $"{Url}testBuild";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

            request.Headers.Clear();

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }

        }

        public async Task<BuildCollection> GetBuilds()
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            var requestUrl = $"{Url}api/json?tree=builds[number,url,duration,icon,result,timestamp,displayname,builtOn,actions[causes[shortDescription,upstreamBuild,upstreamProject,upstreamUrl]]]";
            BuildCollection builds = null;

            using (var client = new HttpClient(new HttpClientHandler {UseDefaultCredentials = true}))
            {

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var jsonBuilds = await client.GetStringAsync(requestUrl);

                if (!string.IsNullOrEmpty(jsonBuilds))
                {
                    var responseObject = JsonConvert.DeserializeObject<JObject>(jsonBuilds);
                    var buildsToken = responseObject.SelectToken("$.builds");

                    builds = JsonConvert.DeserializeObject<BuildCollection>(buildsToken.ToString());
                }
            }

            return builds;
        }


        public async Task RunBuild(string authToken, List<ParameterDef> buildParams)
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            // construct build parameters
            NameValueCollection parameters = null;
            if (buildParams != null)
            {
                parameters = HttpUtility.ParseQueryString(string.Empty);
                foreach (var p in buildParams)
                {
                    parameters[p.Name] = p.Value;
                }
            }

            UriBuilder requestUrlBuilder;

            if (buildParams == null || buildParams.Count == 0)
            {
                if (string.IsNullOrEmpty(authToken))
                {
                    requestUrlBuilder = new UriBuilder($"{Url}build");
                }
                else
                {
                    requestUrlBuilder = new UriBuilder($"{Url}build?token={authToken}");
                }
                
            }
            else
            {
                requestUrlBuilder = new UriBuilder($"{Url}buildWithParameters");
                parameters["token"] = authToken;
                requestUrlBuilder.Query = parameters.ToString();
            }

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrlBuilder.ToString());
            

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task EnableDisable(bool enable)
        {
            var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
            var targetUrl = enable ? $"{Url}/enable" : $"{Url}/disable";
            var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
            request.Headers.Clear();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }

                try
                {
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    if (!e.Message.Contains("403"))
                    {
                        throw;
                    }
                }
                
            }
        }

        public bool ValidateJobConfigXml()
        {
            try
            {
                XDocument.Parse(File.ReadAllText(LocalConfigFilePath));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task Delete(string username = "", string apiToken = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(username))
                {
                    Username = username;
                }

                if (!string.IsNullOrEmpty(apiToken))
                {
                    ApiToken = apiToken;
                }

                var credentialToken = JenkinsServer.GetCredentialToken(Username, ApiToken);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{Url}doDelete");

                request.Headers.Clear();
                using (var client = new HttpClient())
                {
                    //client.BaseAddress = new Uri(jenkinsServerBaseUrl);
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                    var crumb = await JenkinsServer.IssueCrumb(Url, Username, ApiToken);
                    if (crumb != null)
                    {
                        client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                    }
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (HttpRequestException e)
            {
                //Jenkins redirects to the NewFolder/configure immediate, and it doesn't recognize the 
                //current user session, so it throws 403 (Unauthorized) after job/folder deletion is successful.
                //To get around the issue, I'm ignoring 403 exception for now. All other exceptions will be
                //thrown to the application.
                if (!e.Message.Contains("403"))
                {
                    throw;
                }
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}