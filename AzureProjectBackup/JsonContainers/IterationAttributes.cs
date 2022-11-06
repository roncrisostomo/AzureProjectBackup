namespace AzureProjectBackup.JsonContainers;

using System.Text.Json.Serialization;

public class IterationAttributes
{
    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = "";

    [JsonPropertyName("finishDate")]
    public string FinishDate { get; set; } = "";
    
    [JsonPropertyName("timeFrame")]
    public string TimeFrame { get; set; } = "";
}
