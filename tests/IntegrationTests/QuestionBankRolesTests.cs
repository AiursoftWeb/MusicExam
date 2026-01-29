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

        string roleName = "TestRole-" + Guid.NewGuid();
        string roleId;
        int privateSchoolId;
        int publicSchoolId;

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create a role
            var role = new IdentityRole(roleName);
            await roleManager.CreateAsync(role);
            roleId = role.Id;

            // Create a private school
            var privateSchool = new School { Name = "Private School" };
            db.Schools.Add(privateSchool);
            await db.SaveChangesAsync();
            privateSchoolId = privateSchool.Id;

            // Authorize the role to the private school
            db.QuestionBankRoles.Add(new QuestionBankRole
            {
                SchoolId = privateSchoolId,
                RoleId = roleId
            });

            // Create a public school
            var publicSchool = new School { Name = "Public School" };
            db.Schools.Add(publicSchool);
            await db.SaveChangesAsync();
            publicSchoolId = publicSchool.Id;

            // Create papers for both
            db.ExamPapers.Add(new ExamPaper { Title = "Private Paper", SchoolId = privateSchoolId });
            db.ExamPapers.Add(new ExamPaper { Title = "Public Paper", SchoolId = publicSchoolId });
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

        // 3. Check Dashboard - should see BOTH now (requirement change: all visible, access restricted)
        var dashboardResponse = await Http.GetAsync("/Dashboard/Index");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Public School", dashboardHtml);
        Assert.Contains("Private School", dashboardHtml);

        // 4. Try to access private paper directly - should be forbidden (or redirect to forbid/login)
        // Since we are logged in, it should be 403 or redirect to some error page.
        // The middleware might handle it.
        
        int privatePaperId;
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            privatePaperId = db.ExamPapers.First(p => p.Title == "Private Paper").Id;
        }

        var takeResponse = await Http.GetAsync($"/Exam/Take/{privatePaperId}");
        // Controller returns Forbid() which by default might redirect to AccessDenied path.
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

        // 6. Check Dashboard - should see BOTH now
        dashboardResponse = await Http.GetAsync("/Dashboard/Index");
        dashboardResponse.EnsureSuccessStatusCode();
        dashboardHtml = await dashboardResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Public School", dashboardHtml);
        Assert.Contains("Private School", dashboardHtml);

        // 7. Access private paper directly - should be success
        takeResponse = await Http.GetAsync($"/Exam/Take/{privatePaperId}");
        takeResponse.EnsureSuccessStatusCode();
        var takeHtml = await takeResponse.Content.ReadAsStringAsync();
        Assert.Contains("Private Paper", takeHtml);

        // 8. Test Global Management Page (as admin)
        await LogoutAsync();
        await LoginAsAdmin();

        var mgmtResponse = await Http.GetAsync("/QuestionBankRoles/Index");
        mgmtResponse.EnsureSuccessStatusCode();
        var mgmtHtml = await mgmtResponse.Content.ReadAsStringAsync();
        Assert.Contains("Private School", mgmtHtml);
        Assert.Contains("Public School", mgmtHtml);
        Assert.Contains(roleName, mgmtHtml);

        // Test Edit page
        var editUrl = $"/QuestionBankRoles/Edit/{privateSchoolId}";
        var editResponse = await Http.GetAsync(editUrl);
        editResponse.EnsureSuccessStatusCode();
        
        // Test POST Edit
        await PostForm(editUrl, new Dictionary<string, string>
        {
            { "SchoolId", privateSchoolId.ToString() },
            { "Roles[0].RoleId", roleId },
            { "Roles[0].RoleName", roleName },
            { "Roles[0].IsSelected", "false" } // Unselect
        });

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var anyRole = await db.QuestionBankRoles.AnyAsync(r => r.SchoolId == privateSchoolId);
            Assert.IsFalse(anyRole, "Role should have been removed");
        }
    }
}
