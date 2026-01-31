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
        _logger.LogInformation("Data importer is starting...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();

        var rootPath = _dataSettings.Path;
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Data root directory not found at {rootPath}.");
        }

        // Level 1: Schools
        var schoolDirs = Directory.GetDirectories(rootPath).OrderBy(d => d, new NaturalSortComparer()).ToArray();
        _logger.LogInformation("Found {Count} schools directories.", schoolDirs.Length);

        foreach (var schoolDir in schoolDirs)
        {
            var schoolName = Path.GetFileName(schoolDir);
            var school = await dbContext.Schools.FirstOrDefaultAsync(s => s.Name == schoolName, cancellationToken);
            if (school == null)
            {
                school = new School { Name = schoolName };
                await dbContext.Schools.AddAsync(school, cancellationToken);
                _logger.LogInformation("Creating School: {SchoolName}", schoolName);
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            // Level 2: Levels
            var levelDirs = Directory.GetDirectories(schoolDir).OrderBy(d => d, new NaturalSortComparer()).ToArray();
            foreach (var levelDir in levelDirs)
            {
                var levelDirName = Path.GetFileName(levelDir);
                var levelName = levelDirName.Contains('_')
                    ? levelDirName.Split('_', 2)[1]
                    : levelDirName;

                // Level 3: Categories
                var categoryDirs = Directory.GetDirectories(levelDir).OrderBy(d => d, new NaturalSortComparer()).ToArray();
                foreach (var categoryDir in categoryDirs)
                {
                    var categoryName = Path.GetFileName(categoryDir);

                    // Level 4: Exam Papers
                    var paperDirs = Directory.GetDirectories(categoryDir).OrderBy(d => d, new NaturalSortComparer()).ToArray();
                    foreach (var paperDir in paperDirs)
                    {
                        var paperTitle = Path.GetFileName(paperDir);

                        var paper = await dbContext.ExamPapers
                            .Include(p => p.Questions)
                            .FirstOrDefaultAsync(p => p.Title == paperTitle && p.SchoolId == school.Id, cancellationToken);

                        if (paper == null)
                        {
                            paper = new ExamPaper
                            {
                                Title = paperTitle,
                                School = school,
                                Level = levelName,
                                Category = categoryName
                            };
                            await dbContext.ExamPapers.AddAsync(paper, cancellationToken);
                            _logger.LogInformation("Creating ExamPaper: {Title} (Level: {Level}, Category: {Category})", paperTitle, levelName, categoryName);
                        }
                        else
                        {
                            // Update metadata
                            if (paper.Level != levelName || paper.Category != categoryName)
                            {
                                paper.Level = levelName;
                                paper.Category = categoryName;
                                _logger.LogInformation("Updating ExamPaper metadata: {Title}", paperTitle);
                            }
                        }

                        // Check if we need to import questions
                        if (paper.Questions.Any())
                        {
                            continue;
                        }

                        // Import Questions
                        var questionFiles = Directory.GetFiles(paperDir, "question_*.json")
                            .Concat(Directory.GetFiles(paperDir, "sight_singing.json"))
                            .Select(f => new { Path = f, Index = GetQuestionIndex(Path.GetFileName(f)) })
                            .OrderBy(x => x.Index)
                            .Select(x => x.Path)
                            .ToArray();
                        var questionOrder = 0;
                        foreach (var questionFile in questionFiles)
                        {
                            var questionJson = await File.ReadAllTextAsync(questionFile, cancellationToken);
                            var questionDto = JsonSerializer.Deserialize<QuestionDto>(questionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (questionDto == null) continue;

                            var questionAssets = new List<string>();
                            var questionAssetTasks = new List<Task<string>>();
                            var sourceAssetDir = paperDir;

                            questionAssetTasks.AddRange(questionDto.LocalAudios.Select(localAudio => CopyAsset(localAudio, sourceAssetDir)));
                            questionAssetTasks.AddRange(questionDto.LocalImages.Select(localImage => CopyAsset(localImage, sourceAssetDir)));

                            questionAssets.AddRange(await Task.WhenAll(questionAssetTasks));

                            var newQuestion = new Question
                            {
                                Content = questionDto.Question,
                                Explanation = questionDto.Explanation,
                                AssetPath = JsonSerializer.Serialize(questionAssets.Where(q => !string.IsNullOrWhiteSpace(q))),
                                Paper = paper,
                                Order = questionOrder++, // Order based on file name sorting
                                QuestionType = questionDto.Options.Any() ? QuestionType.MultipleChoice : QuestionType.SightSinging,
                                Options = new List<Option>()
                            };

                            var correctAnswers = questionDto.CorrectAnswer?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>();

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

    private int GetQuestionIndex(string fileName)
    {
        try
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var parts = nameWithoutExtension.Split('_');
            if (parts.Length >= 2 && int.TryParse(parts.Last(), out var index))
            {
                return index;
            }
        }
        catch
        {
            // Ignore
        }
        return int.MaxValue;
    }

    private async Task<string> CopyAsset(string relativeAssetPath, string sourceDir)
    {
        if (string.IsNullOrWhiteSpace(relativeAssetPath)) return string.Empty;

        if (relativeAssetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeAssetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return relativeAssetPath;
        }

        // Try to find the file
        var fullSourceDir = Path.GetFullPath(sourceDir);
        var sourcePath = Path.GetFullPath(Path.Combine(fullSourceDir, relativeAssetPath));

        if (!sourcePath.StartsWith(fullSourceDir, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Potential path traversal attempt in asset path: {Path}", relativeAssetPath);
            return string.Empty;
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Asset file not found at {sourcePath}!", sourcePath);
        }

        try
        {
            var now = DateTime.UtcNow;
            var fileName = Path.GetFileName(sourcePath);
            var destinationLogicalPath = $"{AssetBucket}/{now:yyyy}/{now:MM}/{now:dd}/{fileName}";

            return await _storageService.SaveFileFromPhysicalPath(sourcePath, destinationLogicalPath, isVault: false);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Could not copy asset file from {Path}!", sourcePath);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data importer is stopping.");
        return Task.CompletedTask;
    }
}
