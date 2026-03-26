using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGravity.Shared.Modules;

namespace PortalGravity.Module.Audit;

public sealed class AuditModule : IModule
{
    public string Name => "Audit";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditWriter, PostgresAuditWriter>();
        services.AddScoped<IAuditConfigService, AuditConfigService>();
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/audit").WithTags("Audit").RequireAuthorization();
        group.MapGet("/logs", AuditEndpoints.GetLogs);
    }
}
