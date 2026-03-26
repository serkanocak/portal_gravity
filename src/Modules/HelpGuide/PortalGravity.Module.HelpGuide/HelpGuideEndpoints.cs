using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.User;

namespace PortalGravity.Module.HelpGuide;

public static class HelpGuideEndpoints
{
    public static async Task<IResult> GetGuide(
        [FromRoute] string slug,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var guide = await db.Set<HelpGuideEntity>().AsNoTracking()
            .FirstOrDefaultAsync(h => h.Slug == slug, ct);

        if (guide == null) return Results.NotFound();

        return Results.Ok(guide);
    }

    public static async Task<IResult> GetAllGuides(
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var guides = await db.Set<HelpGuideEntity>().AsNoTracking()
            .Select(h => new { h.Id, h.Slug, h.Title, h.UpdatedAt })
            .ToListAsync(ct);

        return Results.Ok(guides);
    }

    public record UpsertGuideReq(string Slug, string Title, string ContentHtml);

    public static async Task<IResult> UpsertGuide(
        [FromBody] UpsertGuideReq req,
        [FromServices] AppDbContext db,
        [FromServices] ICurrentUser currentUser,
        CancellationToken ct)
    {
        var guide = await db.Set<HelpGuideEntity>()
            .FirstOrDefaultAsync(h => h.Slug == req.Slug, ct);

        if (guide != null)
        {
            guide.Title = req.Title;
            guide.ContentHtml = req.ContentHtml; // Uyarı: DOMPurify frontend tarafında kullanılacak ancak yine de sanitizasyon faydalı olabilir!
            guide.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            guide = new HelpGuideEntity
            {
                TenantId = currentUser.TenantId,
                Slug = req.Slug,
                Title = req.Title,
                ContentHtml = req.ContentHtml
            };
            db.Set<HelpGuideEntity>().Add(guide);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(guide);
    }

    public static async Task<IResult> DeleteGuide(
        [FromRoute] Guid id,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var guide = await db.Set<HelpGuideEntity>().FindAsync(new object[] { id }, ct);
        if (guide == null) return Results.NotFound();

        db.Set<HelpGuideEntity>().Remove(guide);
        await db.SaveChangesAsync(ct);
        
        return Results.NoContent();
    }
}
