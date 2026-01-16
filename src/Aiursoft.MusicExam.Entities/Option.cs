using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表选择题的一个选项。
/// </summary>
public class Option
{
    [Key]
    public int Id { get; init; }

    /// <summary>
    /// 选项的显示内容。
    /// </summary>
    [MaxLength(1000)]
    public required string Content { get; set; }

    /// <summary>
    /// 选项关联的二进制资源（如图片、音频）的相对路径。
    /// 若为空，表示该选项没有关联的资源。
    /// </summary>
    [MaxLength(500)]
    public string? AssetPath { get; set; }

    /// <summary>
    /// 选项的顺序值（如 0, 1, 2, 3 对应 A, B, C, D）。
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// 标记该选项是否为正确答案。
    /// </summary>
    public bool IsCorrect { get; set; }

    // ================= 关联关系 =================

    public int QuestionId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(QuestionId))]
    [NotNull]
    public Question? Question { get; set; }
}
