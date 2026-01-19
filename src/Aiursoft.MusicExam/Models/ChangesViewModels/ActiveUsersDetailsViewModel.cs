using Aiursoft.MusicExam.Services;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.ChangesViewModels;

public class ActiveUsersDetailsViewModel : UiStackLayoutViewModel
{
    public ActiveUsersDetailsViewModel()
    {
        PageTitle = "Active Users Details";
    }

    public required MonthlyActiveUserReport Report { get; set; }
}
