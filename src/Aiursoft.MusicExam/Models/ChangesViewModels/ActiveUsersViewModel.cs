using Aiursoft.MusicExam.Services;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ChangesViewModels;

public class ActiveUsersViewModel : UiStackLayoutViewModel
{
    public ActiveUsersViewModel()
    {
        PageTitle = "Active Users Statistics";
    }

    public List<MonthlyActiveUserReport> Reports { get; set; } = [];
}
