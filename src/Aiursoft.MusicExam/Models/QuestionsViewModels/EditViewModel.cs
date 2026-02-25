using System.ComponentModel.DataAnnotations;
using Aiursoft.MusicExam.Entities;

using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class EditViewModel : UiStackLayoutViewModel
{
    public int QuestionId { get; set; }
    public int PaperId { get; set; }

    [Display(Name = "Question Content")]
    public required string Content { get; set; }

    [Display(Name = "Explanation")]
    public string? Explanation { get; set; }

    [Display(Name = "Question Type")]
    public QuestionType QuestionType { get; set; }

    [Display(Name = "Question Asset")]
    [MaxLength(500)]
    [RegularExpression(@"^questions/.*", ErrorMessage = "Please upload a valid asset file.")]
    public string? AssetPath { get; set; }

    public List<OptionViewModel> Options { get; set; } = new();
}
