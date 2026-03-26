using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Tenant;

namespace PortalGravity.Shared.Database.Repositories;

public class PostgresTenantRepository : ITenantRepository
{
    private readonly AppDbContext _dbContext;

    public PostgresTenantRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ICurrentTenant?> FindBySlugAsync(string slug, CancellationToken ct = default)
    {
        var entity = await _dbContext.Tenants
            // Tenants tablosu global public şemadadır, interceptor zaten schema eklese de,
            // interceptor çalışmadan önce Tenant middleware'indeyiz, yani interceptor hala search_path=tenant eklemedi!
            // Bu yüzden "public" tablo olduğu için EF üzerinden rahatlıkla sorgulanabilir.
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug, ct);

        if (entity is null || !entity.IsActive)
            return null;

        return new CurrentTenant
        {
            Id = entity.Id,
            Slug = entity.Slug,
            IsMain = entity.IsMain
        };
    }

    public async Task<ICurrentTenant?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (entity is null || !entity.IsActive)
            return null;

        return new CurrentTenant
        {
            Id = entity.Id,
            Slug = entity.Slug,
            IsMain = entity.IsMain
        };
    }
}
