using System.Text.Json.Serialization;

namespace AzureProjectBackup.JsonContainers
{
    public class IterationsGetResult
    {
        [JsonPropertyName("count")]
        public int Count { get; set; } = 0;

        [JsonPropertyName("value")]
        public List<IterationInfo> Value { get; set; } = new();
    }
}
