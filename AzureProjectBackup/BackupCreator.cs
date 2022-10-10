namespace AzureProjectBackup
{
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using AzureProjectBackup.JsonContainers;

    public class BackupCreator
    {
        public async Task CreateBackup(string copyToProject, string copyFromProject, string azureOrganization,
                                       string personalAccessToken)
        {
            string azureDevOpsOrganizationUrl = $"https://dev.azure.com/{azureOrganization}/";
            string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", personalAccessToken)));

            // Create target project if it does not exist yet
            if (!await CheckCreateProject(copyToProject, copyFromProject, azureDevOpsOrganizationUrl, credentials))
            {
                return;
            }

            // Copy iterations from source to target
            if (!await CopyIterations(copyToProject, copyFromProject, azureDevOpsOrganizationUrl, credentials))
            {
                return;
            }

            // TODO: Copy work items
        }

        private async Task<bool> CheckCreateProject(string copyToProject, string copyFromProject,
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
        
        private async Task<bool> CopyIterations(string copyToProject, string copyFromProject,
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
                var response = SendRequest(azureDevOpsOrganizationUrl + copyToProject + "/", credentials, createUri, createBodyStr);
                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse new iteration ID from response
                CreateIterationResult result = JsonSerializer.Deserialize<CreateIterationResult>(responseBody)!;

                // Add new iteration to target project's team settings
                string addUri = GetIterationsUri();
                string addBodyStr = GetAddIterationBodyStr(result.Identifier);
                SendRequest(azureDevOpsOrganizationUrl + copyToProject + "/", credentials, addUri, addBodyStr);
            }
            return true;
        }

        private bool ProjectExists(string projectName, string azureDevOpsOrganizationUrl, string credentials)
        {
            var uri = GetProjectUri(projectName);
            var response = SendRequest(azureDevOpsOrganizationUrl, credentials, uri);
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> CreateProject(string projectName, string azureDevOpsOrganizationUrl, string credentials)
        {
            var uri = GetCreateProjectUri();
            var bodyStr = GetCreateProjectBodyStr(projectName, "");
            var response = SendRequest(azureDevOpsOrganizationUrl, credentials, uri, bodyStr);
            if (response.IsSuccessStatusCode)
            {
                // Wait 1 second for project to be initialized
                // For a more accurate approach, use GetOperation API instead of hardcoded delay
                await Task.Delay(1000);
                return true;
            }
            return false;
        }

        private async Task<List<IterationInfo>> GetProjectIterations(string copyFromProject, string azureDevOpsOrganizationUrl,
                                                                     string credentials)
        {
            // Get all iterations from source project
            string uri = GetIterationsUri();
            var response = SendRequest(azureDevOpsOrganizationUrl + copyFromProject + "/", credentials, uri);
            string responseBody = await response.Content.ReadAsStringAsync();

            // Parse iterations into list
            IterationsGetResult result = JsonSerializer.Deserialize<IterationsGetResult>(responseBody)!;
            return result.Value;
        }

        private HttpResponseMessage SendRequest(string azureDevOpsOrganizationUrl, string credentials, string uri, string? jsonstr = null)
        {
            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(azureDevOpsOrganizationUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "ManagedClientConsoleAppSample");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                if (jsonstr != null)
                {
                    HttpContent body = new StringContent(jsonstr, Encoding.UTF8, "application/json");
                    response = client.PostAsync(uri, body).Result;
                }
                else
                {
                    response = client.GetAsync(uri).Result;
                }
            }
            return response;
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
}
