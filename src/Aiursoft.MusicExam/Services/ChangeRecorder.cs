using Aiursoft.MusicExam.Entities;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.MusicExam.Services;

public class ChangeRecorder(TemplateDbContext dbContext) : IScopedDependency
{
    public async Task Record(
        ChangeType type, 
        string? triggerUserId, 
        string? targetUserId = null, 
        string? targetDisplayName = null,
        string? targetRoleId = null, 
        string? targetPermission = null, 
        string details = "")
    {
        var change = new Change
        {
            Type = type,
            TriggerUserId = triggerUserId,
            TargetUserId = targetUserId,
            TargetDisplayName = targetDisplayName,
            TargetRoleId = targetRoleId,
            TargetPermission = targetPermission,
            Details = details
        };
        dbContext.Changes.Add(change);
        await dbContext.SaveChangesAsync();
    }
}
