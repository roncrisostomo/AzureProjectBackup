namespace AzureProjectBackup;

using System;
using System.Threading.Tasks;

public class ProjectCreator
{
    public async Task<bool> CheckCreateProject(string copyToProject, string copyFromProject,
                                               string azureDevOpsOrganizationUrl, string credentials)
    {
        if (!ProjectExists(copyToProject, azureDevOpsOrganizationUrl, credentials))
        {
            Console.WriteLine($"Creating project [{copyToProject}]...");
            bool created = await CreateProject(copyToProject, azureDevOpsOrganizationUrl, credentials);
            if (!created)
            {
                Console.WriteLine("Project creation failed. Exiting...");
                return false;
            }
            Console.WriteLine("Project created");
        }
        else
        {
            Console.WriteLine($"Loading project [{copyFromProject}]");
        }
        return true;
    }

    private bool ProjectExists(string projectName, string azureDevOpsOrganizationUrl, string credentials)
    {
        var uri = GetProjectUri(projectName);
        var response = RequestSender.SendRequest(azureDevOpsOrganizationUrl, credentials, uri);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> CreateProject(string projectName, string azureDevOpsOrganizationUrl, string credentials)
    {
        var uri = GetCreateProjectUri();
        var bodyStr = GetCreateProjectBodyStr(projectName, "");
        var response = RequestSender.SendRequest(azureDevOpsOrganizationUrl, credentials, uri, bodyStr);
        if (response.IsSuccessStatusCode)
        {
            // Wait 1 second for project to be initialized
            // For a more accurate approach, use GetOperation API instead of hardcoded delay
            await Task.Delay(1000);
            return true;
        }
        return false;
    }

    private string GetProjectUri(string projectName)
    {
        string uri = $"_apis/projects/{projectName}?api-version=6.0";
        return uri;
    }

    private string GetCreateProjectUri()
    {
        string uri = "_apis/projects?api-version=6.0";
        return uri;
    }

    private string GetCreateProjectBodyStr(string projectName, string projectDescription)
    {
        string jsonStr = "{" +
                            $"\"name\": \"{projectName}\"," +
                            $"\"description\": \"{projectDescription}\"," +
                            "\"capabilities\": {" +
                                "\"versioncontrol\": {" +
                                    "\"sourceControlType\": \"Git\"" +
                                "}," +
                                "\"processTemplate\": {" +
                                    "\"templateTypeId\": \"b8a3a935-7e91-48b8-a94c-606d37c3e9f2\"" +
                                "}" +
                            "}" +
                            "}";
        return jsonStr;
    }
}
