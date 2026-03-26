using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PortalGravity.Shared.Exceptions;
using PortalGravity.Shared.Tenant;

namespace PortalGravity.Shared.Tenant;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    // Routes that don't require tenant context
    private static readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/health/live", "/health/ready",
        "/api/auth/login", "/api/auth/refresh",
        "/api/i18n"
    };

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        CompositeTenantResolver resolver,
        ITenantRepository tenantRepository,
        TenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (_excludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        try
        {
            var slug = await resolver.ResolveAsync(context);

            if (string.IsNullOrWhiteSpace(slug))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Tenant identifier is required." });
                return;
            }

            var tenant = await tenantRepository.FindBySlugAsync(slug, context.RequestAborted);

            if (tenant is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = $"Tenant '{slug}' not found." });
                return;
            }

            tenantContext.Set(tenant);
            _logger.LogDebug("Tenant resolved: {Slug} ({Id})", tenant.Slug, tenant.Id);

            await _next(context);
        }
        catch (TenantNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        finally
        {
            tenantContext.Clear();
        }
    }
}
