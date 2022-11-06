namespace AzureProjectBackup.JsonContainers;

using System.Text.Json.Serialization;

public class IterationInfo
{
    [JsonPropertyName("id")]
    public string ID { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("attributes")]
    public IterationAttributes Attributes { get; set; } = new();

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
}
