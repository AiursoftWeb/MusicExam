using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ChangesViewModels;

public class HistoryViewModel : UiStackLayoutViewModel
{
    public HistoryViewModel()
    {
        PageTitle = "Change History";
    }

    public List<Change> Changes { get; set; } = [];
}
