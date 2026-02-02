using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.OrderManagementViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageQuestionBankRoles)]
public class OrderManagementController(TemplateDbContext dbContext) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Directory",
        CascadedLinksIcon = "users",
        CascadedLinksOrder = 9997,
        LinkText = "Manage Display Order",
        LinkOrder = 4)]
    public async Task<IActionResult> Index()
    {
        // Load all schools with their papers
        var schools = await dbContext.Schools
            .Include(s => s.Papers)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        // Build hierarchical structure
        var schoolNodes = schools.Select(school =>
        {
            // Group papers by Level, then by Category
            var levels = school.Papers
                .OrderBy(p => p.DisplayOrder)
                .GroupBy(p => p.Level ?? "Uncategorized")
                .Select(levelGroup => new LevelNode
                {
                    Name = levelGroup.Key,
                    Categories = levelGroup
                        .GroupBy(p => p.Category ?? "General")
                        .Select(categoryGroup => new CategoryNode
                        {
                            Name = categoryGroup.Key,
                            Papers = categoryGroup
                                .Select(p => new PaperNode
                                {
                                    Id = p.Id,
                                    Title = p.Title,
                                    DisplayOrder = p.DisplayOrder
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList();

            return new SchoolNode
            {
                Id = school.Id,
                Name = school.Name,
                DisplayOrder = school.DisplayOrder,
                Levels = levels
            };
        }).ToList();

        var model = new IndexViewModel
        {
            Schools = schoolNodes
        };

        return this.StackView(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSchoolOrder([FromBody] UpdateOrderDto dto)
    {
        foreach (var item in dto.Items)
        {
            var school = await dbContext.Schools.FindAsync(item.Id);
            if (school != null)
            {
                school.DisplayOrder = item.DisplayOrder;
            }
        }

        await dbContext.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePaperOrder([FromBody] UpdateOrderDto dto)
    {
        foreach (var item in dto.Items)
        {
            var paper = await dbContext.ExamPapers.FindAsync(item.Id);
            if (paper != null)
            {
                paper.DisplayOrder = item.DisplayOrder;
            }
        }

        await dbContext.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
