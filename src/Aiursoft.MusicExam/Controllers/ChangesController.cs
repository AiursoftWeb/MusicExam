using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.ChangesViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[Authorize]
[LimitPerMin]
public class ChangesController(
    TemplateDbContext dbContext,
    ChangeService changeService) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewChangeHistory)]
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "System",
        CascadedLinksIcon = "settings",
        CascadedLinksOrder = 9999,
        LinkText = "Change History",
        LinkOrder = 4)]
    public async Task<IActionResult> History()
    {
        var changes = await dbContext.Changes
            .Include(c => c.TriggerUser)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();

        return this.StackView(new HistoryViewModel
        {
            Changes = changes
        });
    }

    [Authorize(Policy = AppPermissionNames.CanViewActiveUsers)]
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "System",
        CascadedLinksIcon = "settings",
        CascadedLinksOrder = 9999,
        LinkText = "Active Users",
        LinkOrder = 5)]
    public async Task<IActionResult> ActiveUsers()
    {
        var reports = await changeService.GetMonthlyReports();
        return this.StackView(new ActiveUsersViewModel
        {
            Reports = reports
        });
    }

    [Authorize(Policy = AppPermissionNames.CanViewActiveUsers)]
    public async Task<IActionResult> ActiveUsersDetails(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        
        var report = await changeService.GetReportForMonth(start, end);
        return this.StackView(new ActiveUsersDetailsViewModel
        {
            Report = report
        });
    }
}
