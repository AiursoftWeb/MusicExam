using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.MusicExam.Services;

public class ActiveUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarRelativePath { get; set; } = Entities.User.DefaultAvatarPath;
}

public class ActiveUserDetail
{
    public required ActiveUserInfo User { get; set; }
    public required string Reason { get; set; }
}

public class MonthlyActiveUserReport
{
    public DateTime Month { get; set; }
    public List<ActiveUserDetail> ActiveUsers { get; set; } = [];
}

public class ChangeService(TemplateDbContext dbContext) : IScopedDependency
{
    public async Task<List<MonthlyActiveUserReport>> GetMonthlyReports(int months = 12)
    {
        var now = DateTime.UtcNow;
        var reports = new List<MonthlyActiveUserReport>();

        for (var i = 0; i < months; i++)
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
            
            var report = await GetReportForMonth(monthStart, monthEnd);
            reports.Add(report);
        }

        return reports;
    }

    public async Task<MonthlyActiveUserReport> GetReportForMonth(DateTime start, DateTime end)
    {
        var allChanges = await dbContext.Changes
            .Where(c => c.CreateTime <= end)
            .OrderBy(c => c.CreateTime)
            .ToListAsync();

        var userStates = new Dictionary<string, bool>(); // UserId -> IsDeleted
        var rolePermissions = new Dictionary<string, HashSet<string>>(); // RoleId -> Permissions
        var userRoles = new Dictionary<string, HashSet<string>>(); // UserId -> RoleIds
        
        var userNames = new Dictionary<string, string>(); // UserId -> DisplayName
        
        var activeInMonth = new Dictionary<string, string>(); // UserId -> Reason

        bool HasTakeExam(string userId)
        {
            if (userStates.GetValueOrDefault(userId, false)) return false; // Deleted
            if (!userRoles.TryGetValue(userId, out var roles)) return false;
            
            foreach (var roleId in roles)
            {
                if (rolePermissions.TryGetValue(roleId, out var perms) && perms.Contains(AppPermissionNames.CanTakeExam))
                {
                    return true;
                }
            }
            return false;
        }

        var preStartChanges = allChanges.Where(c => c.CreateTime < start).ToList();
        foreach (var change in preStartChanges)
        {
            ApplyChange(change, userStates, rolePermissions, userRoles, userNames);
        }

        // Initial check at start
        foreach (var userId in userStates.Keys)
        {
            if (HasTakeExam(userId))
            {
                activeInMonth[userId] = "Had permission at the start of the month.";
            }
        }

        // Process changes during month
        var duringMonthChanges = allChanges.Where(c => c.CreateTime >= start && c.CreateTime <= end).ToList();
        foreach (var change in duringMonthChanges)
        {
            ApplyChange(change, userStates, rolePermissions, userRoles, userNames);
            
            // Check who became active
            if (change.TargetUserId != null)
            {
                if (!activeInMonth.ContainsKey(change.TargetUserId) && HasTakeExam(change.TargetUserId))
                {
                    activeInMonth[change.TargetUserId] = $"Gained permission during month: {change.Details}";
                }
            }
            else if (change.TargetRoleId != null && change.TargetPermission == AppPermissionNames.CanTakeExam)
            {
                foreach (var userEntry in userRoles)
                {
                    if (userEntry.Value.Contains(change.TargetRoleId))
                    {
                        if (!activeInMonth.ContainsKey(userEntry.Key) && HasTakeExam(userEntry.Key))
                        {
                            activeInMonth[userEntry.Key] = $"Role {change.TargetRoleId} gained permission: {change.Details}";
                        }
                    }
                }
            }
        }

        var realUsers = await dbContext.Users
            .Where(u => activeInMonth.Keys.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var reportDetails = new List<ActiveUserDetail>();
        foreach (var entry in activeInMonth)
        {
            var userId = entry.Key;
            var reason = entry.Value;
            
            ActiveUserInfo userInfo;
            if (realUsers.TryGetValue(userId, out var user))
            {
                userInfo = new ActiveUserInfo
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    DisplayName = user.DisplayName,
                    AvatarRelativePath = user.AvatarRelativePath
                };
            }
            else
            {
                userInfo = new ActiveUserInfo
                {
                    Id = userId,
                    UserName = "deleted",
                    DisplayName = userNames.GetValueOrDefault(userId, "Deleted User"),
                    AvatarRelativePath = Entities.User.DefaultAvatarPath
                };
            }
            
            reportDetails.Add(new ActiveUserDetail
            {
                User = userInfo,
                Reason = reason
            });
        }

        return new MonthlyActiveUserReport
        {
            Month = start,
            ActiveUsers = reportDetails
        };
    }

    private void ApplyChange(Change change, Dictionary<string, bool> userStates, Dictionary<string, HashSet<string>> rolePermissions, Dictionary<string, HashSet<string>> userRoles, Dictionary<string, string> userNames)
    {
        if (change.TargetUserId != null && !string.IsNullOrEmpty(change.TargetDisplayName))
        {
            userNames[change.TargetUserId] = change.TargetDisplayName;
        }

        switch (change.Type)
        {
            case ChangeType.UserCreated:
                if (change.TargetUserId != null) userStates[change.TargetUserId] = false;
                break;
            case ChangeType.UserDeleted:
                if (change.TargetUserId != null) userStates[change.TargetUserId] = true;
                break;
            case ChangeType.UserJoinedRole:
                if (change.TargetUserId != null && change.TargetRoleId != null)
                {
                    if (!userRoles.ContainsKey(change.TargetUserId)) userRoles[change.TargetUserId] = [];
                    userRoles[change.TargetUserId].Add(change.TargetRoleId);
                }
                break;
            case ChangeType.UserLeftRole:
                if (change.TargetUserId != null && change.TargetRoleId != null)
                {
                    if (userRoles.ContainsKey(change.TargetUserId)) userRoles[change.TargetUserId].Remove(change.TargetRoleId);
                }
                break;
            case ChangeType.RoleGainedPermission:
                if (change.TargetRoleId != null && change.TargetPermission != null)
                {
                    if (!rolePermissions.ContainsKey(change.TargetRoleId)) rolePermissions[change.TargetRoleId] = [];
                    rolePermissions[change.TargetRoleId].Add(change.TargetPermission);
                }
                break;
            case ChangeType.RoleLostPermission:
                if (change.TargetRoleId != null && change.TargetPermission != null)
                {
                    if (!rolePermissions.ContainsKey(change.TargetRoleId)) rolePermissions[change.TargetRoleId] = [];
                    rolePermissions[change.TargetRoleId].Remove(change.TargetPermission);
                }
                break;
        }
    }
}
