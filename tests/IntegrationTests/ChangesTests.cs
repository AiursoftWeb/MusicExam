using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Services;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class ChangesTests : TestBase
{
    [TestMethod]
    public async Task TestChangeRecording()
    {
        await LoginAsAdmin();

        // 1. Create a user
        var userName = $"test-{Guid.NewGuid()}";
        var email = $"{userName}@aiursoft.com";
        await PostForm("/Users/Create", new Dictionary<string, string>
        {
            { "UserName", userName },
            { "DisplayName", "Test User" },
            { "Email", email },
            { "Password", "Test-Password-123" }
        });

        // 2. Verify change recorded
        var db = GetService<TemplateDbContext>();
        var user = await db.Users.FirstAsync(u => u.UserName == userName);
        var creationChange = await db.Changes.FirstOrDefaultAsync(c => c.Type == ChangeType.UserCreated && c.TargetUserId == user.Id);
        Assert.IsNotNull(creationChange);

        // 3. Create a role and assign permission
        await PostForm("/Roles/Create", new Dictionary<string, string>
        {
            { "RoleName", "TestRole" }
        });
        var role = await db.Roles.FirstAsync(r => r.Name == "TestRole");
        
        await PostForm($"/Roles/Edit/{role.Id}", new Dictionary<string, string>
        {
            { "Id", role.Id },
            { "RoleName", "TestRole" },
            { "Claims[0].Key", AppPermissionNames.CanTakeExam },
            { "Claims[0].Name", "Take Exam" },
            { "Claims[0].Description", "Allows taking exam" },
            { "Claims[0].IsSelected", "true" }
        });

        var permissionChange = await db.Changes.FirstOrDefaultAsync(c => c.Type == ChangeType.RoleGainedPermission && c.TargetRoleId == role.Id && c.TargetPermission == AppPermissionNames.CanTakeExam);
        Assert.IsNotNull(permissionChange);

        // 4. Assign role to user
        await PostForm($"/Users/ManageRoles/{user.Id}", new Dictionary<string, string>
        {
            { "AllRoles[0].RoleName", "TestRole" },
            { "AllRoles[0].IsSelected", "true" }
        }, tokenUrl: $"/Users/Edit/{user.Id}");

        var roleAssignmentChange = await db.Changes.FirstOrDefaultAsync(c => c.Type == ChangeType.UserJoinedRole && c.TargetUserId == user.Id && c.TargetRoleId == role.Id);
        Assert.IsNotNull(roleAssignmentChange);

        // 5. Test Active User Calculation
        var changeService = GetService<ChangeService>();
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        
        var report = await changeService.GetReportForMonth(start, end);
        Assert.IsTrue(report.ActiveUsers.Any(u => u.User.Id == user.Id));

        // 6. Delete user
        await PostForm($"/Users/Delete/{user.Id}", new Dictionary<string, string>());
        
        var deleteChange = await db.Changes.FirstOrDefaultAsync(c => c.Type == ChangeType.UserDeleted && c.TargetUserId == user.Id);
        Assert.IsNotNull(deleteChange);
        
        // User should STILL be in the report for this month
        report = await changeService.GetReportForMonth(start, end);
        Assert.IsTrue(report.ActiveUsers.Any(u => u.User.Id == user.Id));
    }
}
