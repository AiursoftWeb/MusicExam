
using Aiursoft.Scanner.Abstractions;
using Microsoft.Extensions.Localization;

namespace Aiursoft.MusicExam.Services;

public class ChangeMessageFormatter(IStringLocalizer<ChangeMessageFormatter> localizer) : IScopedDependency
{
    public string FormatUserCreated(string userName)
    {
        return string.Format(localizer["UserCreated"], userName);
    }

    public string FormatUserDeleted(string userName)
    {
        return string.Format(localizer["UserDeleted"], userName);
    }

    public string FormatUserJoinedRole(string userName, string roleName)
    {
        return string.Format(localizer["UserJoinedRole"], userName, roleName);
    }

    public string FormatUserLeftRole(string userName, string roleName)
    {
        return string.Format(localizer["UserLeftRole"], userName, roleName);
    }

    public string FormatRoleGainedPermission(string roleName, string permissionName)
    {
        return string.Format(localizer["RoleGainedPermission"], roleName, permissionName);
    }

    public string FormatRoleLostPermission(string roleName, string permissionName)
    {
        return string.Format(localizer["RoleLostPermission"], roleName, permissionName);
    }

    public string FormatSystemBackfill()
    {
        return localizer["SystemBackfill"];
    }
}
