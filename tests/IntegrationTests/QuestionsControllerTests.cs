using System.Net;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class QuestionsControllerTests : TestBase
{
    [TestMethod]
    public async Task TestQuestionsManagement()
    {
        // 1. Login as admin
        await LoginAsAdmin();

        var schoolId = 0;
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var school = new School { Name = "Question Test School" };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            schoolId = school.Id;
        }

        // 2. Create Paper
        var createPaperUrl = "/Questions/CreatePaper";
        var paperTitle = "Integration Test Paper " + Guid.NewGuid();
        var createResponse = await PostForm(createPaperUrl, new Dictionary<string, string>
        {
            { "SchoolId", schoolId.ToString() },
            { "Title", paperTitle },
            { "Level", "5" },
            { "Category", "Testing" }
        });

        AssertRedirect(createResponse, "/Questions");

        int paperId = 0;
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var paper = await db.ExamPapers.FirstOrDefaultAsync(p => p.Title == paperTitle);
            Assert.IsNotNull(paper);
            paperId = paper.Id;
        }

        // 3. Create Question
        var createQuestionUrl = $"/Questions/Create/{paperId}";
        var questionContent = "What is 1+1?";
        var createQResponse = await PostForm(createQuestionUrl, new Dictionary<string, string>
        {
            { "PaperId", paperId.ToString() },
            { "Content", questionContent },
            { "QuestionType", "MultipleChoice" },
            { "Explanation", "Basic math" },
            { "Options[0].Content", "2" },
            { "Options[0].IsCorrect", "true" },
            { "Options[1].Content", "3" },
            { "Options[1].IsCorrect", "false" },
            { "Options[2].Content", "" },
            { "Options[3].Content", "" }
        });
        
        AssertRedirect(createQResponse, "/Questions");

        int questionId = 0;
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var question = await db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Content == questionContent);
            Assert.IsNotNull(question);
            Assert.AreEqual(2, question.Options.Count);
            questionId = question.Id;
        }

        // 4. Edit Question
        var editQuestionUrl = $"/Questions/Edit/{questionId}";
        var newContent = "What is 2+2?";
        var editQResponse = await PostForm(editQuestionUrl, new Dictionary<string, string>
        {
            { "QuestionId", questionId.ToString() },
            { "PaperId", paperId.ToString() },
            { "Content", newContent },
            { "QuestionType", "MultipleChoice" },
            { "Explanation", "Advanced math" },
            { "Options[0].Content", "4" },
            { "Options[0].Id", "0" }, // ID is ignored in simplistic controller logic but let's pass it
            { "Options[0].IsCorrect", "true" },
            { "Options[1].Content", "5" },
            { "Options[1].IsCorrect", "false" }
        });

        AssertRedirect(editQResponse, "/Questions");

        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var question = await db.Questions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == questionId);
            Assert.IsNotNull(question);
            Assert.AreEqual(newContent, question.Content);
            Assert.AreEqual("4", question.Options.First().Content);
        }

        // 5. Delete Question
        var deleteUrl = $"/Questions/Delete/{questionId}";
        var deleteResponse = await PostForm(deleteUrl, new Dictionary<string, string>());
        AssertRedirect(deleteResponse, "/Questions");

         using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
            var question = await db.Questions.FindAsync(questionId);
            Assert.IsNull(question);
        }
    }
    
    [TestMethod]
    public async Task TestPermissionEnforcement()
    {
         // Register a normal user
        var (userId, email, password) = await RegisterNewUserAsync();
        
        // Login as user
        await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", email },
            { "Password", password }
        });

        // Try to access Questions Index
        var response = await Http.GetAsync("/Questions/Index");
        if (response.StatusCode == HttpStatusCode.Found)
        {
             AssertRedirect(response, "/Error/Code403", exact: false);
        }
        else
        {
             Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
