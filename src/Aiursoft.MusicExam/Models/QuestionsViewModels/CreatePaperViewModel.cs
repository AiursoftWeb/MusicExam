using System.ComponentModel.DataAnnotations;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class CreatePaperViewModel : UiStackLayoutViewModel
{
    [Display(Name = "School")]
    public int SchoolId { get; set; }

    [Display(Name = "Title")]
    [Required]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Level")]
    public string? Level { get; set; }

    [Display(Name = "Category")]
    public string? Category { get; set; }

    public IEnumerable<SelectListItem> AvailableSchools { get; set; } = new List<SelectListItem>();
}
