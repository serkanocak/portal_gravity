namespace PortalGravity.Shared.User;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
    Guid TenantId { get; }
    Guid? CompanyId { get; }
    Guid? DepartmentId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsImpersonated { get; }
    Guid? ImpersonatedBy { get; }
}
