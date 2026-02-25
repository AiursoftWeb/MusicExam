using System.ComponentModel.DataAnnotations;
using Aiursoft.MusicExam.Entities;

using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class CreateViewModel : UiStackLayoutViewModel
{
    public int PaperId { get; set; }

    [Display(Name = "Question Content")]
    [Required]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Explanation")]
    public string? Explanation { get; set; }

    [Display(Name = "Question Type")]
    public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;

    [Display(Name = "Question Asset")]
    [MaxLength(500)]
    [RegularExpression(@"^(questions/.*|importer-assets/.*)$", ErrorMessage = "Please upload a valid asset file.")]
    public string? AssetPath { get; set; }

    public List<OptionViewModel> Options { get; set; } = new();
}
