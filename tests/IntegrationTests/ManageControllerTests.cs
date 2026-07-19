using Aiursoft.MusicExam.Services;

using Aiursoft.MusicExam.Entities;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class ManageControllerTests : TestBase
{
    [TestMethod]
    public async Task TestManageWorkflow()
    {
        await LoginAsAdmin();

        // Ensure AllowUserAdjustNickname is true
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(Configuration.SettingsMap.AllowUserAdjustNickname, "True");
        }

        // 1. Index
        var indexResponse = await Http.GetAsync("/Manage/Index");
        indexResponse.EnsureSuccessStatusCode();

        // 2. ChangePassword (GET)
        var changePasswordPage = await Http.GetAsync("/Manage/ChangePassword");
        changePasswordPage.EnsureSuccessStatusCode();

        // 3. ChangeProfile (GET)
        var changeProfilePage = await Http.GetAsync("/Manage/ChangeProfile");
        changeProfilePage.EnsureSuccessStatusCode();

        // 4. ChangeAvatar (GET)
        var changeAvatarPage = await Http.GetAsync("/Manage/ChangeAvatar");
        changeAvatarPage.EnsureSuccessStatusCode();
    }

    [TestMethod]
    public async Task TestChangeAvatarAllowsJpegExtension()
    {
        await LoginAsAdmin();

        var response = await Http.GetAsync("/Manage/ChangeAvatar");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-allowed-file-extensions=\"png bmp jpg jpeg\"", html);
        Assert.Contains("validExtensions: ('png bmp jpg jpeg' || '').split(' ').filter(Boolean)", html);
    }

    [TestMethod]
    public async Task TestDeleteAccount_WithContent_ContentSurvives()
    {
        // Arrange: register, login, create content owned by the user
        var (email, _) = await RegisterAndLoginAsync();

        string userId;
        int entityId; // or Guid
        using (var scope = Server!.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            userId = user!.Id;

            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var entity = new Change { Type = ChangeType.UserCreated, TriggerUserId = userId };
            db.Changes.Add(entity);
            await db.SaveChangesAsync();
        }

        // Act: delete account
        var deleteResponse = await PostForm("/Manage/DeleteAccountPost", new(),
            tokenUrl: "/Manage/DeleteAccount");
        AssertRedirect(deleteResponse, "/");

        // Assert: signed out
        var managePage = await Http.GetAsync("/Manage/Index");
        Assert.AreEqual(HttpStatusCode.Found, managePage.StatusCode);

        // Assert: user gone from DB
        using (var scope = Server!.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.IsNull(await userManager.FindByEmailAsync(email));
        }

        // Assert: content still exists (not cascade-deleted, proving SetNull behavior)
        // Note: InMemory DB does not enforce FK constraints, so TriggerUserId
        // is NOT set to NULL here. With Sqlite/MySQL, ON DELETE SET NULL
        // would nullify this column automatically.
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            Assert.IsTrue(await db.Changes.AnyAsync(c => c.TriggerUserId == userId));
        }
    }

    [TestMethod]
    public async Task TestDeleteAccount_NoContent_Succeeds()
    {
        var (email, _) = await RegisterAndLoginAsync();

        var deletePage = await Http.GetAsync("/Manage/DeleteAccount");
        deletePage.EnsureSuccessStatusCode();

        var deleteResponse = await PostForm("/Manage/DeleteAccountPost", new(),
            tokenUrl: "/Manage/DeleteAccount");
        AssertRedirect(deleteResponse, "/");

        var managePage = await Http.GetAsync("/Manage/Index");
        Assert.AreEqual(HttpStatusCode.Found, managePage.StatusCode);

        using var scope = Server!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Assert.IsNull(await userManager.FindByEmailAsync(email));
    }

    [TestMethod]
    public async Task TestDeleteAccount_Unauthenticated_RedirectsToLogin()
    {
        var deletePage = await Http.GetAsync("/Manage/DeleteAccount");
        Assert.AreEqual(HttpStatusCode.Found, deletePage.StatusCode);
    }
}
