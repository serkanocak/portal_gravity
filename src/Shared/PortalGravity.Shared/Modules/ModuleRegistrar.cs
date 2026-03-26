using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PortalGravity.Shared.Modules;

public static class ModuleRegistrar
{
    private static readonly List<IModule> _registeredModules = new();
    public static IReadOnlyList<IModule> RegisteredModules => _registeredModules.AsReadOnly();

    /// <summary>
    /// Discovers and registers all IModule implementations from the given assemblies.
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        var moduleTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type)!;
            module.RegisterServices(services, configuration);
            _registeredModules.Add(module);
        }

        return services;
    }

    public static IApplicationBuilder UseModules(this IApplicationBuilder app)
    {
        foreach (var module in _registeredModules)
            module.RegisterMiddleware(app);
        return app;
    }

    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder routes)
    {
        foreach (var module in _registeredModules)
            module.RegisterEndpoints(routes);
        return routes;
    }
}
