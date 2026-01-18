using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class SchoolDto
{
    [JsonPropertyName("subjectTitle")]
    public required string SubjectTitle { get; set; }
    
    [JsonPropertyName("subjects")]
    // ReSharper disable once CollectionNeverUpdated.Global
    public required List<PaperDto> Subjects { get; init; }
}
