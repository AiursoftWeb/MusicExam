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
            // Ensure school ID is available
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var levelDto in schoolDto.Subjects)
            {
                var levelName = levelDto.SubjectTitle;
                _logger.LogInformation("Processing Level: {LevelName}", levelName);

                // Find the level JSON file
                // The directory structure seems to use "1_Title" format usually, but we need to find the specific file.
                // Based on previous code: schoolDir/id_Title.json

                var levelDirName = $"{levelDto.Id}_{levelDto.SubjectTitle}";
                var levelJsonPath = Path.Combine(_dataSettings.Path, school.Name, $"{levelDirName}.json");
                var levelDirPath = Path.Combine(_dataSettings.Path, school.Name, levelDirName);

                if (!File.Exists(levelJsonPath))
                {
                    _logger.LogWarning("Level JSON file not found at {Path}. Skipping level.", levelJsonPath);
                    continue;
                }

                var levelJsonContent = await File.ReadAllTextAsync(levelJsonPath, cancellationToken);
                var levelData = JsonSerializer.Deserialize<PaperCategoriesFileDto>(levelJsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (levelData?.Data == null)
                {
                    _logger.LogWarning("Could not parse level JSON for {LevelName}.", levelName);
                    continue;
                }

                foreach (var (categoryName, papers) in levelData.Data)
                {
                    foreach (var paperDto in papers)
                    {
                        var paperId = paperDto.Id;
                        var paperTitle = paperDto.Title;

                        var paper = await dbContext.ExamPapers
                            .Include(p => p.Questions)
                            .FirstOrDefaultAsync(p => p.Id == paperId, cancellationToken: cancellationToken);

                        if (paper == null)
                        {
                            paper = new ExamPaper
                            {
                                Id = paperId,
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
                            // Update metadata if it exists
                            if (paper.Level != levelName || paper.Category != categoryName)
                            {
                                paper.Level = levelName;
                                paper.Category = categoryName;
                                _logger.LogInformation("Updating ExamPaper metadata: {Title}", paperTitle);
                            }
                        }

                        if (paper.Questions.Any())
                        {
                            // Skip importing questions if already populate
                            // But we might want to update questions? For now, stick to skip for performance/safety logic.
                            continue;
                        }

                        // Import Questions
                        // Directory structure: Data/School/LevelDir/Category/PaperTitle
                        // We need to resolve the Category Directory and Paper Directory

                        var categoryDir = Path.Combine(levelDirPath, categoryName);
                        // Category dir might not exist or might optionally have different naming?
                        // Previous code used GetBestMatchingDirectory, lets reuse that logic if possible or assume exact match first.
                        if (!Directory.Exists(categoryDir))
                        {
                            // Try to find best match? Or currently strict.
                            if (!Directory.Exists(categoryDir))
                            {
                                _logger.LogWarning("Category directory missing: {Path}", categoryDir);
                                continue;
                            }
                        }

                        var paperDir = GetBestMatchingDirectory(categoryDir, paperTitle);
                        if (string.IsNullOrEmpty(paperDir))
                        {
                            _logger.LogWarning("Paper directory not found for: {PaperTitle} in {CategoryDir}", paperTitle, categoryDir);
                            continue;
                        }

                        // Now import questions from paperDir
                        var questionFiles = Directory.GetFiles(paperDir, "question_*.json").OrderBy(f => f);
                        var questionOrder = 0;
                        foreach (var questionFile in questionFiles)
                        {
                            var questionJson = await File.ReadAllTextAsync(questionFile, cancellationToken);
                            var questionDto = JsonSerializer.Deserialize<QuestionDto>(questionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (questionDto == null) continue;

                            var questionAssets = new List<string>();
                            var questionAssetTasks = new List<Task<string>>();

                            var sourceAssetDir = Path.GetDirectoryName(questionFile)!;
                            // Assuming CopyAsset is available as private method
                            questionAssetTasks.AddRange(questionDto.LocalAudios.Select(localAudio => CopyAsset(localAudio, sourceAssetDir)));
                            questionAssetTasks.AddRange(questionDto.LocalImages.Select(localImage => CopyAsset(localImage, sourceAssetDir)));

                            questionAssets.AddRange(await Task.WhenAll(questionAssetTasks));

                            var newQuestion = new Question
                            {
                                Content = questionDto.Question,
                                Explanation = questionDto.Explanation,
                                AssetPath = JsonSerializer.Serialize(questionAssets.Where(q => !string.IsNullOrWhiteSpace(q))),
                                Paper = paper,
                                Order = questionOrder++,
                                QuestionType = questionDto.Options.Any() ? QuestionType.MultipleChoice : QuestionType.SightSinging, // Heuristic
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
                // Generate a logical path for the asset: AssetBucket/year/month/day/filename
                var now = DateTime.UtcNow;
                var fileName = Path.GetFileName(sourcePath);
                var destinationLogicalPath = $"{AssetBucket}/{now:yyyy}/{now:MM}/{now:dd}/{fileName}";
                
                return await _storageService.SaveFileFromPhysicalPath(sourcePath, destinationLogicalPath, isVault: false);
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

