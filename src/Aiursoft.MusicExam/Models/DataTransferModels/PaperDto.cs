using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class PaperDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("subjectTitle")]
    public required string SubjectTitle { get; set; }
    
    [JsonPropertyName("subjectDesc")]
    public required string SubjectDesc { get; set; }
    
    [JsonPropertyName("imageUrl")]
    public required string ImageUrl { get; set; }
}
