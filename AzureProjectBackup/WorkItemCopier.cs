namespace AzureProjectBackup;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using AzureProjectBackup.JsonContainers;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

public class WorkItemCopier
{
    private readonly string copyToProject;
    private readonly string copyFromProject;
    private readonly string azureOrganizationUri;
    private readonly string personalAccessToken;
    private readonly WorkItemTrackingHttpClient httpClient;
    private readonly QueryExecutor queryExecutor = new();

    private readonly string[] requestFields;

    public WorkItemCopier(
        string copyToProject,
        string copyFromProject,
        string azureOrganizationUri,
        string personalAccessToken)
    {
        this.copyToProject = copyToProject;
        this.copyFromProject = copyFromProject;
        this.azureOrganizationUri = azureOrganizationUri;
        this.personalAccessToken = personalAccessToken;
        
        Uri uri = new(this.azureOrganizationUri);
        VssBasicCredential credentials = new("", this.personalAccessToken);
        VssConnection connection = new VssConnection(uri, credentials);
        this.httpClient = connection.GetClient<WorkItemTrackingHttpClient>();

        this.requestFields = new List<WorkItemField>()
        {
            WorkItemField.ID,
            WorkItemField.WorkItemType,
            WorkItemField.Title,
            WorkItemField.State,
            WorkItemField.RemainingWork,
            WorkItemField.CompletedWork,
            WorkItemField.Effort,
            WorkItemField.Description,
            WorkItemField.AssignedTo,
            WorkItemField.IterationPath,
            WorkItemField.AreaPath,
        }.Select(x => ToFieldUri(x)).ToArray();
    }

    private struct Node
    {
        // ID in source work item tree
        public int ID = -1;
        // Work item in target work item tree copied from source
        public WorkItem? WorkItemCopy = null;
        // List of child work items
        public List<Node>? Children = null;

        public Node() {}
    }

    public async Task<bool> CopyWorkItems()
    {
        // Get list of work items to copy
        Console.WriteLine($"Loading work items from project [{this.copyFromProject}]");

        // Algorithm: Go from top to bottom in work item tree
        //  Start from top-level work items and iterate through child items
        
        // Temp: For debugging
        // await this.queryExecutor.PrintWorkItemsAsync(httpClient, GetTopLevelWorkItemsQueryString(this.copyFromProject));
        
        // Get list of top-level work items from copyFromProject, i.e. items without parents
        List<int?> topLevelIDs = await this.queryExecutor.GetWorkItemIDs(httpClient, GetTopLevelWorkItemsQueryString(this.copyFromProject));

        // Generate tree of work items from copyFromProject with a dummy node as root
        Node rootNode = await this.CreateNode(-1, topLevelIDs, this.copyFromProject);

        // Create new work items in copyToProject
        await CreateWorkItems(rootNode, null);

        return true;
    }

    private async Task CreateWorkItems(Node curNode, Node? parentNode)
    {
        // Create work item in target project for curNode
        if (curNode.ID >= 0)
        {
            WorkItem? parent = (parentNode is not null) ? parentNode.Value.WorkItemCopy : null;
            curNode.WorkItemCopy = await CreateWorkItem(curNode.ID, parent, copyFromProject, copyToProject);
        }

        // Recurse for each child item
        if (curNode.Children is not null)
        {
            foreach (Node childNode in curNode.Children)
            {
                await CreateWorkItems(childNode, curNode);
            }
        }
    }

