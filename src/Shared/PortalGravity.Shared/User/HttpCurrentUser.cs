using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Linq;
using PortalGravity.Shared.User;

namespace PortalGravity.Shared.User;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context.");

    public Guid Id => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public string Email => User.FindFirstValue(ClaimTypes.Email)!;
    public Guid TenantId => Guid.Parse(User.FindFirstValue("tenantId")!);
    public Guid? CompanyId => TryParseGuid(User.FindFirstValue("companyId"));
    public Guid? DepartmentId => TryParseGuid(User.FindFirstValue("departmentId"));
    public IReadOnlyList<string> Roles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    public bool IsImpersonated => User.HasClaim(c => c.Type == "impersonatedBy");
    public Guid? ImpersonatedBy => TryParseGuid(User.FindFirstValue("impersonatedBy"));

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var g) ? g : null;
}
