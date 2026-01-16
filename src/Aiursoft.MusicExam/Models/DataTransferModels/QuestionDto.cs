using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class QuestionDto
{
    [JsonPropertyName("question")]
    public required string Question { get; set; }
    
    [JsonPropertyName("options")]
    public List<OptionDto> Options { get; set; } = new();
    
    [JsonPropertyName("correctAnswer")]
    public required string CorrectAnswer { get; set; }
    
    [JsonPropertyName("local_audios")]
    public List<string> LocalAudios { get; set; } = new();
    
    [JsonPropertyName("local_images")]
    public List<string> LocalImages { get; set; } = new();
}
