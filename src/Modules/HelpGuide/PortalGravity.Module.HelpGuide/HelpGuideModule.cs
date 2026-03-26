using PortalGravity.Shared.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PortalGravity.Module.HelpGuide;

public class HelpGuideModule : IModule
{
    public string Name => "HelpGuide";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Gerekli bağımlılık yok, API üzerinden DbContext çekiliyor
    }

    public void RegisterEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/helpguide").WithTags("HelpGuides").RequireAuthorization();
        
        // Okuma uç noktaları (yetki her zaman açık)
        group.MapGet("/", HelpGuideEndpoints.GetAllGuides);
        group.MapGet("/{slug}", HelpGuideEndpoints.GetGuide);

        // Düzenleme uç noktaları
        group.MapPost("/", HelpGuideEndpoints.UpsertGuide);
        group.MapDelete("/{id:guid}", HelpGuideEndpoints.DeleteGuide);
    }
}
