namespace PortalGravity.Shared.Exceptions;

public sealed class TenantNotFoundException : Exception
{
    public TenantNotFoundException(string identifier)
        : base($"Tenant '{identifier}' was not found or is inactive.") { }
}

public sealed class UnauthorizedTenantAccessException : Exception
{
    public UnauthorizedTenantAccessException()
        : base("Access to this tenant is not authorized.") { }
}

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string resource, string action)
        : base($"You do not have permission to '{action}' on '{resource}'.") { }
}
