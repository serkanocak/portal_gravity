using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGravity.Shared.Modules;

namespace PortalGravity.Module.Localization;

public sealed class LocalizationModule : IModule
{
    public string Name => "Localization";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITranslationService, RedisBackedTranslationService>();
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/i18n").WithTags("Localization");
        group.MapGet("/{ns}/{loc}", LocalizationEndpoints.GetTranslations);
        group.MapPost("/{ns}", LocalizationEndpoints.UpsertTranslation).RequireAuthorization();
    }
}
