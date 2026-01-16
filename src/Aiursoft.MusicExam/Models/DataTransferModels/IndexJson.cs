using System.Text.Json.Serialization;

namespace Aiursoft.MusicExam.Models.DataTransferModels;

public class IndexJson
{
    [JsonPropertyName("data")]
    public required List<SchoolDto> Data { get; set; }
}
