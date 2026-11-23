using System.Net;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class AccountingControllerTests : TestBase
{
    [TestMethod]
    public async Task Index_AsAdmin_ReturnsSuccess()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/Accounting");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Accounting", content);
    }

    [TestMethod]
    public async Task Index_Unauthenticated_RedirectsToLogin()
    {
        var response = await Http.GetAsync("/Accounting");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [TestMethod]
    public async Task Index_AsRegularUser_ReturnsForbiddenOrRedirects()
    {
        await RegisterNewUserAsync();
        var response = await Http.GetAsync("/Accounting");
        // Regular user lacks CanViewAccounting policy
        Assert.IsTrue(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Found);
    }
}
