namespace PortalGravity.Shared.Database.Entities;

public class TenantEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string SchemaName { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public bool IsMain { get; set; }
    public string Settings { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = default!;
    public string? PasswordHash { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
