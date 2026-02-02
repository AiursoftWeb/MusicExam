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

        var allPermissions = await _dbContext.QuestionBankRoles.ToListAsync();

        var schools = await _dbContext.Schools
            .Include(s => s.Papers)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var schoolViewModels = new List<SchoolViewModel>();

        foreach (var school in schools)
        {
            var accessiblePapers = school.Papers.Where(paper =>
            {
                var requiredRoles = allPermissions
                    .Where(r => r.SchoolId == paper.SchoolId && r.Level == paper.Level)
                    .ToList();

                if (!requiredRoles.Any())
                {
                    return true;
                }

                return requiredRoles.Any(rr => userRoleIds.Contains(rr.RoleId));
            }).ToList();
            
            if (accessiblePapers.Any())
            {
                schoolViewModels.Add(new SchoolViewModel
                {
                    Name = school.Name,
                    Papers = accessiblePapers
                });
            }
        }

        var model = new IndexViewModel
        {
            Schools = schoolViewModels
        };
        return this.StackView(model);
    }
}
