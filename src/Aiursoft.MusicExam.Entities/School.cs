using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 代表一所学校或机构。
/// </summary>
public class School
{
    [Key]
    public int Id { get; init; }

    [MaxLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// 学校的描述信息。
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 显示顺序，用于自定义排序。
    /// </summary>
    public int DisplayOrder { get; set; }

    [InverseProperty(nameof(ExamPaper.School))]
    public IEnumerable<ExamPaper> Papers { get; init; } = new List<ExamPaper>();

    [InverseProperty(nameof(QuestionBankRole.School))]
    public IEnumerable<QuestionBankRole> AuthorizedRoles { get; init; } = new List<QuestionBankRole>();
}
