using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionBankRolesViewModels;

public class EditViewModel : UiStackLayoutViewModel
{
    public int PaperId { get; set; }
    public string? PaperTitle { get; set; }

    public List<RoleSelectionViewModel> Roles { get; set; } = new();

    public EditViewModel()
    {
        PageTitle = "Manage Paper Roles";
    }
}

public class RoleSelectionViewModel
{
    public required string RoleId { get; set; }
    public required string RoleName { get; set; }
    public bool IsSelected { get; set; }
}
