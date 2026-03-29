using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Module.RBAC;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.User;
using Xunit;

namespace PortalGravity.Tests.Unit.RBAC;

public class PermissionResolverTests
{
    private readonly AppDbContext _db;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly PermissionResolver _sut;

    public PermissionResolverTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new AppDbContext(options);
        _currentUserMock = new Mock<ICurrentUser>();
        _sut = new PermissionResolver(_db, _currentUserMock.Object);
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnTrue_WhenUserHasManagePermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resource = "AuditLogs";
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        
        _db.Set<PermissionAssignmentEntity>().Add(new PermissionAssignmentEntity
        {
            AssigneeType = "user",
            AssigneeId = userId,
            Resource = resource,
            CanManage = true
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.HasPermissionAsync(resource, "read");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnFalse_WhenNoPermissionExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock.Setup(u => u.Roles).Returns(new List<string>());

        // Act
        var result = await _sut.HasPermissionAsync("SecretStuff", "read");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldRespectHierarchy_UserOverDepartment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var resource = "Orders";
        
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock.Setup(u => u.DepartmentId).Returns(deptId);

        // Department says NO
        _db.Set<PermissionAssignmentEntity>().Add(new PermissionAssignmentEntity
        {
            AssigneeType = "department",
            AssigneeId = deptId,
            Resource = resource,
            CanRead = false
        });

        // User says YES
        _db.Set<PermissionAssignmentEntity>().Add(new PermissionAssignmentEntity
        {
            AssigneeType = "user",
            AssigneeId = userId,
            Resource = resource,
            CanRead = true
        });
        
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.HasPermissionAsync(resource, "read");

        // Assert
        result.Should().BeTrue();
    }
}
