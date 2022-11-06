namespace AzureProjectBackup;

// nuget:Microsoft.TeamFoundationServer.Client
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Linq;

public class QueryExecutor
{
    // Execute a WIQL (Work Item Query Language) query to return a list of work items.
    public async Task<IList<WorkItem>> RunQuery(WorkItemTrackingHttpClient httpClient, string query)
    {
        // Create a wiql object and build our query
        var wiql = new Wiql()
        {
            // NOTE: Even if other columns are specified, only the ID & URL are available in the WorkItemReference
            Query = query,
        };

        // Execute the query to get the list of work items in the results
        var result = await httpClient.QueryByWiqlAsync(wiql).ConfigureAwait(false);
        var ids = (result.WorkItems is not null) ?
            result.WorkItems.Select(item => item.Id).ToArray() :
            result.WorkItemRelations.Select(item => item.Target.Id).ToArray();

        if (ids.Length == 0)
        {
            return Array.Empty<WorkItem>();
        }

        // Build a list of the fields we want to see
        var fields = new[] {
            "System.Id",
            "System.Title",
        };

        // Get work items for the ids found in query
        return await httpClient.GetWorkItemsAsync(ids, fields, result.AsOf).ConfigureAwait(false);
    }

    public async Task<WorkItem> GetWorkItemAsync(WorkItemTrackingHttpClient httpClient, int id)
    {
        // Build a list of the fields we want to see
        var fields = new[] {
            "System.Id",
            "System.Title",
        };

        // Get work items for the ids found in query
        return await httpClient.GetWorkItemAsync(id, expand: WorkItemExpand.All).ConfigureAwait(false);
    }

    // Execute a WIQL (Work Item Query Language) query to print a list of work items.
    public async Task PrintWorkItemsAsync(WorkItemTrackingHttpClient httpClient, string query)
    {
        var workItems = await this.RunQuery(httpClient, query).ConfigureAwait(false);
        IList<WorkItem> a = await this.RunQuery(httpClient, query).ConfigureAwait(false);

        Console.WriteLine("Query Results: {0} items found", workItems.Count);

        // Loop though work items and write to console
        foreach (var workItem in workItems)
        {
            Console.WriteLine(
                "{0}\t{1}",
                workItem.Id,
                workItem.Fields["System.Title"]);
        }
    }

    public async Task<List<int?>> GetWorkItemIDs(WorkItemTrackingHttpClient httpClient, string query)
    {
        var workItems = await this.RunQuery(httpClient, query).ConfigureAwait(false);

        // Console.WriteLine("Query Results: {0} items found", workItems.Count);

        return workItems.Select(workItem => workItem.Id).ToList();
    }
}
