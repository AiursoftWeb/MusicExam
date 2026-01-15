using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
