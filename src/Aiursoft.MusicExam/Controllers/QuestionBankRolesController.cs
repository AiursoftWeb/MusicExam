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
        LinkText = "Global Management",
        LinkOrder = 3)]
    public async Task<IActionResult> Index()
    {
        var schools = await dbContext.Schools
            .OrderBy(s => s.Id)
            .ToListAsync();

        var qbRoles = await dbContext.QuestionBankRoles
            .Include(r => r.Role)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Schools = schools.Select(s => new SchoolWithRoles
            {
                School = s,
                Roles = qbRoles
                    .Where(r => r.SchoolId == s.Id && r.Role != null)
                    .Select(r => r.Role!)
                    .ToList()
            }).ToList()
        };

        return this.StackView(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(s => s.Id == id);
        if (school == null)
        {
            return NotFound();
        }

        var allRoles = await roleManager.Roles.ToListAsync();
        var selectedRoleIds = await dbContext.QuestionBankRoles
            .Where(r => r.SchoolId == id)
            .Select(r => r.RoleId)
            .ToListAsync();

        var model = new EditViewModel
        {
            SchoolId = school.Id,
            SchoolName = school.Name,
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
            .Where(r => r.SchoolId == model.SchoolId)
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
                RoleId = mr.RoleId
            });
        dbContext.QuestionBankRoles.AddRange(toAdd);

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
