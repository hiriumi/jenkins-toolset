using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//test1
namespace JenkinsLib
{
    public class JenkinsServer : IJenkinsServer
    {
        private JenkinsCrumb _jenkinsCrumb;

        public string JenkinsBaseUrl
        {
            get
            {
                var uri = new Uri(JenkinsUrl);
                string baseUrl;
                if (uri.Port == 80 || uri.Port == 443)
                {
                    baseUrl = $"{uri.Scheme}://{uri.DnsSafeHost}/";
                }
                else
                {
                    baseUrl = $"{uri.Scheme}://{uri.DnsSafeHost}:{uri.Port}/";
                }
                return baseUrl;
            }
        }

        private bool CsrfEnabled { get; set; } = true;

        private string _jenkinsUrl;
        public string JenkinsUrl {
            get
            {
                if (_jenkinsUrl.EndsWith("/"))
                {
                    return _jenkinsUrl;
                }
                else
                {
                    _jenkinsUrl += "/";
                    return _jenkinsUrl;
                }
            }
            set
            {
                _jenkinsUrl = value;
            }
        }

        public string JenkinsVersion { get; set; }
        public string Username { get; set; }
        public string ApiToken { get; set; }
        public NetworkCredential Credential { get; set; }
        public async Task<ComputerCollection> GetAllComputers()
        {
            var credentialToken = GetCredentialToken(Username, ApiToken);
            string jsonData;
            ComputerCollection computers = null;

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.BaseAddress = new Uri(JenkinsBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var response = await client.GetStringAsync($"{JenkinsBaseUrl}computer/api/json");
                jsonData = response;
            }

            if (!string.IsNullOrEmpty(jsonData))
            {
                var responseObject = JsonConvert.DeserializeObject<JObject>(jsonData);
                var computerToken = responseObject.SelectToken("$.computer");
                computers = JsonConvert.DeserializeObject<ComputerCollection>(computerToken.ToString());
            }

            return computers;
        }
        public async Task CreateNewJob(string folderUrl, string jobName, string jobXml, string username, string apiToken)
        {
            var credentialToken = GetCredentialToken(username, apiToken);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri($"{folderUrl}createItem?name={jobName}");
            request.Headers.Clear();
            request.Content = new StringContent(jobXml, Encoding.UTF8, "application/xml");
            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var uri = new Uri(folderUrl);
                var crumb = await IssueCrumb($"{uri.Scheme}://{uri.Host}/", username, apiToken);

                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task CreateNewJob(string jobName, string jobXml)
        {
            var credentialToken = GetCredentialToken(Username, ApiToken);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri($"{JenkinsUrl}createItem?name={jobName}");
            request.Headers.Clear();
            request.Content = new StringContent(jobXml, Encoding.UTF8, "application/xml");
            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var crumb = await IssueCrumb(JenkinsBaseUrl, Username, ApiToken);

                if (crumb != null)
                {
                    client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                }
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<JenkinsNode> GetJobDetails(string jobName)
        {
            string jsonJob;
            JenkinsNode jenkinsNode = null;
            var credentialToken = GetCredentialToken(Username, ApiToken);

            // if (!UrlExists())
            string url = $"{JenkinsUrl}job/{jobName}/api/json";
            bool urlExists = await UrlExists(url, Username, ApiToken);
            if (!urlExists)
            {
                return null;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var response = await client.GetStringAsync($"{JenkinsUrl}job/{jobName}/api/json");
                jsonJob = response;
            }

            if (!string.IsNullOrEmpty(jsonJob))
            {
                jenkinsNode = JsonConvert.DeserializeObject<JenkinsNode>(jsonJob);

                var responseObject = JsonConvert.DeserializeObject<JObject>(jsonJob);
                var parameterToken = responseObject.SelectToken("$.actions[0].parameterDefinitions");

                if (parameterToken != null)
                    jenkinsNode.Parameters = JsonConvert.DeserializeObject<List<ParameterDef>>(parameterToken.ToString(),new ParameterConverter());
            }

            return jenkinsNode;
        }

        public async Task<string> GetSchema()
        {
            string jenkinsSchema;

            using (var client = new HttpClient())
            {
                jenkinsSchema = await client.GetStringAsync($"{JenkinsUrl}/api/schema");
            }

            return jenkinsSchema;
        }

        public async Task<JenkinsCrumb> IssueCrumb(string jenkinsBaseUrl, string username, string apiToken)
        {
            if (!CsrfEnabled)
                return null;

            if (_jenkinsCrumb != null)
                return _jenkinsCrumb;

            var credentialToken = GetCredentialToken(username, apiToken);
            string jsonData;

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                try
                {
                    //client.BaseAddress = uri;
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                    var uri = new Uri(jenkinsBaseUrl);
                    var response =
                        await client.GetStringAsync($"{uri.Scheme}://{uri.DnsSafeHost}/crumbIssuer/api/json");
                    jsonData = response;
                }
                catch (HttpRequestException)
                {
                    CsrfEnabled = false;
                    return null;
                }
            }

            if (!string.IsNullOrEmpty(jsonData))
                if (_jenkinsCrumb != null)
                    lock (_jenkinsCrumb)
                    {
                        _jenkinsCrumb = JsonConvert.DeserializeObject<JenkinsCrumb>(jsonData);
                    }
                else
                    _jenkinsCrumb = JsonConvert.DeserializeObject<JenkinsCrumb>(jsonData);


            return _jenkinsCrumb;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceUrl">URL of the folder that </param>
        /// <param name="destinationRootUrl"></param>
        /// <param name="sourceJenkinsUsername">If Username property is set at constructor, this parameter is not necessary.</param>
        /// <param name="sourceJenkinsApiToken">If ApiToken property is set at constructor, this parameter is not necessary.</param>
        /// <param name="targetJenkinsUsername"></param>
        /// <param name="targetJenkinsApiToken"></param>
        /// <returns></returns>
        public async Task CopyFolder(string sourceUrl, string destinationRootUrl,
            string targetJenkinsUsername,
            string targetJenkinsApiToken,
            string sourceJenkinsUsername = "",
            string sourceJenkinsApiToken = "")
        {
            if (!string.IsNullOrEmpty(sourceJenkinsUsername))
            {
                Username = sourceJenkinsUsername;
            }

            if (!string.IsNullOrEmpty(sourceJenkinsApiToken))
            {
                ApiToken = sourceJenkinsApiToken;
            }

            // 
            var sourceUri = new Uri(sourceUrl);
            var sourceFolderName = sourceUri.Segments[sourceUri.Segments.Length - 1].Replace("/", string.Empty);
            
            Debug.WriteLine($"sourceFolderName: {sourceFolderName}");

            //Does the destination folder exist?
            var urlOk = await UrlExists(destinationRootUrl, targetJenkinsUsername, targetJenkinsApiToken);
            if (!urlOk)
            {
                throw new Exception($"Destination folder \"{destinationRootUrl}\" does not exist.");
            }

            // Is the destination Url a folder or the root?
            var destUri = new Uri(destinationRootUrl);
            var destinationFolder = await GetJenkinsNode(destinationRootUrl, targetJenkinsUsername, targetJenkinsApiToken);
            if ($"{destUri.Scheme}://{destUri.Host}/" == destinationRootUrl ||
                destinationFolder.JenkinsNodeType == JenkinsNodeType.Folder)
            {
                var destUrl = $"{destinationRootUrl}job/{sourceFolderName}";
                var destFolderExists = await UrlExists(destUrl, targetJenkinsUsername, targetJenkinsApiToken);

                if (!destFolderExists)
                {
                    // Create the folder at destination because it doesn't exist.
                    await CreateFolder(destinationRootUrl, sourceFolderName, targetJenkinsUsername, targetJenkinsApiToken);
                }

                // The destination folder is ready at this point, so get all the jobs and folders
                // under the source folder

                var nodesToCopy = await GetJenkinsNodesRecurse(sourceUrl, sourceJenkinsUsername, sourceJenkinsApiToken);

                // Create all folders first if any
                foreach (var folderNode in nodesToCopy.Where(n => n.JenkinsNodeType == JenkinsNodeType.Folder))
                {
                    var newTargetFolder = folderNode.Url.Replace(sourceUrl, "");
                    newTargetFolder = $"{destinationRootUrl}job/{sourceFolderName}/{newTargetFolder}";

                    // Does the target folder exist? If not, create it.
                    var folderExists = await UrlExists(newTargetFolder, targetJenkinsUsername, targetJenkinsApiToken);

                    if (!folderExists)
                    {
                        var folderName = GetFolderName(newTargetFolder);
                        var oneLevelUpUrl = GetJenkinsUpperLevelUrl(newTargetFolder, 1);
                        await CreateFolder(oneLevelUpUrl, folderName, targetJenkinsUsername, targetJenkinsApiToken);
                    }
                }

                // Folder creation completed, so copy the jobs to the appropriate target folders.
                foreach (var jobToCopy in nodesToCopy.Where(n => n.JenkinsNodeType != JenkinsNodeType.Folder))
                {
                    Debug.WriteLine(jobToCopy.Url);
                    var sourceFolderContainingJob = GetJenkinsUpperLevelUrl(jobToCopy.Url, 1);
                    var sourceUrlOneLevelUp = GetJenkinsUpperLevelUrl(sourceUrl, 1);

                    var targetFolderUrl = sourceFolderContainingJob.Replace(sourceUrlOneLevelUp, "");
                    targetFolderUrl = destinationRootUrl + targetFolderUrl;
                    // source job and destination folder has been determined.


                    var targetJobUrl = targetFolderUrl + jobToCopy.Name;
                    var targetJobExists = await UrlExists(targetJobUrl, targetJenkinsUsername, targetJenkinsApiToken);

                    var jobXml = await jobToCopy.GetConfigXml(sourceJenkinsUsername, sourceJenkinsApiToken);

                    if (targetJobExists)
                    {
                        // the target job exists, so update it.
                        var targetJob = await GetJenkinsNode(targetJobUrl, targetJenkinsUsername, targetJenkinsApiToken);
                        await targetJob.Update(jobXml);
                    }
                    else
                    {
                        await CreateNewJob(targetFolderUrl, jobToCopy.Name, jobXml, targetJenkinsUsername,
                            targetJenkinsApiToken);
                    }
                }
            }
            else
            {
                throw new Exception("Destination type must be a folder.");
            }
        }

        public static string GetFolderName(string originalUrl)
        {
            var uri = new Uri(originalUrl);
            return uri.Segments[uri.Segments.Length - 1].Replace("/", "");
        }

        public static string GetJenkinsUpperLevelUrl(string originalUrl, int level)
        {
            var uri = new Uri(originalUrl);
            var result = new StringBuilder($"{uri.Scheme}://{uri.Host}");

            for (int i = 0; i < uri.Segments.Length - level*2; i++)
            {
                result.Append(uri.Segments[i]);
            }

            return result.ToString();
        }

        public async Task<bool> UrlExists(string url, string username, string apiToken)
        {
            var credentialToken = GetCredentialToken(username, apiToken);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Head,
                RequestUri = new Uri(url)
            };
            request.Headers.Clear();
            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
        }

        public async Task CreateFolder(string url, string folderName, string username = "", string apiToken = "")
        {
            if (!string.IsNullOrEmpty(username))
            {
                Username = username;
            }

            if (!string.IsNullOrEmpty(apiToken))
            {
                ApiToken = apiToken;
            }
            
            var credentialToken = "";

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken))
            {
                credentialToken = GetCredentialToken(Username, ApiToken);
            }

