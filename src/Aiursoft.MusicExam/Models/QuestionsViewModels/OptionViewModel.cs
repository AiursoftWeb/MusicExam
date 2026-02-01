using System.ComponentModel.DataAnnotations;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class OptionViewModel
{
    public int Id { get; set; }

    [Display(Name = "Option Content")]
    public string? Content { get; set; }

    [Display(Name = "Is Correct Answer")]
    public bool IsCorrect { get; set; }
}
