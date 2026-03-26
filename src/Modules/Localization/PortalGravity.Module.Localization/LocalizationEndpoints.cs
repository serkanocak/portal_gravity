using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PortalGravity.Module.Localization;

public static class LocalizationEndpoints
{
    public static async Task<IResult> GetTranslations(
        [FromRoute] string ns,
        [FromRoute] string loc,
        [FromServices] ITranslationService translationService,
        CancellationToken ct)
    {
        var translations = await translationService.GetTranslationsAsync(ns, loc, ct);
        return Results.Ok(translations);
    }

    public record UpsertTranslationReq(string Locale, string Key, string Value);

    public static async Task<IResult> UpsertTranslation(
        [FromRoute] string ns,
        [FromBody] UpsertTranslationReq req,
        [FromServices] ITranslationService translationService,
        CancellationToken ct)
    {
        await translationService.UpsertTranslationAsync(ns, req.Locale, req.Key, req.Value, ct);
        return Results.Ok();
    }
}
