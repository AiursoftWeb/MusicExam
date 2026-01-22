using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ManageViewModels;

public class ExamHistoryViewModel : UiStackLayoutViewModel
{
    public IEnumerable<ExamPaperSubmission> Submissions { get; set; } = new List<ExamPaperSubmission>();

    public ExamHistoryViewModel()
    {
        PageTitle = "My Exam History";
    }
}
