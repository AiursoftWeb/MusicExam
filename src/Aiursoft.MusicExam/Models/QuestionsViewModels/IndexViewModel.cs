using Aiursoft.MusicExam.Entities;

using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.QuestionsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public required List<School> Schools { get; set; }
}
