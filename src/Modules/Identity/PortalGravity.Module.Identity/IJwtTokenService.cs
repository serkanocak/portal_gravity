using System.Security.Claims;

namespace PortalGravity.Module.Identity;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, Guid tenantId, IReadOnlyList<string> roles, Guid? impersonatedBy = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
