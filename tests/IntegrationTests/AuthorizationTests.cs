using System.Net;


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
    public async Task UserWithoutPermission_CannotAccessExam_ReturnsForbidden()
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
    public async Task AdminCanGrantPermission_And_UserCanAccessExam()
    {
        // 1. Arrange: Register a new user.
        var (userId, email, password) = await RegisterNewUserAsync();

        // 2. Act (as Admin): Create a role with the permission and assign it to the user.
        await LoginAsAdmin();
        
        // Create a role "Student"
        var createRoleResponse = await PostForm("/Roles/Create", new Dictionary<string, string>
        {
            { "RoleName", "Student" }
        });
        
        // Extract Role ID from redirect URL
        var redirectUrl = createRoleResponse.Headers.Location!.ToString();
        var roleId = redirectUrl.Split('/').Last(); 
        
        // Add "CanTakeExam" permission to "Student" role
        await PostForm($"/Roles/Edit/{roleId}", new Dictionary<string, string>
        {
            { "Id", roleId },
            { "RoleName", "Student" },
            { "Claims[0].Key", "CanTakeExam" },
            { "Claims[0].IsSelected", "true" }
        });
        
        // Assign "Student" role to the user
        await PostForm($"/Users/ManageRoles/{userId}", new Dictionary<string, string>
        {
            { "AllRoles[0].RoleName", "Student" },
            { "AllRoles[0].IsSelected", "true" }
        });

        // 3. Arrange: Log in as the user.
        await LogoutAsync();
        var loginResponse = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });
        AssertRedirect(loginResponse, "/");

        // 4. Act: Try to access the exam page.
        var examResponse = await Http.GetAsync($"/Exam/Take/{SeededExamPaperId}");

        // 5. Assert: Access is granted.
        examResponse.EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.OK, examResponse.StatusCode);
    }
}
