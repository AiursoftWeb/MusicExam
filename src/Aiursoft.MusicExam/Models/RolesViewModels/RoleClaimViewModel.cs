namespace Aiursoft.MusicExam.Models.RolesViewModels;

public class RoleClaimViewModel
{
    public required string Key { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsSelected { get; set; }
}
