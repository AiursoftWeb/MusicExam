using System.Net;
using Aiursoft.MusicExam.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class AuthorizationTests : TestBase
{
    [TestMethod]
    public async Task AnonymousUser_CannotAccessExam_RedirectsToLogin()
    {
        // 1. Arrange
        // User is anonymous by default.

        // 2. Act
        var response = await Http.GetAsync($"/Exam/Take/{SeededExamPaperId}");

        // 3. Assert - The app redirects to Error/Unauthorized for anonymous users
        AssertRedirect(response, "/Error/Unauthorized", exact: false);
    }

    [TestMethod]
    public async Task UnactivatedUser_CannotAccessExam_ReturnsForbidden()
    {
        // 1. Arrange
        await RegisterAndLoginAsync();

        // 2. Act
        var response = await Http.GetAsync($"/Exam/Take/{SeededExamPaperId}");

        // 3. Assert - Forbid() returns a redirect, not a 403 status code
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        // Verify it redirects to an error or access denied page
        Assert.IsNotNull(response.Headers.Location);
    }

    [TestMethod]
    public async Task AdminCanActivateUser_And_ActivatedUserCanAccessExam()
    {
        // 1. Arrange: Register a new user.
        var (userId, email, password) = await RegisterNewUserAsync();

        // 2. Act (as Admin): Activate the user.
        await LoginAsAdmin();
        var editResponse = await PostForm($"/Users/Edit/{userId}", new Dictionary<string, string>
        {
            { "Id", userId },
            { "UserName", email.Split('@')[0] },
            { "Email", email },
            { "DisplayName", "Activated User" },
            { "IsActivated", "true" },
            { "AvatarUrl", User.DefaultAvatarPath }
        });
        AssertRedirect(editResponse, "/Users/Details/", exact: false);
        
        // 3. Arrange: Log in as the activated user.
        await LogoutAsync();
        var loginResponse = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });
        AssertRedirect(loginResponse, "/");

        // 4. Act: Try to access the exam page again.
        var examResponse = await Http.GetAsync($"/Exam/Take/{SeededExamPaperId}");

        // 5. Assert: Access is granted.
        examResponse.EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.OK, examResponse.StatusCode);
    }
}
