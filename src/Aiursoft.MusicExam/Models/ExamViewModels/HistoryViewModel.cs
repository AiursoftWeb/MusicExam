using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ExamViewModels;

public class HistoryViewModel : UiStackLayoutViewModel
{
    public required List<ExamPaperSubmission> Submissions { get; set; } = new();
}
