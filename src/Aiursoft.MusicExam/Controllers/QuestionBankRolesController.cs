using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.QuestionBankRolesViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageQuestionBankRoles)]
[LimitPerMin]
public class QuestionBankRolesController(
    TemplateDbContext dbContext,
    RoleManager<IdentityRole> roleManager) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Directory",
        CascadedLinksIcon = "users",
        CascadedLinksOrder = 9998,
        LinkText = "Global Question Bank Management",
        LinkOrder = 3)]
    public async Task<IActionResult> Index()
    {
        var papers = await dbContext.ExamPapers
            .Include(p => p.School)
            .OrderBy(p => p.SchoolId)
            .ThenBy(p => p.Level ?? "")
            .ToListAsync();

        var qbRoles = await dbContext.QuestionBankRoles
            .Include(r => r.Role)
            .ToListAsync();

        // Group papers by (SchoolId, Level) to get unique levels
        var levelGroups = papers
            .GroupBy(p => new { p.SchoolId, Level = p.Level ?? "Uncategorized", SchoolName = p.School?.Name ?? "Unknown" })
            .Select(g => new LevelWithRoles
            {
                SchoolId = g.Key.SchoolId,
                SchoolName = g.Key.SchoolName,
                Level = g.Key.Level,
                PaperCount = g.Count(),
                Roles = qbRoles
                    .Where(r => r.SchoolId == g.Key.SchoolId && r.Level == g.Key.Level && r.Role != null)
                    .Select(r => r.Role!)
                    .ToList()
            })
            .ToList();

        var model = new IndexViewModel
        {
            Levels = levelGroups
        };

        return this.StackView(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int schoolId, string level)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(s => s.Id == schoolId);
        if (school == null)
        {
            return NotFound();
        }

        // Verify this level exists in this school
        var levelExists = await dbContext.ExamPapers
            .AnyAsync(p => p.SchoolId == schoolId && p.Level == level);
        
        if (!levelExists)
        {
            return NotFound();
        }

        var allRoles = await roleManager.Roles.ToListAsync();
        var selectedRoleIds = await dbContext.QuestionBankRoles
            .Where(r => r.SchoolId == schoolId && r.Level == level)
            .Select(r => r.RoleId)
            .ToListAsync();

        var model = new EditViewModel
        {
            SchoolId = schoolId,
            SchoolName = school.Name,
            Level = level,
            Roles = allRoles.Select(r => new RoleSelectionViewModel
            {
                RoleId = r.Id,
                RoleName = r.Name!,
                IsSelected = selectedRoleIds.Contains(r.Id)
            }).ToList()
        };

        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(s => s.Id == model.SchoolId);
        if (school == null)
        {
            return NotFound();
        }

        var existingRoles = await dbContext.QuestionBankRoles
            .Where(r => r.SchoolId == model.SchoolId && r.Level == model.Level)
            .ToListAsync();

        // Remove unselected
        var toRemove = existingRoles
            .Where(er => !model.Roles.Any(mr => mr.RoleId == er.RoleId && mr.IsSelected))
            .ToList();
        dbContext.QuestionBankRoles.RemoveRange(toRemove);

        // Add new
        var toAdd = model.Roles
            .Where(mr => mr.IsSelected && !existingRoles.Any(er => er.RoleId == mr.RoleId))
            .Select(mr => new QuestionBankRole
            {
                SchoolId = model.SchoolId,
                Level = model.Level,
                RoleId = mr.RoleId
            });
        dbContext.QuestionBankRoles.AddRange(toAdd);

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
