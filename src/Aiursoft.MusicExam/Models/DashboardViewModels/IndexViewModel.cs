using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.DashboardViewModels;


public class IndexViewModel : UiStackLayoutViewModel
{
    public required IEnumerable<School> Schools { get; init; }
    public IndexViewModel()
    {
        PageTitle = "Home";
    }
}
