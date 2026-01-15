using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class SchoolDto
{
    [JsonPropertyName("subjectTitle")]
    public required string SubjectTitle { get; set; }
    
    [JsonPropertyName("subjects")]
    public required List<PaperDto> Subjects { get; set; }
}