            using (var client = new HttpClient(new HttpClientHandler {UseDefaultCredentials = true}))
            {
                try
                {
                    client.BaseAddress = new Uri(url);
                    client.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(credentialToken))
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                    }

                    var uri = new Uri(url);

                    var crumb = await IssueCrumb($"{uri.Scheme}://{uri.Host}/", Username, ApiToken);

                    if (crumb != null)
                    {
                        client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);
                    }

                    var requestUrl = url.EndsWith("/") ? url + "createItem" : url + "/createItem";

                    var parameters = new Dictionary<string, string>
                    {
                        {"name", folderName},
                        {"mode", "com.cloudbees.hudson.plugins.folder.Folder"},
                        {"Submit", "OK"}
                    };

                    var content = new FormUrlEncodedContent(parameters);
                    var response = await client.PostAsync(requestUrl, content);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException e)
                {
                    //Jenkins redirects to the NewFolder/configure immediate, and it doesn't recognize the 
                    //current user session, so it throws 403 (Forbidden) after folder creation is successful.
                    //To get around the issue, I'm ignoring 403 exception for now. All other exceptions will be
                    //thrown to the application.
                    if (!e.Message.Contains("403"))
                    {
                        throw;
                    }
                }

            }
        }

        JenkinsNodeCollection _jenkinsNodes;
        public async Task<JenkinsNodeCollection> GetJenkinsNodesRecurse(string url, string username = "", string apiToken = "")
        {
            Username = username;
            ApiToken = apiToken;

            var credentialToken = "";

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(apiToken))
            {
                credentialToken = GetCredentialToken(username, apiToken);
            }

            string jsonJobs;
            

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(credentialToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                }
                var response = await client.GetStringAsync($"{url}api/json/tree=jobs[name,description,url,buildable]");
                jsonJobs = response;
            }

            if (!string.IsNullOrEmpty(jsonJobs))
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CustomContractResolver()
                };

                var responseObject = JsonConvert.DeserializeObject<JObject>(jsonJobs, settings);
                var jobsToken = responseObject.SelectToken("$.jobs");

                if (jobsToken != null)
                {
                    //Initialize _jenkinsNodes collection if it's the first execution of the 
                    //recursive function.
                    if (_jenkinsNodes == null || !_jenkinsNodes.Any())
                    {
                        _jenkinsNodes = new JenkinsNodeCollection();
                    }
                    var nodes = JsonConvert.DeserializeObject<JenkinsNodeCollection>(jobsToken.ToString());
                    if (nodes != null)
                    {
                        _jenkinsNodes.AddRange(nodes);

                        if (_jenkinsNodes.JenkinsServer == null)
                        {
                            _jenkinsNodes.JenkinsServer = this;
                        }

                        var folderNodes = nodes.Where(node => node.JenkinsNodeType == JenkinsNodeType.Folder);
                        if (folderNodes.Any())
                        {
                            foreach (var folderNode in folderNodes)
                            {
                                await GetJenkinsNodesRecurse(folderNode.Url, username, apiToken);
                            }
                        }
                    }
                }


            }

            return _jenkinsNodes;
        }

        public void ClearJenkinsNodes()
        {
            _jenkinsNodes?.Clear();
        }
        public async Task<JenkinsNodeCollection> SearchJenkinsJobs(string url, string regex, string username = "", string apiToken = "")
        {

            if (!string.IsNullOrEmpty(username))
            {
                Username = username;
            }

            if (!string.IsNullOrEmpty(apiToken))
            {
                ApiToken = apiToken;
            }

            var credentialToken = "";
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken))
            {
                credentialToken = GetCredentialToken(Username, ApiToken);
            }

            string jsonJobs;

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(credentialToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                }
                var response = await client.GetStringAsync($"{url}api/json?tree=jobs[name,url,jobs[name,url,jobs[name,url]]]");
                jsonJobs = response;
            }

            IDictionary result = null;
            if (!string.IsNullOrEmpty(jsonJobs))
            {
                result = (IDictionary) JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonJobs, new DictionaryConverter());
            }

            var jobs = (List<object>) result["jobs"];
            foreach (Dictionary<string, object> job in jobs)
            {
                Debug.WriteLine($"{job["name"]}: {job["url"]}");
            }

            return null;

        }

        public async Task<JenkinsNode> GetJenkinsNode(string url, string username = "", string apiToken = "")
        {
            if (!string.IsNullOrEmpty(username))
            {
                Username = username;
            }

            if (!string.IsNullOrEmpty(apiToken))
            {
                ApiToken = apiToken;
            }

            var credentialToken = "";
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken))
            {
                credentialToken = GetCredentialToken(Username, ApiToken);
            }

            string jsonJob;
            JenkinsNode jenkinsNode = null;

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.BaseAddress = new Uri(JenkinsUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(credentialToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                }

                var jsonUrl = url.EndsWith("/") ? $"{url}api/json" : $"{url}/api/json";
                var response = await client.GetStringAsync(jsonUrl);
                jsonJob = response;
            }

            if (!string.IsNullOrEmpty(jsonJob))
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new CustomContractResolver();
                var responseObject = JsonConvert.DeserializeObject<JObject>(jsonJob, settings);
                jenkinsNode = JsonConvert.DeserializeObject<JenkinsNode>(responseObject.ToString());
            }

            return jenkinsNode;
        }

        public async Task<JenkinsNodeCollection> GetJenkinsNodes(string username = "", string apiToken = "", bool recurse = false)
        {
            if (!string.IsNullOrEmpty(username))
            {
                Username = username;
            }

            if (!string.IsNullOrEmpty(apiToken))
            {
                ApiToken = apiToken;
            }
            
            var credentialToken = "";

            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken))
            {
                credentialToken = GetCredentialToken(Username, ApiToken);
            }

            string jsonJobs;
            JenkinsNodeCollection jenkinsNodes = null;

            using (var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
            {
                client.BaseAddress = new Uri(JenkinsUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(credentialToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentialToken}");
                }
                var response = await client.GetStringAsync($"{JenkinsUrl}api/json/tree=jobs[name,description,url,buildable]");
                jsonJobs = response;
            }

            if (!string.IsNullOrEmpty(jsonJobs))
            {
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new CustomContractResolver();
                var responseObject = JsonConvert.DeserializeObject<JObject>(jsonJobs, settings);
                var jobsToken = responseObject.SelectToken("$.jobs");

                if (jobsToken != null)
                {
                    jenkinsNodes = JsonConvert.DeserializeObject<JenkinsNodeCollection>(jobsToken.ToString());
                }

                if (jenkinsNodes != null)
                {
                    jenkinsNodes.JenkinsServer = this;
                }
            }

            return jenkinsNodes;
        }

        public async Task<string> GetVersion()
        {
            var version = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(JenkinsBaseUrl);
                    var response = await client.GetAsync(JenkinsUrl);
                    var headerValues = response.Headers.GetValues("X-Jenkins");
                    var values = headerValues as string[] ?? headerValues.ToArray();
                    if (values.Any())
                        version = values.First();
                }
            }
            catch (Exception e)
            {
                version = "unknown";
            }

            return version;
        }

        public static string GetCredentialToken(string username, string apiToken)
        {
            var pair = $"{username}:{apiToken}";
            var bytes = Encoding.UTF8.GetBytes(pair);
            return Convert.ToBase64String(bytes);
        }
    }
}