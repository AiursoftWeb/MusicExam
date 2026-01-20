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

        return this.StackView(model);
    }
}
