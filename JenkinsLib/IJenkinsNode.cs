using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace JenkinsLib
{
    public interface IJenkinsNode
    {
        JenkinsServer JenkinsServer { get; set; }
        bool Buildable { get; set; }
        string Color { get; set; }
        string Description { get; set; }
        string DisplayNameOrNull { get; set; }
        List<JenkinsNode> DownSreamProjects { get; set; }
        bool InQueue { get; set; }
        string JenkinsDomain { get; }
        Build LastBuild { get; set; }
        Build LastFailedBuild { get; set; }
        Build LastStableBuild { get; set; }
        Build LastSuccessfulBuild { get; set; }
        string LocalConfigFilePath { get; set; }
        string Name { get; set; }
        int NextBuildNumber { get; set; }
        string OriginalConfigXml { get; set; }
        List<ParameterDef> Parameters { get; set; }
        bool Selected { get; set; }
        JobState State { get; set; }
        string Url { get; set; }
        string Username { get; set; }
        string ApiToken { get; set; }
        string OriginalUrl { get; set; }

        event PropertyChangedEventHandler PropertyChanged;

        Task Delete(string username = "", string apiToken = "");
        Task<string> GetConfigXml(string username = "", string apiToken = "");
        Task Update(string updatedJobXml);

        Task Rename(string newJobName);

        Task RunBuild(string authToken, List<ParameterDef> parameters);

        Task EnableDisable(bool enable);

        Task<BuildCollection> GetBuilds();

        bool ValidateJobConfigXml();
    }
}