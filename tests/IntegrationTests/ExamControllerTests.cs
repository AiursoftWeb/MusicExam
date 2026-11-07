using System.Net;
using Aiursoft.MusicExam.Entities;
using Microsoft.EntityFrameworkCore;

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
        var postResponse = await PostForm($"/Exam/Result/{SeededExamPaperId}", new Dictionary<string, string>(), tokenUrl: $"/Exam/Take/{SeededExamPaperId}");
        Assert.AreEqual(HttpStatusCode.OK, postResponse.StatusCode);
        var postHtml = await postResponse.Content.ReadAsStringAsync();
        Assert.Contains("Result - Test Paper", postHtml);

        // Step 3: Check history
        var historyResponse = await Http.GetAsync("/Manage/ExamHistory");
        historyResponse.EnsureSuccessStatusCode();
        var historyHtml = await historyResponse.Content.ReadAsStringAsync();
        Assert.Contains("Test Paper", historyHtml);
    }
    [TestMethod]
    [DataRow("not-a-number")]
    [DataRow("2147483648")]
    [DataRow("-1")]
    [DataRow("0")]
    [DataRow("2147483647")]
    public async Task InvalidAnswerReturnsBadRequestWithoutSavingSubmission(string answer)
    {
        await LoginAsAdmin();
        var question = await AddQuestion();
        var response = await PostForm($"/Exam/Result/{SeededExamPaperId}",
            new Dictionary<string, string> { [$"question-{question.Id}"] = answer },
            tokenUrl: $"/Exam/Take/{SeededExamPaperId}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        Assert.AreEqual(0, await db.ExamPaperSubmissions.CountAsync(submission => submission.PaperId == SeededExamPaperId));
        Assert.AreEqual(0, await db.QuestionSubmissions.CountAsync(submission => submission.QuestionId == question.Id));
    }

    [TestMethod]
    public async Task AnOptionFromAnotherQuestionIsRejected()
    {
        await LoginAsAdmin();
        var question = await AddQuestion();
        var otherQuestion = await AddQuestion();
        var response = await PostForm($"/Exam/Result/{SeededExamPaperId}",
            new Dictionary<string, string>
            {
                [$"question-{question.Id}"] = otherQuestion.Options.Single().Id.ToString()
            }, tokenUrl: $"/Exam/Take/{SeededExamPaperId}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task ValidAnswerStillReceivesCredit()
    {
        await LoginAsAdmin();
        var question = await AddQuestion();
        var response = await PostForm($"/Exam/Result/{SeededExamPaperId}",
            new Dictionary<string, string>
            {
                [$"question-{question.Id}"] = question.Options.Single().Id.ToString()
            }, tokenUrl: $"/Exam/Take/{SeededExamPaperId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        Assert.AreEqual(1, (await db.ExamPaperSubmissions.SingleAsync(submission => submission.PaperId == SeededExamPaperId)).Score);
    }

    [TestMethod]
    public async Task UnansweredQuestionRemainsAValidSubmission()
    {
        await LoginAsAdmin();
        await AddQuestion();
        var response = await PostForm($"/Exam/Result/{SeededExamPaperId}",
            new Dictionary<string, string>(), tokenUrl: $"/Exam/Take/{SeededExamPaperId}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Question> AddQuestion()
    {
        using var scope = Server!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        var question = new Question
        {
            PaperId = SeededExamPaperId,
            Content = "Choose the correct answer.",
            QuestionType = QuestionType.SingleChoice,
            Options = [new Option { Content = "Correct answer", IsCorrect = true }]
        };
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }
}
