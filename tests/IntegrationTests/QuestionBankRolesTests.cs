using System.Net;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class QuestionBankRolesTests : TestBase
{
    [TestMethod]
    public async Task TestQuestionBankAccessControl()
    {
        // 1. Login as admin to setup
        await LoginAsAdmin();

        var roleName = "TestRole-" + Guid.NewGuid();
        string roleId;
        int privatePaperId;
        int schoolId;
        const string privateLevel = "Advanced";
        const string publicLevel = "Beginner";

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create a role
            var role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
            roleId = role.Id;

            // Create a school
            var school = new School { Name = "Test School" };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            schoolId = school.Id;

            // Create papers with different levels
            var privatePaper = new ExamPaper { Title = "Private Paper", SchoolId = school.Id, Level = privateLevel };
            var publicPaper = new ExamPaper { Title = "Public Paper", SchoolId = school.Id, Level = publicLevel };
            db.ExamPapers.AddRange(privatePaper, publicPaper);
            await db.SaveChangesAsync();
            privatePaperId = privatePaper.Id;

            // Authorize the role to the Advanced level (not specific paper)
            db.QuestionBankRoles.Add(new QuestionBankRole
            {
                SchoolId = schoolId,
                Level = privateLevel,
                RoleId = roleId
            });
            await db.SaveChangesAsync();
        }

        await LogoutAsync();

        // 2. Register a normal user
        var (userId, email, password) = await RegisterNewUserAsync();

        using (var scope = Server!.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.AddToRoleAsync(user!, "Students"); // Grant exam permission
        }
        
        // Login as the user
        var loginResponse = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });
        Assert.AreEqual(HttpStatusCode.Found, loginResponse.StatusCode);

        // 3. Check Dashboard - should see Public Paper only (requirement: Private Paper hidden)
        var dashboardResponse = await Http.GetAsync("/Dashboard/Index");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Public Paper", dashboardHtml);
        Assert.DoesNotContain("Private Paper", dashboardHtml);

        // 4. Try to access private paper directly - should be forbidden
        var takeResponse = await Http.GetAsync($"/Exam/Take/{privatePaperId}");
        if (takeResponse.StatusCode == HttpStatusCode.Found)
        {
             AssertRedirect(takeResponse, "/Error/Code403", exact: false);
        }
        else
        {
            Assert.AreEqual(HttpStatusCode.Forbidden, takeResponse.StatusCode);
        }

        // 5. Grant role to user
        await LogoutAsync();
        await LoginAsAdmin();

        using (var scope = Server!.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.AddToRoleAsync(user!, roleName);
        }

        await LogoutAsync();
        
        // Login again as the user
        await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });

        // 6. Access private paper directly - should be success
        takeResponse = await Http.GetAsync($"/Exam/Take/{privatePaperId}");
        takeResponse.EnsureSuccessStatusCode();
        var takeHtml = await takeResponse.Content.ReadAsStringAsync();
        Assert.Contains("Private Paper", takeHtml);

        // 7. Test Global Management Page (as admin)
        await LogoutAsync();
        await LoginAsAdmin();

        var mgmtResponse = await Http.GetAsync("/QuestionBankRoles/Index");
        mgmtResponse.EnsureSuccessStatusCode();
        var mgmtHtml = await mgmtResponse.Content.ReadAsStringAsync();
        Assert.Contains("Test School", mgmtHtml);
        Assert.Contains(privateLevel, mgmtHtml);
        Assert.Contains(roleName, mgmtHtml);

        // Test Edit page for the Advanced level
        var editUrl = $"/QuestionBankRoles/Edit?schoolId={schoolId}&level={privateLevel}";
        var editResponse = await Http.GetAsync(editUrl);
        editResponse.EnsureSuccessStatusCode();
        
        // Test POST Edit - remove authorization
        await PostForm(editUrl, new Dictionary<string, string>
        {
            { "SchoolId", schoolId.ToString() },
            { "Level", privateLevel },
            { "Roles[0].RoleId", roleId },
            { "Roles[0].RoleName", roleName },
            { "Roles[0].IsSelected", "false" } // Unselect
        });

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var anyRole = await db.QuestionBankRoles.AnyAsync(r => r.SchoolId == schoolId && r.Level == privateLevel);
            Assert.IsFalse(anyRole, "Role should have been removed from this level");
        }
    }
}