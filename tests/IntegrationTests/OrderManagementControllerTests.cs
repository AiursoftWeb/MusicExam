using System.Net;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.OrderManagementViewModels;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class OrderManagementControllerTests : TestBase
{
    [TestMethod]
    public async Task Index_AsAdmin_ReturnsSuccess()
    {
        await LoginAsAdmin();
        var response = await Http.GetAsync("/OrderManagement");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Manage Display Order", content);
    }

    [TestMethod]
    public async Task Index_Unauthenticated_Redirects()
    {
        var response = await Http.GetAsync("/OrderManagement");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateSchoolOrder_UpdatesDisplayOrder()
    {
        await LoginAsAdmin();

        int schoolId;
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var school = await db.Schools.FirstAsync();
            schoolId = school.Id;
        }

        var dto = new UpdateOrderDto
        {
            Items =
            [
                new OrderItem { Id = schoolId, DisplayOrder = 42 }
            ]
        };

        var response = await Http.PostAsJsonAsync("/OrderManagement/UpdateSchoolOrder", dto);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var school = await db.Schools.FindAsync(schoolId);
            Assert.IsNotNull(school);
            Assert.AreEqual(42, school.DisplayOrder);
        }
    }

    [TestMethod]
    public async Task UpdatePaperOrder_UpdatesDisplayOrder()
    {
        await LoginAsAdmin();

        var dto = new UpdateOrderDto
        {
            Items =
            [
                new OrderItem { Id = SeededExamPaperId, DisplayOrder = 99 }
            ]
        };

        var response = await Http.PostAsJsonAsync("/OrderManagement/UpdatePaperOrder", dto);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var paper = await db.ExamPapers.FindAsync(SeededExamPaperId);
            Assert.IsNotNull(paper);
            Assert.AreEqual(99, paper.DisplayOrder);
        }
    }
}
