using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.MusicExam.Models.QuestionBankRolesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public List<SchoolWithRoles> Schools { get; set; } = new();

    public IndexViewModel()
    {
        PageTitle = "Global Management";
    }
}

public class SchoolWithRoles
{
    public required School School { get; set; }
    public List<IdentityRole> Roles { get; set; } = new();
}
