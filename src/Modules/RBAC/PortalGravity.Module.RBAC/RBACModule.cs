using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGravity.Shared.Modules;

namespace PortalGravity.Module.RBAC;

public sealed class RBACModule : IModule
{
    public string Name => "RBAC";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPermissionResolver, PermissionResolver>();
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/rbac").WithTags("RBAC").RequireAuthorization();
        group.MapGet("/roles", RBACEndpoints.ListRoles);
        group.MapPost("/roles", RBACEndpoints.CreateRole);
        group.MapPut("/roles/{id:guid}", RBACEndpoints.UpdateRole);
        group.MapDelete("/roles/{id:guid}", RBACEndpoints.DeleteRole);
        group.MapPost("/permissions/assign", RBACEndpoints.AssignPermission);
        group.MapPost("/permissions/revoke", RBACEndpoints.RevokePermission);
    }
}
