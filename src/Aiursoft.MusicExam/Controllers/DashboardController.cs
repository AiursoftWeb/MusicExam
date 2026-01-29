using Aiursoft.MusicExam.Models.DashboardViewModels;
using Aiursoft.MusicExam.Services;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.MusicExam.Controllers;

[LimitPerMin]
public class DashboardController : Controller
{
    private readonly TemplateDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public DashboardController(
        TemplateDbContext dbContext,
        UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [Authorize]
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
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        var userRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var schools = await _dbContext.Schools
            .Include(s => s.Papers)
            .Include(s => s.AuthorizedRoles)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Schools = schools
        };
        return this.StackView(model);
    }
}
