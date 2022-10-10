using System.Text.Json.Serialization;

namespace AzureProjectBackup.JsonContainers
{
    public class CreateIterationResult
    {
        [JsonPropertyName("id")]
        public int ID { get; set; } = -1;
        
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("structureType")]
        public string StructureType { get; set; } = "";
        
        [JsonPropertyName("hasChildren")]
        public bool HasChildren { get; set; } = false;
        
        [JsonPropertyName("attributes")]
        public IterationAttributes Attributes { get; set; } = new();
        
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";
        
        [JsonPropertyName("links")]
        public object Links { get; set; } = new();

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }
}
