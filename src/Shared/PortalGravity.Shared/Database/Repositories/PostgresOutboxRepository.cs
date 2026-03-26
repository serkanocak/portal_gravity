using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Outbox;

namespace PortalGravity.Shared.Database.Repositories;

public class PostgresOutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public PostgresOutboxRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _dbContext.OutboxMessages.AddAsync(message, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize = 50, CancellationToken ct = default)
    {
        return await _dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.RetryCount < 5)
            // Postgres'deki verilerin Tenant schema'larında olduğunu biliyoruz.
            // Fakat arkaplan işleyicisi çalışırken _tenantContext ayarlanamayacaktır!
            // Bu, çok kiracılı Outbox Pattern'inde önemli bir detay.
            // O yüzden burada uyarı olarak bırakıyorum: OutboxProcessorService, "her tenant" için döngüyle
            // bu repository'i çağırıp, interceptor scope'u açarak çalışmalıdır.
            // Şimdilik Interceptor sayesinde o anki Tenant'a göre sorgulama yapacağız.
            // (Processor service'i tenant-aware yapmak için onu daha da geliştireceğiz.)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { id }, ct);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync(new object[] { id }, ct);
        if (message != null)
        {
            message.Error = error;
            message.RetryCount++;
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