    // Create work item in copyToProject that is a copy of work item [id] from copyFromProject
    private async Task<WorkItem> CreateWorkItem(int id, WorkItem? parentWorkItem, string copyFromProject, string copyToProject)
    {
        if (parentWorkItem is null)
        {
            Console.WriteLine($"Creating copy of work item {id}");
        }
        else
        {
            Console.WriteLine($"Creating copy of work item {id} as child of {parentWorkItem.Id}");
        }

        // Get source work item from copyFromProject
        WorkItem sourceWorkItem = await this.httpClient.GetWorkItemAsync(this.copyFromProject, id, this.requestFields);

        // Create copy in copyToProject
        JsonPatchDocument patchDocument = new();

        string workItemType = string.Empty;
        string state = string.Empty;
        foreach (KeyValuePair<string, object> item in sourceWorkItem.Fields)
        {
            if (item.Value is null)
            {
                continue;
            }

            string fieldType = item.Key;
            string? value = item.Value.ToString()!;
            
            // Skip ID--the work item copy will get its own ID upon creation
            if (fieldType.Equals(ToFieldUri(WorkItemField.ID)))
            {
                continue;
            }

            // For assignee, target value is contained in IdentityRef
            if (fieldType.Equals(ToFieldUri(WorkItemField.AssignedTo)))
            {
                var identityRef = (Microsoft.VisualStudio.Services.WebApi.IdentityRef)item.Value;
                value = identityRef.DisplayName;
            }

            // Cache work item type--this is passed as parameter to CreateWorkItems API, rather than included in patch document
            if (fieldType.Equals(ToFieldUri(WorkItemField.WorkItemType)))
            {
                workItemType = value;
                continue;
            }

            // Cache state--this needs to be set after work item is created because work items start in To Do state upon creation
            if (fieldType.Equals(ToFieldUri(WorkItemField.State)))
            {
                state = value;
                continue;
            }
            
            // In iteration and area path, replace "copyFromProject" with "copyToProject"
            //  e.g. "ProjSource - Sprint 1" -> "ProjCopy - Sprint 1"
            if (fieldType.Equals(ToFieldUri(WorkItemField.IterationPath)) ||
                fieldType.Equals(ToFieldUri(WorkItemField.AreaPath)))
            {
                value = value.Replace(copyFromProject, copyToProject);
            }

            // Add fields and their values to the patch document
            if (fieldType.Length > 0)
            {
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/" + fieldType,
                        Value = value
                    }
                );
            }
        }
        Debug.Assert(workItemType.Length > 0, "Error: Work item type is blank");

        // For non-root nodes, set parent node
        if (parentWorkItem is not null)
        {
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Reverse",
                        url = parentWorkItem.Url
                    }
                }
            );
        }

        WorkItem newWorkItem = new();
        try
        {
            newWorkItem = this.httpClient.CreateWorkItemAsync(patchDocument, copyToProject, workItemType).Result;

            Console.WriteLine($"Work item successfully created: #{newWorkItem.Id}");
        }
        catch (AggregateException ex)
        {
            Debug.Assert(false, $"Error creating work item: {ex?.InnerException?.Message}");
        }

        // Update state once work item is created
        if (newWorkItem.Id.HasValue)
        {
            JsonPatchDocument updatePatchDocument = new();
            updatePatchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + ToFieldUri(WorkItemField.State),
                    Value = state
                }
            );

            try
            {
                int newWorkItemID = (int)newWorkItem.Id.Value;
                newWorkItem = this.httpClient.UpdateWorkItemAsync(updatePatchDocument, newWorkItemID).Result;
            }
            catch (AggregateException ex)
            {
                Debug.Assert(false, $"Error updating work item state: {ex?.InnerException?.Message}");
            }
        }
        
        return newWorkItem;
    }

    private async Task<Node> CreateNode(int curID, List<int?> childIDs, string copyFromProject)
    {
        // Create node
        Node curNode = new()
        {
            ID = curID,
        };

        // If ID list is empty, then current item is a leaf node (no children)
        // Otherwise, process each ID as children of current node
        if (childIDs.Count > 0)
        {
            List<Node> childNodes = new();
            foreach (int? childID in childIDs)
            {
                if (!childID.HasValue)
                {
                    continue;
                }
                // Get child links of current child
                List<int?> grandChildIDs = await this.queryExecutor.GetWorkItemIDs(this.httpClient, GetWorkItemLinksQueryString(copyFromProject, childID.Value));
                // Exclude current child from query results
                grandChildIDs.Remove(childID.Value);
                // Recursively create child nodes
                Node childNode = await CreateNode(childID.Value, grandChildIDs, copyFromProject);
                childNodes.Add(childNode);
            }
            curNode.Children = childNodes;
        }
        
        return curNode;
    }

    private static string ToFieldUri(WorkItemField columnType)
    {
        return columnType switch
        {
            WorkItemField.ID            => "System.Id",
            WorkItemField.Parent        => "System.Parent",
            WorkItemField.WorkItemType  => "System.WorkItemType",
            WorkItemField.Title         => "System.Title",
            WorkItemField.State         => "System.State",
            WorkItemField.RemainingWork => "Microsoft.VSTS.Scheduling.RemainingWork",
            WorkItemField.CompletedWork => "Microsoft.VSTS.Scheduling.CompletedWork",
            WorkItemField.Effort        => "Microsoft.VSTS.Scheduling.Effort",
            WorkItemField.Description   => "System.Description",
            WorkItemField.AssignedTo    => "System.AssignedTo",
            WorkItemField.IterationPath => "System.IterationPath",
            WorkItemField.AreaPath      => "System.AreaPath",
            _                           => "",
        };
    }

    // Get query for work items without parent in specified project
    // By convention, Tasks must have parent--thus this queries only Epics and Issues
    private string GetTopLevelWorkItemsQueryString(string project)
    {
        string queryStr =
            "Select [Id] " +
            "From WorkItems " +
            $"Where [System.TeamProject] = '{project}' " +
            "And (" +
                "[Work Item Type] = 'Epic' " +
                "Or [Work Item Type] = 'Issue' " +
            ")" +
            "And [System.Parent] = '' " +
            "And [System.State] <> 'Closed' " +
            "Order By [State] Asc, [Changed Date] Desc";
        return queryStr;
    }

    private string GetWorkItemLinksQueryString(string project, int workItemID)
    {
        string queryStr =
            "Select [System.Id] " +
            "From WorkItemLinks " +
            "Where [System.Links.LinkType] = 'System.LinkTypes.Hierarchy-Forward' " +
            $"And [Source].[System.Id] = {workItemID} " +
            "And (" +
                $"[Target].[System.TeamProject] = '{project}' " +
                "And [Target].[System.WorkItemType] <> '' " +
                "And [Target].[System.State] <> 'Closed' " +
            ") " +
            "Order By [System.Id]";
        return queryStr;
    }
}
