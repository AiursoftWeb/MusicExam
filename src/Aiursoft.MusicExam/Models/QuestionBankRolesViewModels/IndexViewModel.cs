using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.MusicExam.Models.QuestionBankRolesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public List<PaperWithRoles> Papers { get; set; } = new();

    public IndexViewModel()
    {
        PageTitle = "Global Question Bank Management";
    }
}

public class PaperWithRoles
{
    public required ExamPaper Paper { get; set; }
    public List<IdentityRole> Roles { get; set; } = new();
}
