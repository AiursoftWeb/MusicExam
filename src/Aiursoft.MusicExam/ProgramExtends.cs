using Aiursoft.CSTools.Tools;
using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Aiursoft.MusicExam.Services;
using Aiursoft.MusicExam.Services.FileStorage;

namespace Aiursoft.MusicExam;

public static class ProgramExtends
{
    private static async Task SyncChangeLogs(TemplateDbContext dbContext, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ChangeMessageFormatter messageFormatter)
    {
        var roles = await roleManager.Roles.ToListAsync();
        foreach (var role in roles)
        {
            var claims = await roleManager.GetClaimsAsync(role);
            if (claims.Any(c => c.Type == AppPermissions.Type && c.Value == AppPermissionNames.CanTakeExam))
            {
                // Ensure RoleGainedPermission event exists
                var roleLogExists = await dbContext.Changes.AnyAsync(c =>
                    c.Type == ChangeType.RoleGainedPermission &&
                    c.TargetRoleId == role.Id &&
                    c.TargetPermission == AppPermissionNames.CanTakeExam);

                if (!roleLogExists)
                {
                    dbContext.Changes.Add(new Change
                    {
                        Type = ChangeType.RoleGainedPermission,
                        TargetRoleId = role.Id,
                        TargetPermission = AppPermissionNames.CanTakeExam,
                        CreateTime = DateTime.MinValue, // Historic permission
                        Details = messageFormatter.FormatSystemBackfill()
                    });
                }

                // Ensure UserJoinedRole event exists for all users in this role
                var users = await userManager.GetUsersInRoleAsync(role.Name!);
                foreach (var user in users)
                {
                    var userLogExists = await dbContext.Changes.AnyAsync(c =>
                        c.Type == ChangeType.UserJoinedRole &&
                        c.TargetUserId == user.Id &&
                        c.TargetRoleId == role.Id);

                    if (!userLogExists)
                    {
                        dbContext.Changes.Add(new Change
                        {
                            Type = ChangeType.UserJoinedRole,
                            TargetUserId = user.Id,
                            TargetRoleId = role.Id,
                            CreateTime = user.CreationTime,
                            Details = messageFormatter.FormatSystemBackfill()
                        });
                    }
                }
            }
        }
        await dbContext.SaveChangesAsync();
    }

    private static async Task<bool> ShouldSeedAsync(TemplateDbContext dbContext)
    {
        if (EntryExtends.IsInUnitTests())
        {
            // Always seed in unit tests to ensure a clean state.
            // The seeder logic is idempotent and will handle existing data.
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            dbContext.Users.RemoveRange(dbContext.Users);
            dbContext.Roles.RemoveRange(dbContext.Roles);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        var haveUsers = await dbContext.Users.AnyAsync();
        var haveRoles = await dbContext.Roles.AnyAsync();
        return !haveUsers && !haveRoles;
    }

    public static Task<IHost> CopyAvatarFileAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var storageService = services.GetRequiredService<StorageService>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var avatarFilePath = Path.Combine(host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath,
            "wwwroot", "images", "default-avatar.jpg");
        var physicalPath = storageService.GetFilePhysicalPath(User.DefaultAvatarPath);
        if (!File.Exists(avatarFilePath))
        {
            logger.LogWarning("Avatar file does not exist. Skip copying.");
            return Task.FromResult(host);
        }

        if (File.Exists(physicalPath))
        {
            logger.LogInformation("Avatar file already exists. Skip copying.");
            return Task.FromResult(host);
        }

        if (!Directory.Exists(Path.GetDirectoryName(physicalPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
        }

        File.Copy(avatarFilePath, physicalPath);
        logger.LogInformation("Avatar file copied to {Path}", physicalPath);
        return Task.FromResult(host);
    }

    public static async Task<IHost> SeedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<TemplateDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        var messageFormatter = services.GetRequiredService<ChangeMessageFormatter>();
        
        var settingsService = services.GetRequiredService<GlobalSettingsService>();
        await settingsService.SeedSettingsAsync();

        var shouldSeed = await ShouldSeedAsync(db);
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!shouldSeed)
        {
            logger.LogInformation("Do not need to seed the database. There are already users or roles present.");
            await SyncChangeLogs(db, userManager, roleManager, messageFormatter);
            return host;
        }

        logger.LogInformation("Seeding the database with initial data...");

        var role = await roleManager.FindByNameAsync("Administrators");
        if (role == null)
        {
            role = new IdentityRole("Administrators");
            await roleManager.CreateAsync(role);
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        var existingClaimValues = existingClaims
            .Where(c => c.Type == AppPermissions.Type)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in AppPermissions.GetAllPermissions())
        {
            if (!existingClaimValues.Contains(permission.Key))
            {
                var claim = new Claim(AppPermissions.Type, permission.Key);
                await roleManager.AddClaimAsync(role, claim);
            }
        }

        var studentRole = await roleManager.FindByNameAsync("Students");
        if (studentRole == null)
        {
            studentRole = new IdentityRole("Students");
            await roleManager.CreateAsync(studentRole);
        }

        var studentClaims = await roleManager.GetClaimsAsync(studentRole);
        if (studentClaims.All(c => c.Type != AppPermissions.Type || c.Value != AppPermissionNames.CanTakeExam))
        {
            await roleManager.AddClaimAsync(studentRole, new Claim(AppPermissions.Type, AppPermissionNames.CanTakeExam));
        }

        if (!await db.Users.AnyAsync(u => u.UserName == "admin"))
        {
            var user = new User
            {
                UserName = "admin",
                DisplayName = "Super Administrator",
                Email = "admin@default.com",
            };
            _ = await userManager.CreateAsync(user, "admin123");

            await userManager.AddToRoleAsync(user, "Administrators");
        }
        
        await SyncChangeLogs(db, userManager, roleManager, messageFormatter);

        return host;
    }

    public static async Task<IHost> ImportData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dataImporter = services.GetRequiredService<DataImporter>();
        await dataImporter.StartAsync(CancellationToken.None);
        return host;
    }
}
