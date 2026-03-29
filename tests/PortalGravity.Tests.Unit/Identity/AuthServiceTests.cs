using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortalGravity.Module.Identity;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.User;
using Xunit;

namespace PortalGravity.Tests.Unit.Identity;

public class AuthServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<ITenantRepository> _tenantRepoMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly JwtTokenService _jwtService;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new AppDbContext(options);
        _tenantRepoMock = new Mock<ITenantRepository>();
        _currentUserMock = new Mock<ICurrentUser>();
        _configMock = new Mock<IConfiguration>();

        // Mock JWT config
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Key"]).Returns("THIS_IS_A_VERY_LONG_SECRET_KEY_FOR_TESTING_1234567890");
        jwtSection.Setup(s => s["Issuer"]).Returns("test-issuer");
        jwtSection.Setup(s => s["Audience"]).Returns("test-audience");
        jwtSection.Setup(s => s["AccessTokenExpiryMinutes"]).Returns("60");
        
        _configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);

        _jwtService = new JwtTokenService(_configMock.Object);
        _sut = new AuthService(_db, _jwtService, _tenantRepoMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantSlug = "test-tenant";
        var email = "user@test.com";
        var password = "Password123!";

        _tenantRepoMock.Setup(r => r.FindBySlugAsync(tenantSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrentTenant { Id = tenantId, Slug = tenantSlug, IsMain = false });

        _db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = password,
            IsActive = true
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.LoginAsync(tenantSlug, email, password, CancellationToken.None);

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordIsInvalid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenantSlug = "test-tenant";
        var email = "user@test.com";

        _tenantRepoMock.Setup(r => r.FindBySlugAsync(tenantSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrentTenant { Id = tenantId, Slug = tenantSlug, IsMain = false });

        _db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            PasswordHash = "CorrectPassword",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _sut.LoginAsync(tenantSlug, email, "WrongPassword", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
