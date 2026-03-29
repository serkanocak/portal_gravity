using Moq;
using FluentAssertions;
using MediatR;
using PortalGravity.Module.Audit;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.User;
using PortalGravity.Shared.Database.Entities;
using Xunit;

namespace PortalGravity.Tests.Unit.Audit;

public class AuditBehaviorTests
{
    private readonly Mock<IAuditWriter> _auditWriterMock;
    private readonly Mock<IAuditConfigService> _auditConfigMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;

    public AuditBehaviorTests()
    {
        _auditWriterMock = new Mock<IAuditWriter>();
        _auditConfigMock = new Mock<IAuditConfigService>();
        _tenantContextMock = new Mock<ITenantContext>();
        _currentUserMock = new Mock<ICurrentUser>();
    }

    public record TestCommand : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldWriteAuditLog_WhenRequestIsCommandAndEnabled()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new TestCommand();
        
        _tenantContextMock.Setup(t => t.Current).Returns(new CurrentTenant { Id = tenantId });
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _auditConfigMock.Setup(c => c.IsEnabledAsync(tenantId, nameof(TestCommand), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new AuditBehavior<TestCommand, string>(
            _auditWriterMock.Object, 
            _auditConfigMock.Object, 
            _tenantContextMock.Object, 
            _currentUserMock.Object);

        // Act
        await sut.Handle(command, () => Task.FromResult("Done"), CancellationToken.None);

        // Assert
        _auditWriterMock.Verify(w => w.WriteAsync(It.Is<AuditEntry>(e => 
            e.TenantId == tenantId && 
            e.UserId == userId && 
            e.Action == nameof(TestCommand) &&
            e.Result == "success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    public record TestQuery : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldNotWriteAuditLog_WhenRequestIsQuery()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new TestQuery();
        
        _tenantContextMock.Setup(t => t.Current).Returns(new CurrentTenant { Id = tenantId });
        _auditConfigMock.Setup(c => c.IsEnabledAsync(tenantId, nameof(TestQuery), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new AuditBehavior<TestQuery, string>(
            _auditWriterMock.Object, 
            _auditConfigMock.Object, 
            _tenantContextMock.Object, 
            _currentUserMock.Object);

        // Act
        await sut.Handle(query, () => Task.FromResult("Done"), CancellationToken.None);

        // Assert
        _auditWriterMock.Verify(w => w.WriteAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
