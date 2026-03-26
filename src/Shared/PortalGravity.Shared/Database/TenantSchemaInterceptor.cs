using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using PortalGravity.Shared.Tenant;

namespace PortalGravity.Shared.Database;

/// <summary>
/// EF Core interceptor that automatically sets PostgreSQL search_path
/// to the current tenant's schema before each command execution.
/// </summary>
public sealed class TenantSchemaInterceptor : DbCommandInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantSchemaInterceptor> _logger;

    public TenantSchemaInterceptor(
        ITenantContext tenantContext,
        ILogger<TenantSchemaInterceptor> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ApplySearchPath(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken ct = default)
    {
        ApplySearchPath(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySearchPath(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        ApplySearchPath(command);
        return ValueTask.FromResult(result);
    }

    private void ApplySearchPath(DbCommand command)
    {
        if (_tenantContext.Current is null) return;

        var schema = _tenantContext.Current.SchemaName;
        command.CommandText = $"SET search_path TO {schema}, public;\n{command.CommandText}";

        _logger.LogTrace("search_path set to: {Schema}", schema);
    }
}
