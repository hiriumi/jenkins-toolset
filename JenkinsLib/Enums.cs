using System;

namespace JenkinsLib
{

    public enum JenkinsNodeType
    {
        Unknown,
        ExternalJob,
        MatrixProject,
        MultiJobProject,
        WorkflowMultiBranchProject,
        MavenModuleSet,
        WorkflowJob,
        FreeStyleProject,
        Folder
    }
    public enum ComputerType
    {
        Windows,
        Linux,
        Mac,
        Unknown
    }
    public enum HttpVerb
    {
        Get,
        Post,
        Put,
        Delete
    }

    [Flags]
    public enum JobState
    {
        Orginal = 1,
        UpdatedLocally = 2,
        New = 4
    }
}