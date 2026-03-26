using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.Exceptions;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.User;

namespace PortalGravity.Module.Identity;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> LoginAsync(string tenantSlug, string email, string password, CancellationToken ct);
    Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct);
    Task<(string AccessToken, string RefreshToken)> ImpersonateAsync(string targetTenantSlug, string targetUserEmail, CancellationToken ct);
    Task<(string AccessToken, string RefreshToken)> StopImpersonationAsync(CancellationToken ct);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtService;
    private readonly ITenantRepository _tenantRepo;
    private readonly ICurrentUser _currentUser;

    public AuthService(AppDbContext dbContext, IJwtTokenService jwtService, ITenantRepository tenantRepo, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
        _tenantRepo = tenantRepo;
        _currentUser = currentUser;
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(string tenantSlug, string email, string password, CancellationToken ct)
    {
        var tenant = await _tenantRepo.FindBySlugAsync(tenantSlug, ct);
        if (tenant is null) throw new TenantNotFoundException(tenantSlug);

        // Gerçekte şifre hashlenmiş olarak doğrulanmalıdır (ör: BCrypt)
        // Şimdilik test amaçlı düz metin diyebiliriz, ama production projesinde muhakkak hash kullanılır.
        // Bu taslakta düz metin karşılaştırması üzerinden simülasyon yapıyoruz.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email && u.IsActive, ct);

        if (user is null || user.PasswordHash != password) 
            throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");

        // Assign roles based on tenant type - main tenant users get Admin role
        var roles = tenant.IsMain 
            ? new List<string> { "Admin" } 
            : new List<string> { "Standard" };

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, tenant.Id, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await SaveRefreshToken(user.Id, refreshToken, ct);

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken ct)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null) throw new SecurityTokenException("Gecersiz token.");

        var idClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(idClaim, out var userId)) throw new SecurityTokenException("Kullanici id bulunamadi.");

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId && rt.RevokedAt == null, ct);

        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token geçersiz veya süresi dolmuş.");
        }

        // Token rotasyonu: eskisini iptal et ve yenisini oluştur
        storedToken.RevokedAt = DateTime.UtcNow;

        var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)!;
        var tenantId = Guid.Parse(principal.FindFirstValue("tenantId")!);
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        string? impersonatedByClaim = principal.FindFirstValue("impersonatedBy");
        Guid? impersonatorId = null;
        if (!string.IsNullOrEmpty(impersonatedByClaim))
        {
            impersonatorId = Guid.Parse(impersonatedByClaim);
        }

        var newAccessToken = _jwtService.GenerateAccessToken(userId, email, tenantId, roles, impersonatorId);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        await SaveRefreshToken(userId, newRefreshToken, ct);

        return (newAccessToken, newRefreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> ImpersonateAsync(string targetTenantSlug, string targetUserEmail, CancellationToken ct)
    {
        // 1. Sadece ana tenant'tan gelen kullanıcılar bürünebilir
        var currentTenantId = _currentUser.TenantId;
        var currentTenant = await _tenantRepo.FindByIdAsync(currentTenantId, ct);
        if (currentTenant?.IsMain != true)
        {
            throw new UnauthorizedTenantAccessException();
        }

        // 2. Hedef tenant ve kullanıcıyı bul
        var targetTenant = await _tenantRepo.FindBySlugAsync(targetTenantSlug, ct);
        if (targetTenant == null) throw new TenantNotFoundException(targetTenantSlug);

        var targetUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TenantId == targetTenant.Id && u.Email == targetUserEmail && u.IsActive, ct);

        if (targetUser == null) throw new Exception("Hedef kullanici bulunamadi.");

        // Asıl admin in Id'sini impersonatedBy'da saklıyoruz.
        var accessToken = _jwtService.GenerateAccessToken(targetUser.Id, targetUser.Email, targetTenant.Id, new List<string> { "Standard" }, _currentUser.Id);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await SaveRefreshToken(targetUser.Id, refreshToken, ct);

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)> StopImpersonationAsync(CancellationToken ct)
    {
        if (!_currentUser.IsImpersonated || !_currentUser.ImpersonatedBy.HasValue)
        {
            throw new InvalidOperationException("Şu an başka birine bürünmüş durumda değilsiniz.");
        }

        var originalAdminId = _currentUser.ImpersonatedBy.Value;
        var adminUser = await _dbContext.Users.FindAsync(new object[] { originalAdminId }, ct);
        
        if (adminUser == null) throw new Exception("Asıl admin kullanıcısı bulunamadı.");
        
        var accessToken = _jwtService.GenerateAccessToken(adminUser.Id, adminUser.Email, adminUser.TenantId, new List<string> { "Admin" });
        var refreshToken = _jwtService.GenerateRefreshToken();

        await SaveRefreshToken(adminUser.Id, refreshToken, ct);

        return (accessToken, refreshToken);
    }

    private async Task SaveRefreshToken(Guid userId, string token, CancellationToken ct)
    {
        _dbContext.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Bu appsettings'den okunabilir
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(ct);
    }
}
