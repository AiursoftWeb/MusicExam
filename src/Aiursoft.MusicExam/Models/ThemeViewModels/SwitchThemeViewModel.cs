using System.ComponentModel.DataAnnotations;

namespace Aiursoft.MusicExam.Models.ThemeViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
