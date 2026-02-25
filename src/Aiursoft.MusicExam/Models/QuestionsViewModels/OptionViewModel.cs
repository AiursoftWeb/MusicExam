using System.ComponentModel.DataAnnotations;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class OptionViewModel
{
    public int Id { get; set; }

    [Display(Name = "Option Content")]
    public string? Content { get; set; }

    [Display(Name = "Is Correct Answer")]
    public bool IsCorrect { get; set; }

    [Display(Name = "Option Asset")]
    [MaxLength(500)]
    [RegularExpression(@"^(questions/.*|importer-assets/.*)$", ErrorMessage = "Please upload a valid asset file.")]
    public string? AssetPath { get; set; }
}
