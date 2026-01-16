using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.ExamViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Aiursoft.MusicExam.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[LimitPerMin]
public class ExamController : Controller
{
    private readonly TemplateDbContext _dbContext;
    private readonly UserManager<User> _userManager;

    public ExamController(TemplateDbContext dbContext, UserManager<User> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissionNames.CanTakeExam)]
    public async Task<IActionResult> Take(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }
        
        var paper = await _dbContext.ExamPapers
            .Include(p => p.Questions)
            .ThenInclude(q => q.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (paper == null)
        {
            return NotFound();
        }

        var model = new TakeViewModel(paper);
        
        return this.StackView(model);
    }
}
