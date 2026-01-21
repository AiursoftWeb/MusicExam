using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.PaymentManagementViewModels
{
    public class UsageDetailsViewModel : UiStackLayoutViewModel
    {
        public UsageDetailsViewModel()
        {
            PageTitle = "Usage Details";
        }
        public required User TargetUser { get; set; }
        public required IEnumerable<ExamPaperSubmission> Submissions { get; set; }
    }
}
