using Aiursoft.MusicExam.Entities;

using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Question Management";
    }

    public required List<School> Schools { get; set; }
}
