using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表用户对单个问题的提交答案。
/// </summary>
public class QuestionSubmission
{
    [Key]
    public Guid Id { get; init; }

    /// <summary>
    /// 用户的答案。
    /// 对于选择题，可能为 "0" 或 "0,2" 格式。
    /// 对于视唱题，可能为用户上传的录音文件路径。
    /// 若为空，表示用户未作答。
    /// </summary>
    [MaxLength(500)]
    public string? UserAnswer { get; set; }
    
    // ================= 关联关系 =================
    
    public required Guid ExamPaperSubmissionId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ExamPaperSubmissionId))]
    [NotNull]
    public ExamPaperSubmission? ExamPaperSubmission { get; set; }
    
    public required int QuestionId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(QuestionId))]
    [NotNull]
    public Question? Question { get; set; }
}
