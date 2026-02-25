using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Services;
using Aiursoft.MusicExam.Services.FileStorage;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.QuestionsViewModels;
using Aiursoft.WebTools.Attributes;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[Authorize(Policy = AppPermissionNames.CanModifyQuestions)]
[LimitPerMin]
public class QuestionsController : Controller
{
    private readonly TemplateDbContext _dbContext;
    private readonly StorageService _storageService;

    public QuestionsController(TemplateDbContext dbContext, StorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
    }
    
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Directory",
        CascadedLinksIcon = "users",
        CascadedLinksOrder = 9998,
        LinkText = "Manage Questions",
        LinkOrder = 4)]
    public async Task<IActionResult> Index()
    {
        var schools = await _dbContext.Schools
            .Include(s => s.Papers)
            .ThenInclude(p => p.Questions)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Schools = schools
        };

        return this.StackView(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreatePaper()
    {
        var schools = await _dbContext.Schools.OrderBy(s => s.Id).ToListAsync();
        var model = new CreatePaperViewModel
        {
            AvailableSchools = new SelectList(schools, nameof(School.Id), nameof(School.Name))
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePaper(CreatePaperViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var schools = await _dbContext.Schools.OrderBy(s => s.Id).ToListAsync();
            model.AvailableSchools = new SelectList(schools, nameof(School.Id), nameof(School.Name));
            return this.StackView(model);
        }

        var paper = new ExamPaper
        {
            Title = model.Title,
            SchoolId = model.SchoolId,
            Level = model.Level,
            Category = model.Category
        };

        _dbContext.ExamPapers.Add(paper);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditPaper(int id)
    {
        var paper = await _dbContext.ExamPapers.FindAsync(id);
        if (paper == null)
        {
            return NotFound();
        }

        var schools = await _dbContext.Schools.OrderBy(s => s.Id).ToListAsync();
        var model = new EditPaperViewModel
        {
            PaperId = paper.Id,
            Title = paper.Title,
            SchoolId = paper.SchoolId,
            Level = paper.Level,
            Category = paper.Category,
            AvailableSchools = new SelectList(schools, nameof(School.Id), nameof(School.Name))
        };

        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPaper(EditPaperViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var schools = await _dbContext.Schools.OrderBy(s => s.Id).ToListAsync();
            model.AvailableSchools = new SelectList(schools, nameof(School.Id), nameof(School.Name));
            return this.StackView(model);
        }

        var paper = await _dbContext.ExamPapers.FindAsync(model.PaperId);
        if (paper == null)
        {
            return NotFound();
        }

        paper.Title = model.Title;
        paper.SchoolId = model.SchoolId;
        paper.Level = model.Level;
        paper.Category = model.Category;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int id) // id is PaperId
    {
        var paper = await _dbContext.ExamPapers.FindAsync(id);
        if (paper == null)
        {
            return NotFound();
        }

        var model = new CreateViewModel
        {
            PaperId = id,
            Options = new List<OptionViewModel>()
        };
        // Add some default empty options
        for (int i = 0; i < 4; i++)
        {
            model.Options.Add(new OptionViewModel { Content = "" });
        }

        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        if (!string.IsNullOrWhiteSpace(model.AssetPath))
        {
            try
            {
                var physicalPath = _storageService.GetFilePhysicalPath(model.AssetPath);
                if (!System.IO.File.Exists(physicalPath))
                {
                    ModelState.AddModelError(nameof(model.AssetPath), "Question media file upload failed or missing. Please re-upload.");
                    return this.StackView(model);
                }
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        for (int i = 0; i < model.Options.Count; i++)
        {
            var opt = model.Options[i];
            if (!string.IsNullOrWhiteSpace(opt.AssetPath))
            {
                try
                {
                    var physicalPath = _storageService.GetFilePhysicalPath(opt.AssetPath);
                    if (!System.IO.File.Exists(physicalPath))
                    {
                        ModelState.AddModelError($"Options[{i}].AssetPath", "Option media file upload failed or missing. Please re-upload.");
                        return this.StackView(model);
                    }
                }
                catch (ArgumentException)
                {
                    return BadRequest();
                }
            }
        }

        var paper = await _dbContext.ExamPapers.Include(p => p.Questions).FirstOrDefaultAsync(p => p.Id == model.PaperId);
        if (paper == null)
        {
            return NotFound();
        }

        var maxOrder = paper.Questions.Any() ? paper.Questions.Max(q => q.Order) : 0;

        var question = new Question
        {
            PaperId = model.PaperId,
            Content = model.Content,
            AssetPath = model.AssetPath,
            Explanation = model.Explanation,
            QuestionType = model.QuestionType,
            Order = maxOrder + 1,
            Options = model.Options
                .Where(o => !string.IsNullOrWhiteSpace(o.Content))
                .Select(o => new Option
                {
                    Content = o.Content!,
                    AssetPath = o.AssetPath,
                    IsCorrect = o.IsCorrect
                })
                .ToList()
        };

        _dbContext.Questions.Add(question);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var question = await _dbContext.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound();
        }

        var model = new EditViewModel
        {
            QuestionId = question.Id,
            PaperId = question.PaperId,
            Content = question.Content,
            AssetPath = question.AssetPath,
            Explanation = question.Explanation,
            QuestionType = question.QuestionType,
            Options = question.Options.OrderBy(o => o.Id).Select(o => new OptionViewModel
            {
                Id = o.Id,
                Content = o.Content,
                AssetPath = o.AssetPath,
                IsCorrect = o.IsCorrect
            }).ToList()
        };

        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        if (!string.IsNullOrWhiteSpace(model.AssetPath))
        {
            try
            {
                var physicalPath = _storageService.GetFilePhysicalPath(model.AssetPath);
                if (!System.IO.File.Exists(physicalPath))
                {
                    ModelState.AddModelError(nameof(model.AssetPath), "Question media file upload failed or missing. Please re-upload.");
                    return this.StackView(model);
                }
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        for (int i = 0; i < model.Options.Count; i++)
        {
            var opt = model.Options[i];
            if (!string.IsNullOrWhiteSpace(opt.AssetPath))
            {
                try
                {
                    var physicalPath = _storageService.GetFilePhysicalPath(opt.AssetPath);
                    if (!System.IO.File.Exists(physicalPath))
                    {
                        ModelState.AddModelError($"Options[{i}].AssetPath", "Option media file upload failed or missing. Please re-upload.");
                        return this.StackView(model);
                    }
                }
                catch (ArgumentException)
                {
                    return BadRequest();
                }
            }
        }

        var question = await _dbContext.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == model.QuestionId);

        if (question == null)
        {
            return NotFound();
        }

        question.Content = model.Content;
        question.AssetPath = model.AssetPath;
        question.Explanation = model.Explanation;
        question.QuestionType = model.QuestionType;

        // Handle Options Update
        // This is a simplified approach: remove all and Add new.
        // For production, diff-ing is better, but this works for now.
        _dbContext.Options.RemoveRange(question.Options);
        question.Options = model.Options
            .Where(o => !string.IsNullOrWhiteSpace(o.Content))
            .Select(o => new Option
            {
                Content = o.Content!,
                AssetPath = o.AssetPath,
                IsCorrect = o.IsCorrect
            })
            .ToList();

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _dbContext.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        _dbContext.Questions.Remove(question);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
