using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表一份完整的试卷，包含一组题目。
/// </summary>
public class ExamPaper
{
    [Key]
    public int Id { get; init; }

    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(200)]
    public string? Level { get; set; }

    [MaxLength(200)]
    public string? Category { get; set; }

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    // ================= 关联关系 =================

    public int SchoolId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(SchoolId))]
    [NotNull]
    public School? School { get; set; }

    [InverseProperty(nameof(Question.Paper))]
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    [InverseProperty(nameof(ExamPaperSubmission.Paper))]
    public ICollection<ExamPaperSubmission> Submissions { get; set; } = new List<ExamPaperSubmission>();

    [InverseProperty(nameof(QuestionBankRole.ExamPaper))]
    public ICollection<QuestionBankRole> AuthorizedRoles { get; set; } = new List<QuestionBankRole>();
}
