namespace AzureProjectBackup;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AzureProjectBackup.JsonContainers;

public class IterationCopier
{
    public async Task<bool> CopyIterations(string copyToProject, string copyFromProject,
                                           string azureDevOpsOrganizationUrl, string credentials)
    {
        // Get list of iterations to copy
        Console.WriteLine($"Loading iterations from project [{copyFromProject}]");
        var iterations = await GetProjectIterations(copyFromProject, azureDevOpsOrganizationUrl, credentials);

        // For each iteration...
        foreach (var iteration in iterations)
        {
            Console.WriteLine($"Copying iteration [{iteration.Name}]: " +
                              $"startDate={iteration.Attributes.StartDate} " +
                              $"finishDate={iteration.Attributes.FinishDate}");

            // TODO: Newly created projects start with Sprint 1 by default. If source project has sprint with same name,
            //  do not create a new sprint--just update the existing sprint's start and finish dates.

            // Create new iteration in target project with same start and end dates
            string createUri = GetCreateIterationUri();
            string createBodyStr = GetCreateIterationBodyStr(iteration);
            var response = RequestSender.SendRequest(azureDevOpsOrganizationUrl + copyToProject + "/", credentials, createUri, createBodyStr);
            string responseBody = await response.Content.ReadAsStringAsync();

            // Parse new iteration ID from response
            CreateIterationResult result = JsonSerializer.Deserialize<CreateIterationResult>(responseBody)!;

            // Add new iteration to target project's team settings
            string addUri = GetIterationsUri();
            string addBodyStr = GetAddIterationBodyStr(result.Identifier);
            RequestSender.SendRequest(azureDevOpsOrganizationUrl + copyToProject + "/", credentials, addUri, addBodyStr);
        }
        return true;
    }

    private async Task<List<IterationInfo>> GetProjectIterations(string copyFromProject, string azureDevOpsOrganizationUrl,
                                                                 string credentials)
    {
        // Get all iterations from source project
        string uri = GetIterationsUri();
        var response = RequestSender.SendRequest(azureDevOpsOrganizationUrl + copyFromProject + "/", credentials, uri);
        string responseBody = await response.Content.ReadAsStringAsync();

        // Parse iterations into list
        IterationsGetResult result = JsonSerializer.Deserialize<IterationsGetResult>(responseBody)!;
        return result.Value;
    }

    private string GetIterationsUri()
    {
        string uri = "_apis/work/teamsettings/iterations?api-version=6.0";
        return uri;
    }

    private string GetCreateIterationUri()
    {
        string uri = "_apis/wit/classificationnodes/iterations?api-version=6.0";
        return uri;
    }

    private string GetCreateIterationBodyStr(IterationInfo iteration)
    {
        string jsonStr = "{" +
                            $"\"name\": \"{iteration.Name}\"," +
                            "\"attributes\": {" +
                                $"\"startDate\": \"{iteration.Attributes.StartDate}\"," +
                                $"\"finishDate\": \"{iteration.Attributes.FinishDate}\"" +
                            "}" +
                            "}";
        return jsonStr;
    }

    private string GetAddIterationBodyStr(string iterationID)
    {
        string jsonStr = "{" +
                            $"\"id\": \"{iterationID}\"" +
                            "}";
        return jsonStr;
    }
}
