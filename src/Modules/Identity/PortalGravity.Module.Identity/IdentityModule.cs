using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGravity.Shared.Modules;

namespace PortalGravity.Module.Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");
        group.MapPost("/login", AuthEndpoints.Login);
        group.MapPost("/refresh", AuthEndpoints.Refresh);
        group.MapPost("/impersonate", AuthEndpoints.Impersonate).RequireAuthorization();
        group.MapPost("/stop-impersonate", AuthEndpoints.StopImpersonate).RequireAuthorization();
    }
}
