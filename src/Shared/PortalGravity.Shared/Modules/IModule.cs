using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PortalGravity.Shared.Modules;

public interface IModule
{
    string Name { get; }
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
    void RegisterMiddleware(IApplicationBuilder app) { }
    void RegisterEndpoints(IEndpointRouteBuilder routes) { }
}
