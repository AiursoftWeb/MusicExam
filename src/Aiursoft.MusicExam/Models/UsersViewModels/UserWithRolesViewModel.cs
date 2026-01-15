using Aiursoft.MusicExam.Entities;

namespace Aiursoft.MusicExam.Models.UsersViewModels;

public class UserWithRolesViewModel
{
    public required User User { get; set; }
    public required IList<string> Roles { get; set; }
}
