using Aiursoft.UiStack.Layout;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.MusicExam.Models.QuestionBankRolesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public List<LevelWithRoles> Levels { get; set; } = new();

    public IndexViewModel()
    {
        PageTitle = "Global Question Bank Management";
    }
}

public class LevelWithRoles
{
    public required int SchoolId { get; set; }
    public required string SchoolName { get; set; }
    public required string Level { get; set; }
    public int PaperCount { get; set; }
    public List<IdentityRole> Roles { get; set; } = new();
}
