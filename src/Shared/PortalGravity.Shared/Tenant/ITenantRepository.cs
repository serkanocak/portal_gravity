using PortalGravity.Shared.Tenant;

namespace PortalGravity.Shared.Tenant;

public interface ITenantRepository
{
    Task<ICurrentTenant?> FindBySlugAsync(string slug, CancellationToken ct = default);
    Task<ICurrentTenant?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
