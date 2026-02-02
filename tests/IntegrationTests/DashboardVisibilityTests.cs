using System.Net;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class DashboardVisibilityTests : TestBase
{
    [TestMethod]
    public async Task Dashboard_ShouldHideRestrictedPapers()
    {
        // 1. Register and login
        var (userId, email, password) = await RegisterNewUserAsync();

        // 2. Setup restriction
        var uniqueTitle = "Restricted Secret Paper-" + Guid.NewGuid();
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            // Create a special role
            var roleName = "SpecialRole";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
            var role = await roleManager.FindByNameAsync(roleName);

            // Create a new restricted paper
            var school = new School { Name = "Secret School" };
            db.Schools.Add(school);
            await db.SaveChangesAsync();

            var paper = new ExamPaper
            {
                Title = uniqueTitle,
                SchoolId = school.Id,
                Category = "Secret",
                Level = "TopSecret"
            };
            db.ExamPapers.Add(paper);
            await db.SaveChangesAsync();
            
            // Add restriction
            db.QuestionBankRoles.Add(new QuestionBankRole
            {
                RoleId = role!.Id,
                SchoolId = paper.SchoolId,
                Level = paper.Level
            });
            await db.SaveChangesAsync();
        }

        // 3. Check Dashboard - Should be hidden (User has no role yet)
        var response = await Http.GetAsync("/Dashboard/Index");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        
        Assert.DoesNotContain(uniqueTitle, html);

        // 4. Grant permission
        using (var scope = Server.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.AddToRoleAsync(user!, "SpecialRole");
        }

        // 5. Re-login to update claims
        await LogoutAsync();
        var loginResponse = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });
        Assert.AreEqual(HttpStatusCode.Found, loginResponse.StatusCode);

        // 6. Check Dashboard - Should be visible
        response = await Http.GetAsync("/Dashboard/Index");
        response.EnsureSuccessStatusCode();
        html = await response.Content.ReadAsStringAsync();

        Assert.Contains(uniqueTitle, html);
    }
}
