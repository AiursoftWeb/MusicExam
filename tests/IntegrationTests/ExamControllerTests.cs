using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.ExamViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class ExamControllerTests : TestBase
{
    [TestMethod]
    public async Task TakeExam_ShouldShowProgressBar()
    {
        // Step 1: Login as admin
        await LoginAsAdmin();

        // Step 2: Access the Take page
        var response = await Http.GetAsync($"/Exam/Take/{SeededExamPaperId}");
        response.EnsureSuccessStatusCode();

        // Step 3: Verify the progress bar is present in the HTML
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("id=\"progress-bar\"", html);
        Assert.Contains("class=\"progress-bar", html);
    }

    [TestMethod]
    public async Task History_ShouldShowSubmission()
    {
        // Step 1: Login as admin
        await LoginAsAdmin();

        // Step 2: Submit an exam
        var postResponse = await PostForm($"/Exam/Take/{SeededExamPaperId}", new Dictionary<string, string>());
        Assert.AreEqual(HttpStatusCode.OK, postResponse.StatusCode);
        var postHtml = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("Result - Test Paper", postHtml);

        // Step 3: Check history
        var historyResponse = await Http.GetAsync("/Exam/History");
        historyResponse.EnsureSuccessStatusCode();
        var historyHtml = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("Test Paper", historyHtml);
    }
}
