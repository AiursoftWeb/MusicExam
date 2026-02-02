using Aiursoft.MusicExam.Entities;

namespace Aiursoft.MusicExam.Models.DashboardViewModels;

public class SchoolViewModel
{
    public required string Name { get; init; }
    public required IEnumerable<ExamPaper> Papers { get; init; }
}
