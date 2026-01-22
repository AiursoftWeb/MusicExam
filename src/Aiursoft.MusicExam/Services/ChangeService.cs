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
    public string AvatarRelativePath { get; set; } = User.DefaultAvatarPath;
}

public class PermissionTimeSpan
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class ActiveUserDetail
{
    public required ActiveUserInfo User { get; set; }
    public List<PermissionTimeSpan> ActiveTimes { get; set; } = [];
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
            
            // Skip future months
            if (monthStart > now)
            {
                continue;
            }

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
        
        // Track active spans: UserId -> List of spans
        var usersHistory = new Dictionary<string, List<PermissionTimeSpan>>();
        // Track currently active start time: UserId -> StartTime
        var currentActiveStart = new Dictionary<string, DateTime>();

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

        void CheckUserStatus(string userId, DateTime time)
        {
            bool isNowActive = HasTakeExam(userId);
            bool wasActive = currentActiveStart.ContainsKey(userId);

            if (isNowActive && !wasActive)
            {
                // User just became active
                // If this is before the month start, we'll cap it at month start later effectively
                // But for logic, we track the actual time they became active
                currentActiveStart[userId] = time;
            }
            else if (!isNowActive && wasActive)
            {
                // User just became inactive
                var startTime = currentActiveStart[userId];
                currentActiveStart.Remove(userId);

                // Add to history
                if (!usersHistory.ContainsKey(userId)) usersHistory[userId] = [];
                usersHistory[userId].Add(new PermissionTimeSpan
                {
                    Start = startTime,
                    End = time
                });
            }
        }

        // Replay all history
        foreach (var change in allChanges)
        {
            // Apply state change
            ApplyChange(change, userStates, rolePermissions, userRoles, userNames);
            
            // Check status impact
            // Users involved could be TargetUserId, or all users with TargetRoleId
            var usersToCheck = new HashSet<string>();
            if (change.TargetUserId != null) usersToCheck.Add(change.TargetUserId);
            
            if (change.TargetRoleId != null)
            {
                 // If permission changed for a role, check all users in that role
                 // Or if user joined/left role, check that user (already handled by TargetUserId)
                 if (change.Type == ChangeType.RoleGainedPermission || change.Type == ChangeType.RoleLostPermission)
                 {
                     foreach(var kvp in userRoles)
                     {
                         if (kvp.Value.Contains(change.TargetRoleId))
                         {
                             usersToCheck.Add(kvp.Key);
                         }
                     }
                 }
            }

            foreach (var userId in usersToCheck)
            {
                CheckUserStatus(userId, change.CreateTime);
            }
        }

        // Finish up: Close any open spans at 'end'
        foreach (var userId in currentActiveStart.Keys)
        {
            if (!usersHistory.ContainsKey(userId)) usersHistory[userId] = [];
            usersHistory[userId].Add(new PermissionTimeSpan
            {
                Start = currentActiveStart[userId],
                End = end 
            });
        }
        
        // Prepare list of users to fetch
        var userIdsWithActivity = new HashSet<string>();
        foreach (var kvp in usersHistory)
        {
            var userId = kvp.Key;
            var spans = kvp.Value;
            // Check if any span overlaps with the month
            if (spans.Any(s => s.Start < end && s.End > start))
            {
                userIdsWithActivity.Add(userId);
            }
        }

        // Batch fetch real users
        var realUsers = await dbContext.Users
            .Where(u => userIdsWithActivity.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var reportDetails = new List<ActiveUserDetail>();
        
        foreach (var kvp in usersHistory)
        {
            var userId = kvp.Key;
            var spans = kvp.Value;
            var monthlySpans = new List<PermissionTimeSpan>();

            foreach (var span in spans)
            {
                var overlapStart = span.Start > start ? span.Start : start;
                var overlapEnd = span.End < end ? span.End : end;

                if (overlapStart <= overlapEnd) 
                {
                     monthlySpans.Add(new PermissionTimeSpan { Start = overlapStart, End = overlapEnd });
                }
            }

            if (monthlySpans.Count > 0)
            {
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
                        AvatarRelativePath = User.DefaultAvatarPath
                    };
                }

                reportDetails.Add(new ActiveUserDetail
                {
                    User = userInfo,
                    ActiveTimes = monthlySpans
                });
            }
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
