namespace PortalGravity.Shared.Database.Entities;

public class AuditLogEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string? Resource { get; set; }
    public string Result { get; set; } = default!;
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AuditEntry
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string? Resource { get; set; }
    public string Result { get; set; } = default!;
    public string? Metadata { get; set; }
}
