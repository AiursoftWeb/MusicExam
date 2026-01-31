using System.ComponentModel.DataAnnotations;

namespace Aiursoft.MusicExam.Models.GlobalSettingsViewModels;

public class EditViewModel
{
    [Required]
    public string Key { get; set; } = string.Empty;
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Value { get; set; }
    public IFormFile? FileValue { get; set; }
}
