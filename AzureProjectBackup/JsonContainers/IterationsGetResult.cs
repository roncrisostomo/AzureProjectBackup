namespace AzureProjectBackup.JsonContainers;

using System.Text.Json.Serialization;

public class IterationsGetResult
{
    [JsonPropertyName("count")]
    public int Count { get; set; } = 0;

    [JsonPropertyName("value")]
    public List<IterationInfo> Value { get; set; } = new();
}
