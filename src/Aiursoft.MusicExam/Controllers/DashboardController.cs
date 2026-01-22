using Aiursoft.MusicExam.Models.DashboardViewModels;
using Aiursoft.MusicExam.Services;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.MusicExam.Authorization;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.MusicExam.Entities;

namespace Aiursoft.MusicExam.Controllers;

[LimitPerMin]
public class DashboardController : Controller
{
    private readonly TemplateDbContext _dbContext;

    public DashboardController(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Policy = AppPermissionNames.CanTakeExam)]
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Index",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var schools = await _dbContext
            .Schools
            .Include(s => s.Papers)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var model = new Aiursoft.MusicExam.Models.HomeViewModels.IndexViewModel
        {
            Schools = schools
        };
        return this.StackView(model);
    }
}
