using System.ComponentModel.DataAnnotations;

namespace Aiursoft.MusicExam.Entities;

public class Change
{
    public int Id { get; set; }
    public ChangeType Type { get; set; }
    
    public string? TriggerUserId { get; set; }
    public User? TriggerUser { get; set; }
    
    public string? TargetUserId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? TargetRoleId { get; set; }
    public string? TargetPermission { get; set; }
    
    [MaxLength(200)]
    public string Details { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
