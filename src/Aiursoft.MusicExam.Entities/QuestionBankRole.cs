using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表授权了特定 Role 访问特定学校中的特定级别（Level）。
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

    /// <summary>
    /// 授权访问的级别。如果为 null，表示授权访问该学校的所有级别。
    /// </summary>
    [MaxLength(200)]
    public string? Level { get; set; }
}
