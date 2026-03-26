using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;

namespace PortalGravity.Module.Audit;

public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct = default);
}

public class PostgresAuditWriter : IAuditWriter
{
    private readonly AppDbContext _dbContext;

    public PostgresAuditWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var logEntity = new AuditLogEntity
        {
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            Action = entry.Action,
            Resource = entry.Resource,
            Result = entry.Result,
            Metadata = entry.Metadata
        };

        _dbContext.Set<AuditLogEntity>().Add(logEntity);
        await _dbContext.SaveChangesAsync(ct);
    }
}
