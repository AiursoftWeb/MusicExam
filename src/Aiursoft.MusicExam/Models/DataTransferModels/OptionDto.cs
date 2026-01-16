using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class OptionDto
{
    [JsonPropertyName("value")]
    public required string Value { get; set; }
    
    [JsonPropertyName("content")]
    public required string Content { get; set; }
    
    [JsonPropertyName("type")]
    public required string Type { get; set; }
    
    [JsonPropertyName("local_content")]
    public string? LocalContent { get; set; }
}
