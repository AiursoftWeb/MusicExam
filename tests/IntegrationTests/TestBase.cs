using System.Net;
using System.Text.RegularExpressions;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.MusicExam.Tests.IntegrationTests;

public abstract class TestBase
{
    protected readonly int Port;
    protected readonly HttpClient Http;
    protected IHost? Server;

    protected TestBase()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = false
        };
        Port = Network.GetAvailablePort();
        Http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://localhost:{Port}")
        };
    }

    [TestInitialize]
    public virtual async Task CreateServer()
    {
        Server = await AppAsync<Startup>([], port: Port);
        await Server.UpdateDbAsync<TemplateDbContext>();
        await Server.SeedAsync(); // Seeds the default admin user and roles
        
        // ============================ Mock Data for Tests (Start) ============================
        // This section adds fake data specifically for integration tests.
        // It's separated for easy removal or modification later.
        using var scope = Server.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TemplateDbContext>();
        await SeedMockDataAsync(dbContext);
        // ============================ Mock Data for Tests (End) ============================
        
        await Server.StartAsync();
    }

    protected int? SeededExamPaperId { get; private set; }

    protected async Task SeedMockDataAsync(TemplateDbContext dbContext)
    {
        // Mock School
        var school = new School { Name = "Test Academy" };
        await dbContext.Schools.AddAsync(school);

        // Mock Exam Paper - Do not hardcode ID, let EF auto-generate it
        var paper = new ExamPaper
        {
            Title = "Basic Music Theory Exam",
            School = school
        };
        await dbContext.ExamPapers.AddAsync(paper);

        // Mock Question 1 (Multiple Choice)
        var question1 = new Question
        {
            Content = "What note is this?",
            AssetPath = "[\"/importer-assets/images/note_c.png\"]", // Example asset path
            Paper = paper,
            QuestionType = QuestionType.MultipleChoice,
            Order = 1
        };
        await dbContext.Questions.AddAsync(question1);

        await dbContext.Options.AddAsync(new Option { Content = "C", Question = question1, IsCorrect = false, DisplayOrder = 0 });
        await dbContext.Options.AddAsync(new Option { Content = "D", Question = question1, IsCorrect = true, DisplayOrder = 1 });
        await dbContext.Options.AddAsync(new Option { Content = "E", Question = question1, IsCorrect = false, DisplayOrder = 2 });
        await dbContext.Options.AddAsync(new Option { Content = "F", Question = question1, IsCorrect = false, DisplayOrder = 3 });

        // Mock Question 2 (Sight Singing)
        var question2 = new Question
        {
            Content = "Sing this melody (Audio asset).",
            AssetPath = "[\"/importer-assets/audio/melody.mp3\"]", // Example asset path
            Paper = paper,
            QuestionType = QuestionType.SightSinging,
            Order = 2
        };
        await dbContext.Questions.AddAsync(question2);
        
        await dbContext.SaveChangesAsync();
        
        // Store the generated ID for use in tests
        SeededExamPaperId = paper.Id;
    }

    [TestCleanup]
    public virtual async Task CleanServer()
    {
        if (Server == null) return;
        await Server.StopAsync();
        Server.Dispose();
    }

    protected async Task<string> GetAntiCsrfToken(string url)
    {
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            response = await Http.GetAsync("/");
        }
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html,
            @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)"" />");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find anti-CSRF token on page: {url}");
        }

        return match.Groups[1].Value;
    }

    protected async Task<HttpResponseMessage> PostForm(string url, Dictionary<string, string> data, string? tokenUrl = null, bool includeToken = true)
    {
        if (includeToken && !data.ContainsKey("__RequestVerificationToken"))
        {
            var token = await GetAntiCsrfToken(tokenUrl ?? url);
            data["__RequestVerificationToken"] = token;
        }
        return await Http.PostAsync(url, new FormUrlEncodedContent(data));
    }

    protected void AssertRedirect(HttpResponseMessage response, string expectedLocation, bool exact = true)
    {
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        var actualLocation = response.Headers.Location?.OriginalString ?? string.Empty;
        var baseUri = Http.BaseAddress?.ToString() ?? "____";
        
        if (actualLocation.StartsWith(baseUri))
        {
            actualLocation = actualLocation.Substring(baseUri.Length - 1); // Keep the leading slash
        }

        if (exact)
        {
            Assert.AreEqual(expectedLocation, actualLocation, $"Expected redirect to {expectedLocation}, but was {actualLocation}");
        }
        else
        {
            Assert.StartsWith(expectedLocation, actualLocation);
        }
    }

    protected async Task LoginAsAdmin()
    {
        var loginResponse = await PostForm("/Account/Login", new Dictionary<string, string>
        {
            { "EmailOrUserName", "admin@default.com" },
            { "Password", "admin123" }
        });
        Assert.AreEqual(HttpStatusCode.Found, loginResponse.StatusCode);
    }

    protected async Task LogoutAsync()
    {
        var response = await Http.GetAsync("/Account/Logout");
        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
    }

    protected async Task<(string userId, string email, string password)> RegisterNewUserAsync()
    {
        var email = $"test-{Guid.NewGuid()}@aiursoft.com";
        var password = "Test-Password-123";
        var userName = email.Split('@')[0];

        // Ensure we are not logged in before trying to register a new user.
        await LogoutAsync();

        var token = await GetAntiCsrfToken("/Account/Register");
        
        var registerResponse = await PostForm("/Account/Register", new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "UserName", userName },
            { "DisplayName", userName },
            { "Email", email },
            { "Password", password },
            { "ConfirmPassword", password }
        });

        if (registerResponse.StatusCode != HttpStatusCode.Found ||
            !registerResponse.Headers.Location!.OriginalString.Contains("/Account/Login"))
        {
            var content = await registerResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Registration failed. Status: {registerResponse.StatusCode}. Location: {registerResponse.Headers.Location}. Content: {content}");
        }

        // To get the user ID, we need to log in as an admin and find the user.
        await LoginAsAdmin();
        var scope = Server!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            Assert.Fail($"Could not find newly registered user with email {email}");
        }
        await LogoutAsync();
        return (user.Id, email, password);
    }

    protected async Task<(string email, string password)> RegisterAndLoginAsync()
    {
        var email = $"test-{Guid.NewGuid()}@aiursoft.com";
        var password = "Test-Password-123";

        var registerResponse = await PostForm("/Account/Register", new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "ConfirmPassword", password }
        });
        Assert.AreEqual(HttpStatusCode.Found, registerResponse.StatusCode);

        return (email, password);
    }
}
