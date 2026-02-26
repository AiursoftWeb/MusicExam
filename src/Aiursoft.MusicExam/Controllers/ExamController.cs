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

    private async Task<bool> CanAccessPaper(ExamPaper paper, User user)
    {
        var userRoleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // Check Level-based authorization
        var requiredRoles = await _dbContext.QuestionBankRoles
            .Where(r => r.SchoolId == paper.SchoolId && r.Level == paper.Level)
            .ToListAsync();

        if (!requiredRoles.Any())
        {
            return true;
        }

        return requiredRoles.Any(rr => userRoleIds.Contains(rr.RoleId));
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

        if (!await CanAccessPaper(paper, user))
        {
            return Forbid();
        }

        var model = new TakeViewModel(paper);

        return this.StackView(model);
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanTakeExam)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Result(int id, IFormCollection form)
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

        if (!await CanAccessPaper(paper, user))
        {
            return Forbid();
        }

        var model = new ResultViewModel
        {
            Title = paper.Title,
            PaperId = paper.Id,
            TotalQuestions = paper.Questions.Count,
            CorrectCount = 0
        };

        foreach (var question in paper.Questions)
        {
            var result = new QuestionAnswerResult
            {
                Question = question,
                CorrectOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
            };

            var userSelection = form[$"question-{question.Id}"];
            if (!string.IsNullOrEmpty(userSelection))
            {
                result.UserSelectedOptionIds = userSelection.Select(s => int.Parse(s!)).ToList();
            }

            // Grading Logic
            if (question.QuestionType == QuestionType.MultipleChoice || question.QuestionType == QuestionType.SingleChoice)
            {
                var correctIds = result.CorrectOptionIds.OrderBy(x => x).ToList();
                var userIds = result.UserSelectedOptionIds.OrderBy(x => x).ToList();
                result.IsCorrect = correctIds.Any() && correctIds.SequenceEqual(userIds);
            }
            else
            {
                // For other types (like SightSinging), we might count it as correct by default or handle differently
                // For now, assuming if it's not multiple choice, it's manually graded or practice, so maybe correct?
                // Or maybe just skip grading. Let's mark it as correct if it's SightSinging since there's no input.
                result.IsCorrect = true;
            }

            if (result.IsCorrect)
            {
                model.CorrectCount++;
            }

            model.Answers.Add(result);
        }

        model.FinalScore = model.TotalQuestions > 0 
            ? (int)Math.Floor((double)model.CorrectCount / model.TotalQuestions * 100)
            : 0;

        // Save Attempt
        var submission = new ExamPaperSubmission
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PaperId = paper.Id,
            Score = model.CorrectCount, // Save CorrectCount (raw count) to DB for compatibility
            SubmissionTime = DateTime.UtcNow
        };
        _dbContext.ExamPaperSubmissions.Add(submission);
        await _dbContext.SaveChangesAsync();

        var questionSubmissions = new List<QuestionSubmission>();
        foreach (var answer in model.Answers)
        {
            var userAnswerString = string.Empty;
            if (answer.UserSelectedOptionIds.Any())
            {
                userAnswerString = string.Join(",", answer.UserSelectedOptionIds.OrderBy(x => x));
            }

            questionSubmissions.Add(new QuestionSubmission
            {
                Id = Guid.NewGuid(),
                ExamPaperSubmissionId = submission.Id,
                QuestionId = answer.Question.Id,
                UserAnswer = userAnswerString
            });
        }
        _dbContext.QuestionSubmissions.AddRange(questionSubmissions);
        await _dbContext.SaveChangesAsync();

        model.PageTitle = "Result - " + paper.Title;

        return this.StackView(model);
    }


    [HttpGet]
    [Authorize(Policy = AppPermissionNames.CanTakeExam)]
    public async Task<IActionResult> Review(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        var submission = await _dbContext.ExamPaperSubmissions
            .Include(s => s.Paper)
            .Include(s => s.QuestionSubmissions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        if (submission.UserId != user.Id)
        {
            return Forbid();
        }

        var paper = await _dbContext.ExamPapers
            .Include(p => p.Questions)
            .ThenInclude(q => q.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == submission.PaperId);

        if (paper == null)
        {
            return NotFound();
        }

        if (!await CanAccessPaper(paper, user))
        {
            return Forbid();
        }

        var model = new ResultViewModel
        {
            Title = paper.Title,
            PaperId = paper.Id,
            TotalQuestions = paper.Questions.Count,
            CorrectCount = submission.Score ?? 0, // Score in DB is CorrectCount
            PageTitle = "Review - " + paper.Title
        };
        
        // Calculate FinalScore (Percentage)
        model.FinalScore = model.TotalQuestions > 0 
            ? (int)Math.Floor((double)model.CorrectCount / model.TotalQuestions * 100)
            : 0;

        foreach (var question in paper.Questions)
        {
            var questionSubmission = submission.QuestionSubmissions.FirstOrDefault(qs => qs.QuestionId == question.Id);
            var result = new QuestionAnswerResult
            {
                Question = question,
                CorrectOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
            };

            if (questionSubmission != null && !string.IsNullOrEmpty(questionSubmission.UserAnswer))
            {
                result.UserSelectedOptionIds = questionSubmission.UserAnswer
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();
            }

            // Re-calculate correctness for display purposes
             if (question.QuestionType == QuestionType.MultipleChoice || question.QuestionType == QuestionType.SingleChoice)
            {
                var correctIds = result.CorrectOptionIds.OrderBy(x => x).ToList();
                var userIds = result.UserSelectedOptionIds.OrderBy(x => x).ToList();
                result.IsCorrect = correctIds.Any() && correctIds.SequenceEqual(userIds);
            }
            else
            {
                result.IsCorrect = true;
            }

            if (result.IsCorrect)
            {
                model.CorrectCount++;
            }

            model.Answers.Add(result);
        }

        return this.StackView(model, nameof(Result));
    }
}
