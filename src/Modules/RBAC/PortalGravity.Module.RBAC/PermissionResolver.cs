using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.User;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;

namespace PortalGravity.Module.RBAC;

public interface IPermissionResolver
{
    Task<bool> HasPermissionAsync(string resource, string action, CancellationToken ct = default);
}

public class PermissionResolver : IPermissionResolver
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public PermissionResolver(AppDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<bool> HasPermissionAsync(string resource, string action, CancellationToken ct = default)
    {
        // action: "read", "write", "delete", "manage"
        
        // 1. Level: Kullanıcı (User) İzini
        var userPerm = await GetPermissionAsync("user", _currentUser.Id, resource, ct);
        if (userPerm != null) return EvaluateAction(userPerm, action);

        // 2. Level: Departman (Department) İzini
        if (_currentUser.DepartmentId.HasValue)
        {
            var deptPerm = await GetPermissionAsync("department", _currentUser.DepartmentId.Value, resource, ct);
            if (deptPerm != null) return EvaluateAction(deptPerm, action);
        }

        // 3. Level: Şirket (Company) İzini
        if (_currentUser.CompanyId.HasValue)
        {
            var compPerm = await GetPermissionAsync("company", _currentUser.CompanyId.Value, resource, ct);
            if (compPerm != null) return EvaluateAction(compPerm, action);
        }

        // 4. Level: Global Roller (Role) İzini (Kullanıcının rollerine göre)
        foreach (var roleName in _currentUser.Roles)
        {
            // Veritabanındaki role'ü isme göre bul
            var role = await _dbContext.Set<RoleEntity>()
                .FirstOrDefaultAsync(r => r.Name == roleName, ct);

            if (role != null)
            {
                var rolePerm = await GetPermissionAsync("role", role.Id, resource, ct);
                if (rolePerm != null) return EvaluateAction(rolePerm, action);
            }
        }

        // 5. Default: Reddedildi (Forbidden)
        return false;
    }

    private Task<PermissionAssignmentEntity?> GetPermissionAsync(string type, Guid id, string resource, CancellationToken ct)
    {
        return _dbContext.Set<PermissionAssignmentEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AssigneeType == type && p.AssigneeId == id && p.Resource == resource, ct);
    }

    private bool EvaluateAction(PermissionAssignmentEntity perm, string action)
    {
        // Manage en üst izindir. Manage olan herkes altındaki işlemleri de yapabilir.
        if (perm.CanManage) return true;

        return action.ToLower() switch
        {
            "read" => perm.CanRead,
            "write" => perm.CanWrite,
            "delete" => perm.CanDelete,
            "manage" => perm.CanManage,
            _ => false
        };
    }
}
