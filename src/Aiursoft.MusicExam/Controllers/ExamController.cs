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

        var model = new ResultViewModel
        {
            Title = paper.Title,
            TotalQuestions = paper.Questions.Count,
            Score = 0
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
            if (question.QuestionType == QuestionType.MultipleChoice)
            {
                var correctIds = result.CorrectOptionIds.OrderBy(x => x).ToList();
                var userIds = result.UserSelectedOptionIds.OrderBy(x => x).ToList();
                result.IsCorrect = correctIds.SequenceEqual(userIds);
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
                model.Score++;
            }

            model.Answers.Add(result);
        }

        // Save Attempt
        var submission = new ExamPaperSubmission
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PaperId = paper.Id,
            Score = model.Score,
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
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }

        var submissions = await _dbContext.ExamPaperSubmissions
            .Include(s => s.Paper)
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.SubmissionTime)
            .ToListAsync();

        var model = new HistoryViewModel
        {
            Submissions = submissions,
            PageTitle = "My Exam Records"
        };

        return this.StackView(model);
    }
}
