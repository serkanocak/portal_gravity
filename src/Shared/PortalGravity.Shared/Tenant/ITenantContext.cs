namespace PortalGravity.Shared.Tenant;

public interface ICurrentTenant
{
    Guid Id { get; }
    string Slug { get; }
    bool IsMain { get; }
    string SchemaName => $"tenant_{Slug}";
}

public interface ITenantContext
{
    ICurrentTenant? Current { get; }
    bool HasTenant => Current is not null;
}
