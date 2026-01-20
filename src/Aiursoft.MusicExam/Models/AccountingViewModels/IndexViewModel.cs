using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.AccountingViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public int TotalUsers { get; set; }
    public int PaidUsers { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalQuestionsSubmitted { get; set; }

    public List<string> ChartLabels { get; set; } = new();
    public List<int> NewUsersData { get; set; } = new();
    public List<int> NewSubmissionsData { get; set; } = new();
}
