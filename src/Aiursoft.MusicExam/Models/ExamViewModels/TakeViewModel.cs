using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ExamViewModels;

public class TakeViewModel : UiStackLayoutViewModel
{
    public TakeViewModel(ExamPaper paper)
    {
        PageTitle = paper.Title;
        Paper = paper;
    }

    public ExamPaper Paper { get; init; }
}
