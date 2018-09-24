using System.Net;
using System.Threading.Tasks;

namespace JenkinsLib
{
    public interface IJenkinsServer
    {
        string JenkinsUrl { get; set; }
        string JenkinsVersion { get; set; }
        string Username { get; set; }
        string ApiToken { get; set; }
        Task CreateNewJob(string jobName, string jobXml);
        Task<JenkinsNodeCollection> GetJenkinsNodes(string username = "", string apiToken = "", bool recurse = false);
        Task<ComputerCollection> GetAllComputers();
        Task<JenkinsNode> GetJobDetails(string jobName);
        Task<string> GetSchema();
        NetworkCredential Credential { get; set; }

    }
}