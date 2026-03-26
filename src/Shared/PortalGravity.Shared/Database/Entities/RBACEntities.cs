namespace PortalGravity.Shared.Database.Entities;

public class RoleEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}

public class PermissionAssignmentEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    /// <summary>
    /// Örn: "suppliers", "users", "invoices"
    /// </summary>
    public string Resource { get; set; } = default!;
    
    /// <summary>
    /// "user", "department", "company", "role"
    /// </summary>
    public string AssigneeType { get; set; } = default!;
    
    /// <summary>
    /// Tipine göre User ID, Dept ID, Co ID veya Role ID
    /// </summary>
    public Guid AssigneeId { get; set; }
    
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public bool CanManage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
