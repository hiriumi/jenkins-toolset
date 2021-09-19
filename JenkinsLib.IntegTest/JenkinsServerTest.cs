using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JenkinsLib.IntegTest
{
    [TestClass]
    public class JenkinsServerTest
    {
        string _url = "https://jenkins.hayato-iriumi.net";
        string _username = "hiriumi";
        string _token = "1180e497d24f371d0912901f260f98abb5";

        [TestMethod]
        public async Task test_list_jobsAsync()
        {
            var jenkinsServer = new JenkinsServer();
            jenkinsServer.JenkinsUrl = _url;
            jenkinsServer.Username = _username;
            jenkinsServer.ApiToken = _token;

            var jobs = await jenkinsServer.GetJenkinsNodes();

            foreach(var job in jobs)
            {
                Console.WriteLine(job.Name);
            }

        }
    }
}
