namespace PortalGravity.Shared.Tenant;

public sealed class CurrentTenant : ICurrentTenant
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = default!;
    public bool IsMain { get; init; }
}

public sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<ICurrentTenant?> _current = new();

    public ICurrentTenant? Current => _current.Value;

    public void Set(ICurrentTenant tenant) => _current.Value = tenant;
    public void Clear() => _current.Value = null;
}
