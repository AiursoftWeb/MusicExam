using System.Text.Json;
using Aiursoft.MusicExam.Configuration;
using Aiursoft.MusicExam.Models.DataTransferModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Services.FileStorage;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.MusicExam.Services;

public class DataImporter : ISingletonDependency
{
    private readonly ILogger<DataImporter> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataSettings _dataSettings;
    private readonly StorageService _storageService;
    private const string AssetBucket = "importer-assets";

    public DataImporter(
        ILogger<DataImporter> logger,
        IServiceProvider serviceProvider,
        IOptions<DataSettings> dataSettings,
        StorageService storageService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dataSettings = dataSettings.Value;
        _storageService = storageService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data importer is starting.");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

        var indexJsonPath = Path.Combine(_dataSettings.Path, "index.json");
        if (!File.Exists(indexJsonPath))
        {
            throw new FileNotFoundException($"Data importer: index.json not found at {indexJsonPath}.");
        }

        var json = await File.ReadAllTextAsync(indexJsonPath, cancellationToken);
        var indexData = JsonSerializer.Deserialize<IndexJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (indexData?.Data == null)
        {
            throw new InvalidDataException("Data importer: Could not parse index.json.");
        }

        _logger.LogInformation("Found {Count} schools in index.json.", indexData.Data.Count);

        foreach (var schoolDto in indexData.Data)
        {
            var school = await dbContext.Schools.FirstOrDefaultAsync(s => s.Name == schoolDto.SubjectTitle, cancellationToken: cancellationToken);
            if (school == null)
            {
                school = new School { Name = schoolDto.SubjectTitle };
                await dbContext.Schools.AddAsync(school, cancellationToken);
                _logger.LogInformation("School '{SchoolName}' not found in DB. Creating it.", schoolDto.SubjectTitle);
            }

            foreach (var paperDto in schoolDto.Subjects)
            {
                var isNewPaper = false;
                var paper = await dbContext.ExamPapers
                    .Include(p => p.Questions)
                    .FirstOrDefaultAsync(p => p.Id == paperDto.Id, cancellationToken: cancellationToken);

                if (paper == null)
                {
                    paper = new ExamPaper
                    {
                        Id = paperDto.Id,
                        Title = paperDto.SubjectTitle,
                        School = school
                    };
                    await dbContext.ExamPapers.AddAsync(paper, cancellationToken);
                    _logger.LogInformation("Paper '{PaperTitle}' not found in DB. Creating it.", paperDto.SubjectTitle);
                    isNewPaper = true;
                }

                if (!isNewPaper && paper.Questions.Any())
                {
                    _logger.LogInformation("Paper '{PaperTitle}' already exists in DB. Skipping question import.", paperDto.SubjectTitle);
                    continue;
                }

                var paperDir = Path.Combine(_dataSettings.Path, school.Name, $"{paper.Id}_{paper.Title}");
                var paperJson = Path.Combine(_dataSettings.Path, school.Name, $"{paper.Id}_{paper.Title}.json");
                if (!File.Exists(paperJson))
                {
                    throw new FileNotFoundException($"Paper JSON file not found at {paperJson}.");
                }

                var paperJsonContent = await File.ReadAllTextAsync(paperJson, cancellationToken);
                var paperCategories = JsonSerializer.Deserialize<PaperCategoriesFileDto>(paperJsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (paperCategories?.Data == null)
                {
                    throw new InvalidDataException($"Could not parse paper category JSON for {paper.Title}.");
                }

                var questionOrder = 0;
                foreach (var (categoryName, questionGroups) in paperCategories.Data)
                {
                    foreach (var questionGroup in questionGroups)
                    {
                        var categoryDir = Path.Combine(paperDir, categoryName);
                        if (!Directory.Exists(categoryDir))
                        {
                            throw new DirectoryNotFoundException($"Category directory not found: {categoryDir}");
                        }

                        var questionGroupDir = GetBestMatchingDirectory(categoryDir, questionGroup.Title);
                        if (string.IsNullOrEmpty(questionGroupDir))
                        {
                            _logger.LogWarning("Question group directory not found: {Path}. Searched in {ParentDir}. Skipping.", Path.Combine(categoryDir, questionGroup.Title), categoryDir);
                            continue;
                        }

                        var questionFiles = Directory.GetFiles(questionGroupDir, "question_*.json").OrderBy(f => f);
                        foreach (var questionFile in questionFiles)
                        {
                            var questionJson = await File.ReadAllTextAsync(questionFile, cancellationToken);
                            var questionDto = JsonSerializer.Deserialize<QuestionDto>(questionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (questionDto == null) continue;

                            var questionAssets = new List<string>();
                            var questionAssetTasks = new List<Task<string>>();

                            var sourceAssetDir = Path.GetDirectoryName(questionFile)!;
                            questionAssetTasks.AddRange(questionDto.LocalAudios.Select(localAudio => CopyAsset(localAudio, sourceAssetDir)));
                            questionAssetTasks.AddRange(questionDto.LocalImages.Select(localImage => CopyAsset(localImage, sourceAssetDir)));

                            questionAssets.AddRange(await Task.WhenAll(questionAssetTasks));

                            var newQuestion = new Question
                            {
                                Content = questionDto.Question,
                                AssetPath = JsonSerializer.Serialize(questionAssets.Where(q => !string.IsNullOrWhiteSpace(q))),
                                Paper = paper,
                                Order = questionOrder++,
                                QuestionType = questionDto.Options.Any() ? QuestionType.MultipleChoice : QuestionType.SightSinging,
                                Options = new List<Option>()
                            };

                            var correctAnswers = questionDto.CorrectAnswer.Split(',').Select(a => a.Trim()).ToList();

                            var optionDisplayOrder = 0;
                            foreach (var optionDto in questionDto.Options)
                            {
                                string optionContent = optionDto.Content;
                                if (optionDto.Type != "text" && !string.IsNullOrEmpty(optionDto.LocalContent))
                                {
                                    optionContent = await CopyAsset(optionDto.LocalContent, sourceAssetDir);
                                }

                                var newOption = new Option
                                {
                                    Content = optionContent,
                                    DisplayOrder = optionDisplayOrder++,
                                    Question = newQuestion,
                                    IsCorrect = correctAnswers.Contains(IndexToLetter(optionDto.Value))
                                };
                                newQuestion.Options.Add(newOption);
                            }
                            paper.Questions.Add(newQuestion);
                        }
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Data importer has finished processing all files.");
    }

    private string IndexToLetter(string? index)
    {
        if (string.IsNullOrWhiteSpace(index)) return "?";
        return int.TryParse(index, out var i) ? ((char)('A' + i)).ToString() : "?";
    }

    private async Task<string> CopyAsset(string relativeAssetPath, string sourceDir)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath)) return string.Empty;

        if (relativeAssetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeAssetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativeAssetPath;
        }

        var sourcePath = Path.Combine(sourceDir, relativeAssetPath);
        if (File.Exists(sourcePath))
        {
            try
            {
                return await _storageService.SaveFileFromPhysicalPath(sourcePath, AssetBucket);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Could not copy asset file from {Path}!", sourcePath);
                return string.Empty;
            }
        }

        _logger.LogWarning("Asset file not found at {Path}!", sourcePath);
        return string.Empty;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data importer is stopping.");
        return Task.CompletedTask;
    }
    private string? GetBestMatchingDirectory(string parentDir, string expectedName)
    {
        var exactPath = Path.Combine(parentDir, expectedName);
        if (Directory.Exists(exactPath))
        {
            return exactPath;
        }

        var expectedNormalized = NormalizeName(expectedName);
        var subDirectories = Directory.GetDirectories(parentDir);

        foreach (var dir in subDirectories)
        {
            var dirName = Path.GetFileName(dir);
            if (NormalizeName(dirName) == expectedNormalized)
            {
                return dir;
            }
        }

        return null;
    }

    private string NormalizeName(string name)
    {
        return name
            .Replace("(", "")
            .Replace(")", "")
            .Replace("（", "")
            .Replace("）", "")
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("•", "")
            .Replace(".", "")
            .Replace("，", "")
            .Replace(",", "")
            .Trim();
    }
}

