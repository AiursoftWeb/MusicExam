using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class PaperCategoriesFileDto
{
    [JsonPropertyName("data")]
    public Dictionary<string, List<PaperCategoryDto>> Data { get; set; } = new();
}

public class PaperCategoryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public required string Title { get; set; }
}
