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
            .OrderBy(s => s.Id)
            .ToListAsync();

        var qbRoles = await dbContext.QuestionBankRoles
            .Include(r => r.Role)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Papers = papers.Select(s => new PaperWithRoles
            {
                Paper = s,
                Roles = qbRoles
                    .Where(r => r.ExamPaperId == s.Id && r.Role != null)
                    .Select(r => r.Role!)
                    .ToList()
            }).ToList()
        };

        return this.StackView(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var paper = await dbContext.ExamPapers.FirstOrDefaultAsync(s => s.Id == id);
        if (paper == null)
        {
            return NotFound();
        }

        var allRoles = await roleManager.Roles.ToListAsync();
        var selectedRoleIds = await dbContext.QuestionBankRoles
            .Where(r => r.ExamPaperId == id)
            .Select(r => r.RoleId)
            .ToListAsync();

        var model = new EditViewModel
        {
            PaperId = paper.Id,
            PaperTitle = paper.Title,
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
        var paper = await dbContext.ExamPapers.FirstOrDefaultAsync(s => s.Id == model.PaperId);
        if (paper == null)
        {
            return NotFound();
        }

        var existingRoles = await dbContext.QuestionBankRoles
            .Where(r => r.ExamPaperId == model.PaperId)
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
                ExamPaperId = model.PaperId,
                RoleId = mr.RoleId
            });
        dbContext.QuestionBankRoles.AddRange(toAdd);

        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
