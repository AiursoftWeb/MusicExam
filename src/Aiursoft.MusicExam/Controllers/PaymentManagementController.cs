using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.PaymentManagementViewModels;
using Aiursoft.MusicExam.Models.UsersViewModels;
using Aiursoft.MusicExam.Services; // Contains StackView extension
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[Authorize]
[LimitPerMin]
public class PaymentManagementController(
    UserManager<User> userManager,
    TemplateDbContext context,
    ChangeRecorder changeRecorder)
    : Controller
{
    [Authorize(Policy = AppPermissionNames.CanReadUsers)]
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "Statistics",
        CascadedLinksIcon = "credit-card",
        CascadedLinksOrder = 9997,
        LinkText = "Payments",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var usageCounts = await context.ExamPaperSubmissions
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.UserId!, v => v.Count);
        
        var allUsers = await context.Users.ToListAsync();
        var usersWithRoles = new List<UserWithRolesViewModel>();
        foreach (var user in allUsers)
        {
            usersWithRoles.Add(new UserWithRolesViewModel
            {
                User = user,
                Roles = await userManager.GetRolesAsync(user),
                UsageCount = usageCounts.GetValueOrDefault(user.Id, 0)
            });
        }

        return this.StackView(new IndexViewModel
        {
            Users = usersWithRoles
        });
    }

    [Authorize(Policy = AppPermissionNames.CanReadUsers)]
    public async Task<IActionResult> UsageDetails(string id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var submissions = await context.ExamPaperSubmissions
            .Include(s => s.Paper)
            .Where(s => s.UserId == id)
            .OrderByDescending(s => s.SubmissionTime)
            .ToListAsync();

        var model = new UsageDetailsViewModel
        {
            TargetUser = user,
            Submissions = submissions
        };
        return this.StackView(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPermissionNames.CanEditUsers)]
    public async Task<IActionResult> UpdatePaymentInfo(string id, DateTime? expireAt)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var oldExpireAt = user.ExpireAt;
        user.ExpireAt = expireAt;
        await context.SaveChangesAsync();

        var triggerUser = await userManager.GetUserAsync(User);
        await changeRecorder.Record(
            ChangeType.UserPaymentInfoUpdated,
            triggerUser?.Id,
            targetUserId: user.Id,
            targetDisplayName: user.DisplayName,
            details: $"Updated user {user.Email}'s expiration from {oldExpireAt?.ToString("O")} to {expireAt?.ToString("O")}.");

        return Json(new { success = true, newExpireAt = expireAt?.ToString("O") });
    }
}
