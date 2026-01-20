using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.AccountingViewModels;
using Aiursoft.WebTools.Attributes;
using Aiursoft.MusicExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.UiStack.Navigation;


namespace Aiursoft.MusicExam.Controllers;

[Authorize]
[LimitPerMin]
public class AccountingController(TemplateDbContext dbContext) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewAccounting)]
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Statistics",
        CascadedLinksIcon = "bar-chart-2",
        CascadedLinksOrder = 9999, // After Roles
        LinkText = "Accounting",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var totalUsers = await dbContext.Users.CountAsync();
        // Placeholder for PaidUsers as there is no specific logic for it yet
        var paidUsers = 0;

        var totalSubmissions = await dbContext.ExamPaperSubmissions.CountAsync();
        var totalQuestionsSubmitted = await dbContext.QuestionSubmissions.CountAsync();

        var model = new IndexViewModel
        {
            TotalUsers = totalUsers,
            PaidUsers = paidUsers,
            TotalSubmissions = totalSubmissions,
            TotalQuestionsSubmitted = totalQuestionsSubmitted
        };

        // Populate Chart Data
        var today = DateTime.UtcNow.Date;
        var day30DaysAgo = today.AddDays(-30);

        var usersHistory = await dbContext.Users
            .Where(t => t.CreationTime > day30DaysAgo)
            .GroupBy(t => t.CreationTime.Date)
            .Select(t => new { Time = t.Key, Count = t.Count() })
            .ToListAsync();

        var submissionHistory = await dbContext.ExamPaperSubmissions
            .Where(t => t.SubmissionTime > day30DaysAgo)
            .GroupBy(t => t.SubmissionTime.Date)
            .Select(t => new { Time = t.Key, Count = t.Count() })
            .ToListAsync();

        for (var i = 0; i <= 30; i++)
        {
            var date = day30DaysAgo.AddDays(i);
            model.ChartLabels.Add(date.ToString("MM-dd"));
            model.NewUsersData.Add(usersHistory.FirstOrDefault(t => t.Time == date)?.Count ?? 0);
            model.NewSubmissionsData.Add(submissionHistory.FirstOrDefault(t => t.Time == date)?.Count ?? 0);
        }

        return this.StackView(model);
    }
}
