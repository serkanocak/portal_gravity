namespace PortalGravity.Module.Audit;

public interface IAuditConfigService
{
    Task<bool> IsEnabledAsync(Guid tenantId, string resourceName, CancellationToken ct = default);
}

public class AuditConfigService : IAuditConfigService
{
    // Şimdilik her tenant ve her method için denetim günlüğü yazma özelliği var (true).
    // Gelişmiş aşamalarda tenant settings'ten veya redis key'den bu yapılandırılabilir.
    public Task<bool> IsEnabledAsync(Guid tenantId, string resourceName, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
}
