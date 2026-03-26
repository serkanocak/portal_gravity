using System.Text.Json;
using MediatR;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.User;
using PortalGravity.Shared.Database.Entities;

namespace PortalGravity.Module.Audit;

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IAuditWriter _auditWriter;
    private readonly IAuditConfigService _auditConfig;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public AuditBehavior(IAuditWriter auditWriter, IAuditConfigService auditConfig, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _auditWriter = auditWriter;
        _auditConfig = auditConfig;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var methodName = typeof(TRequest).Name;

        // Query'leri es geçiyoruz, Command'lar genelde yan etkili işlemlerdir
        var isCommand = methodName.EndsWith("Command");

        var tenantId = _tenantContext.Current?.Id ?? Guid.Empty;
        var shouldLog = tenantId != Guid.Empty && isCommand && await _auditConfig.IsEnabledAsync(tenantId, methodName, ct);

        try
        {
            var response = await next();

            if (shouldLog)
            {
                await _auditWriter.WriteAsync(new AuditEntry
                {
                    TenantId = tenantId,
                    UserId = _currentUser.Id != Guid.Empty ? _currentUser.Id : null,
                    Action = methodName,
                    Resource = typeof(TRequest).Namespace, // Veya request içindeki değer
                    Result = "success",
                    Metadata = JsonSerializer.Serialize(request)
                }, ct);
            }

            return response;
        }
        catch (Exception)
        {
            if (shouldLog)
            {
                await _auditWriter.WriteAsync(new AuditEntry
                {
                    TenantId = tenantId,
                    UserId = _currentUser.Id != Guid.Empty ? _currentUser.Id : null,
                    Action = methodName,
                    Resource = typeof(TRequest).Namespace,
                    Result = "error",
                    Metadata = JsonSerializer.Serialize(request)
                }, ct);
            }

            throw;
        }
    }
}
