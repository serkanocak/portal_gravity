using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.User;

namespace PortalGravity.Module.Audit;

public static class AuditEndpoints
{
    public static async Task<IResult> GetLogs(
        [FromServices] AppDbContext db,
        [FromServices] ITenantContext tenantContext,
        [FromQuery] Guid? userId,
        [FromQuery] string? action,
        [FromQuery] string? result,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        if (tenantContext.Current == null) return Results.BadRequest("Tenant context not found.");
        
        var query = db.Set<AuditLogEntity>().AsNoTracking()
            .Where(a => a.TenantId == tenantContext.Current.Id);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action.Contains(action));

        if (!string.IsNullOrEmpty(result))
            query = query.Where(a => a.Result == result);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var total = await query.CountAsync(ct);

        return Results.Ok(new
        {
            Data = items,
            Total = total,
            Skip = skip,
            Take = take
        });
    }
}
