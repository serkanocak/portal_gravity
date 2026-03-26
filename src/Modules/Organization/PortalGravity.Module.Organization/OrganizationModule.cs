using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGravity.Shared.Modules;

namespace PortalGravity.Module.Organization;

public sealed class OrganizationModule : IModule
{
    public string Name => "Organization";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // services registered in Faz 1
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/org").WithTags("Organization").RequireAuthorization();
        group.MapGet("/companies", OrgEndpoints.ListCompanies);
        group.MapPost("/companies", OrgEndpoints.CreateCompany);
        group.MapGet("/departments", OrgEndpoints.ListDepartments);
        group.MapPost("/departments", OrgEndpoints.CreateDepartment);
        group.MapGet("/users", OrgEndpoints.ListUsers);
        group.MapPost("/users/invite", OrgEndpoints.InviteUser);
    }
}
