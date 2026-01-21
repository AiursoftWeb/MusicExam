using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表试卷中的一道题目。
/// </summary>
public class Question
{
    [Key]
    public int Id { get; init; }
    
    /// <summary>
    /// 题目的具体内容，可能是文本或 JSON 结构。
    /// </summary>
    [MaxLength(4000)]
    public required string Content { get; set; }
    
    /// <summary>
    /// 题目关联的二进制资源（如图片、音频）的相对路径。
    /// 若为空，表示该题目没有直接关联的资源。
    /// </summary>
    [MaxLength(500)]
    public string? AssetPath { get; set; }

    /// <summary>
    /// 题目在试卷中的显示顺序。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 题目的类型。
    /// </summary>
    public required QuestionType QuestionType { get; set; }

    /// <summary>
    /// 题目的解析。
    /// </summary>
    [MaxLength(4000)]
    public string? Explanation { get; set; }

    // ================= 关联关系 =================
    
    public int PaperId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(PaperId))]
    [NotNull]
    public ExamPaper? Paper { get; set; }
    
    [InverseProperty(nameof(Option.Question))]
    public ICollection<Option> Options { get; set; } = new List<Option>();
}
