using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表授权了特定 Role 访问特定题库（School）。
/// </summary>
public class QuestionBankRole
{
    [Key]
    public int Id { get; init; }

    public required string RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public IdentityRole? Role { get; set; }

    public int SchoolId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(SchoolId))]
    public School? School { get; set; }
}
