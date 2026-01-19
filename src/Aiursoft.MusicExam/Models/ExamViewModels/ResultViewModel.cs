using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ExamViewModels;

public class ResultViewModel : UiStackLayoutViewModel
{
    public required string Title { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public List<QuestionAnswerResult> Answers { get; set; } = new();
}

public class QuestionAnswerResult
{
    public required Question Question { get; set; }
    public List<int> UserSelectedOptionIds { get; set; } = new();
    public List<int> CorrectOptionIds { get; set; } = new();
    public bool IsCorrect { get; set; }
}
