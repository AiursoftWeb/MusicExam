using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.InMemory;
using Aiursoft.MusicExam.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.MusicExam.Tests.Services;

[TestClass]
public class ChangeServiceTests
{
    private InMemoryContext _dbContext = null!;
    private ChangeService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        var options = new DbContextOptionsBuilder<InMemoryContext>()
            .UseInMemoryDatabase(databaseName: "ChangeServiceTests_" + Guid.NewGuid())
            .Options;

        _dbContext = new InMemoryContext(options);
        _service = new ChangeService(_dbContext);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task TestGetReportForMonth_UserGainedPermission()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var userId = "user-1";
        var roleId = "role-1";
        var permission = AppPermissionNames.CanTakeExam;

        // 1. User Created
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.UserCreated,
            TargetUserId = userId,
            CreateTime = baseTime.AddHours(-1)
        });

        // 2. Role Created (implied, or explicitly logged if needed by logic, but logic relies on RoleGainedPermission)
        
        // 3. Role Gained Permission
        // This makes anyone in this role have the permission
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.RoleGainedPermission,
            TargetRoleId = roleId,
            TargetPermission = permission,
            CreateTime = baseTime
        });

        // 4. User Joined Role
        // This makes the user active
        var fileTime = baseTime.AddDays(5);
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.UserJoinedRole,
            TargetUserId = userId,
            TargetRoleId = roleId,
            CreateTime = fileTime
        });
        
        // 5. User Left Role
        var endTime = baseTime.AddDays(10);
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.UserLeftRole,
            TargetUserId = userId,
            TargetRoleId = roleId,
            CreateTime = endTime
        });

        await _dbContext.SaveChangesAsync();

        // Act
        // Report for the whole month of Jan 2023
        var reportStart = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reportEnd = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1);

        var report = await _service.GetReportForMonth(reportStart, reportEnd);

        // Assert
        Assert.AreEqual(1, report.ActiveUsers.Count, "Should have 1 active user");
        var activeUser = report.ActiveUsers.First();
        Assert.AreEqual(userId, activeUser.User.Id);
        
        Assert.AreEqual(1, activeUser.ActiveTimes.Count);
        var span = activeUser.ActiveTimes.First();
        
        // Allowed tolerance for DB tick precision if necessary, but InMemory is usually exact.
        Assert.AreEqual(fileTime, span.Start);
        Assert.AreEqual(endTime, span.End);
    }
    
    [TestMethod]
    public async Task TestGetReportForMonth_RoleAlreadyHasPermission()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var userId = "user-2";
        var roleId = "role-2";
        var permission = AppPermissionNames.CanTakeExam;

        // Role Gained Permission BEFORE the month
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.RoleGainedPermission,
            TargetRoleId = roleId,
            TargetPermission = permission,
            CreateTime = baseTime.AddMonths(-1)
        });

        // User Joined Role INSIDE the month
        var joinTime = baseTime.AddDays(2);
        _dbContext.Changes.Add(new Change
        {
            Type = ChangeType.UserJoinedRole,
            TargetUserId = userId,
            TargetRoleId = roleId,
            CreateTime = joinTime
        });

        await _dbContext.SaveChangesAsync();

        // Act
        var reportStart = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reportEnd = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1);

        var report = await _service.GetReportForMonth(reportStart, reportEnd);

        // Assert
        Assert.AreEqual(1, report.ActiveUsers.Count);
        var activeUser = report.ActiveUsers.First();
        Assert.AreEqual(userId, activeUser.User.Id);
        
        // Should start from joinTime and go to end of month
        var span = activeUser.ActiveTimes.First();
        Assert.AreEqual(joinTime, span.Start);
        Assert.AreEqual(reportEnd, span.End);
    }
}
