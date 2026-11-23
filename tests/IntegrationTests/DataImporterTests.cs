using System.Text.Json;
using Aiursoft.MusicExam.Configuration;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.DataTransferModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.MusicExam.Services.FileStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

[TestClass]
public class DataImporterTests : TestBase
{
    [TestMethod]
    public async Task StartAsync_ImportsQuestionsAndHandlesExistingData()
    {
        var tempDir = Directory.CreateTempSubdirectory("MusicExamImporterTest-").FullName;
        try
        {
            // Structure: rootPath / School / 01_Level / Category / Paper
            var schoolDir = Path.Combine(tempDir, "Importer Test School");
            var levelDir = Path.Combine(schoolDir, "01_Grade 1");
            var categoryDir = Path.Combine(levelDir, "Theory");
            var paperDir = Path.Combine(categoryDir, "Paper 2026 A");
            Directory.CreateDirectory(paperDir);

            // Dummy audio / image files
            var dummyImg = Path.Combine(paperDir, "q1.png");
            await File.WriteAllBytesAsync(dummyImg, [1, 2, 3, 4]);

            var questionDto = new QuestionDto
            {
                Question = "What note is middle C?",
                Explanation = "Middle C is C4",
                CorrectAnswer = "A",
                Options =
                [
                    new OptionDto { Value = "A", Content = "C4", Type = "text" },
                    new OptionDto { Value = "B", Content = "D4", Type = "text" }
                ],
                LocalImages = ["q1.png"],
                LocalAudios = []
            };

            var qJson = JsonSerializer.Serialize(questionDto);
            await File.WriteAllTextAsync(Path.Combine(paperDir, "question_1.json"), qJson);

            using var scope = Server!.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataImporter>>();
            var storageService = scope.ServiceProvider.GetRequiredService<StorageService>();
            var dataOptions = Options.Create(new DataSettings { Path = tempDir });

            var importer = new DataImporter(logger, Server.Services, dataOptions, storageService);

            // First run - imports new data
            await importer.StartAsync(CancellationToken.None);

            using (var verifyScope = Server.Services.CreateScope())
            {
                var db = verifyScope.ServiceProvider.GetRequiredService<TemplateDbContext>();
                var importedSchool = await db.Schools.FirstOrDefaultAsync(s => s.Name == "Importer Test School");
                Assert.IsNotNull(importedSchool);

                var importedPaper = await db.ExamPapers
                    .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                    .FirstOrDefaultAsync(p => p.Title == "Paper 2026 A" && p.SchoolId == importedSchool.Id);
                Assert.IsNotNull(importedPaper);
                Assert.AreEqual("Grade 1", importedPaper.Level);
                Assert.AreEqual("Theory", importedPaper.Category);
                Assert.AreEqual(1, importedPaper.Questions.Count);

                var q = importedPaper.Questions.First();
                Assert.AreEqual("What note is middle C?", q.Content);
                Assert.AreEqual(2, q.Options.Count);
            }

            // Second run - verifies update / existing assets branch
            await importer.StartAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
