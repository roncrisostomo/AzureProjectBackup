namespace AzureProjectBackup;

using System.Text;

public class BackupCreator
{
    private readonly ProjectCreator projectCreator = new();
    private readonly IterationCopier iterationCopier = new();
    
    public async Task CreateBackup(string copyToProject, string copyFromProject, string azureOrganization,
                                    string personalAccessToken)
    {
        string azureDevOpsOrganizationUrl = $"https://dev.azure.com/{azureOrganization}/";
        string credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", personalAccessToken)));

        // Create target project if it does not exist yet
        if (!await this.projectCreator.CheckCreateProject(copyToProject, copyFromProject, azureDevOpsOrganizationUrl, credentials))
        {
            return;
        }

        // Copy iterations from source to target
        if (!await this.iterationCopier.CopyIterations(copyToProject, copyFromProject, azureDevOpsOrganizationUrl, credentials))
        {
            return;
        }

        // Copy work items from source to target
        WorkItemCopier workItemCopier = new(copyToProject, copyFromProject, azureDevOpsOrganizationUrl, personalAccessToken);
        if (!await workItemCopier.CopyWorkItems())
        {
            return;
        }

        Console.WriteLine("Backup complete");
    }
}
