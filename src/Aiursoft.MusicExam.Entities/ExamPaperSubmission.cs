using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表用户一次完整的试卷提交记录。
/// </summary>
public class ExamPaperSubmission
{
    [Key]
    public Guid Id { get; init; }
    
    public DateTime SubmissionTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 本次提交的得分。
    /// 若为空，表示尚未评分。
    /// </summary>
    public int? Score { get; set; }
    
    // ================= 关联关系 =================
    
    public required string UserId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(UserId))]
    [NotNull]
    public User? User { get; set; }
    
    public required int PaperId { get; set; }
    
    [JsonIgnore]
    [ForeignKey(nameof(PaperId))]
    [NotNull]
    public ExamPaper? Paper { get; set; }
    
    [InverseProperty(nameof(QuestionSubmission.ExamPaperSubmission))]
    public IEnumerable<QuestionSubmission> QuestionSubmissions { get; init; } = new List<QuestionSubmission>();
}
