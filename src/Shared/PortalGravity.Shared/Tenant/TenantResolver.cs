using Microsoft.AspNetCore.Http;
using System.Linq;
using PortalGravity.Shared.Exceptions;
using PortalGravity.Shared.Tenant;

namespace PortalGravity.Shared.Tenant;

/// <summary>
/// Resolves tenant from: 1) Subdomain, 2) X-Tenant-Id header, 3) JWT claim.
/// </summary>
public interface ITenantResolver
{
    Task<string?> ResolveSlugAsync(HttpContext context);
}

public sealed class SubdomainTenantResolver : ITenantResolver
{
    public Task<string?> ResolveSlugAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        // e.g. acme.app.com => slug = "acme"
        if (parts.Length >= 3 && parts[0] != "www")
            return Task.FromResult<string?>(parts[0]);
        return Task.FromResult<string?>(null);
    }
}

public sealed class HeaderTenantResolver : ITenantResolver
{
    public Task<string?> ResolveSlugAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var value))
            return Task.FromResult<string?>(value.ToString());
        return Task.FromResult<string?>(null);
    }
}

public sealed class JwtClaimTenantResolver : ITenantResolver
{
    public Task<string?> ResolveSlugAsync(HttpContext context)
    {
        var claim = context.User?.FindFirst("tenantId")?.Value;
        return Task.FromResult(claim);
    }
}

public sealed class CompositeTenantResolver
{
    private readonly IEnumerable<ITenantResolver> _resolvers;

    public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
        => _resolvers = resolvers;

    public async Task<string?> ResolveAsync(HttpContext context)
    {
        foreach (var resolver in _resolvers)
        {
            var slug = await resolver.ResolveSlugAsync(context);
            if (!string.IsNullOrWhiteSpace(slug))
                return slug;
        }
        return null;
    }
}
