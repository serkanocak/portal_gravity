using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;

namespace PortalGravity.Module.RBAC;

public static class RBACEndpoints
{
    public static async Task<IResult> ListRoles([FromServices] AppDbContext db, CancellationToken ct)
    {
        var roles = await db.Set<RoleEntity>().ToListAsync(ct);
        return Results.Ok(roles);
    }

    public record CreateRoleReq(string Name, string Description);

    public static async Task<IResult> CreateRole([FromBody] CreateRoleReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var exists = await db.Set<RoleEntity>().AnyAsync(r => r.Name == req.Name, ct);
        if (exists) return Results.BadRequest("Rol zaten mevcut.");

        var role = new RoleEntity { Name = req.Name, Description = req.Description };
        db.Set<RoleEntity>().Add(role);
        await db.SaveChangesAsync(ct);
        
        return Results.Created($"/api/rbac/roles/{role.Id}", role);
    }

    public static async Task<IResult> UpdateRole(Guid id, [FromBody] CreateRoleReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var role = await db.Set<RoleEntity>().FindAsync(new object[] { id }, ct);
        if (role == null) return Results.NotFound();

        role.Name = req.Name;
        role.Description = req.Description;
        await db.SaveChangesAsync(ct);

        return Results.Ok(role);
    }

    public static async Task<IResult> DeleteRole(Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var role = await db.Set<RoleEntity>().FindAsync(new object[] { id }, ct);
        if (role == null) return Results.NotFound();

        db.Set<RoleEntity>().Remove(role);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    public record AssignPermReq(string Resource, string AssigneeType, Guid AssigneeId, bool Read, bool Write, bool Delete, bool Manage);

    public static async Task<IResult> AssignPermission([FromBody] AssignPermReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Set<PermissionAssignmentEntity>()
            .FirstOrDefaultAsync(p => p.Resource == req.Resource && p.AssigneeType == req.AssigneeType && p.AssigneeId == req.AssigneeId, ct);

        if (existing != null)
        {
            existing.CanRead = req.Read;
            existing.CanWrite = req.Write;
            existing.CanDelete = req.Delete;
            existing.CanManage = req.Manage;
        }
        else
        {
            var p = new PermissionAssignmentEntity
            {
                Resource = req.Resource,
                AssigneeType = req.AssigneeType,
                AssigneeId = req.AssigneeId,
                CanRead = req.Read,
                CanWrite = req.Write,
                CanDelete = req.Delete,
                CanManage = req.Manage,
                CreatedAt = DateTime.UtcNow
            };
            db.Set<PermissionAssignmentEntity>().Add(p);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    public record RevokePermReq(string Resource, string AssigneeType, Guid AssigneeId);

    public static async Task<IResult> RevokePermission([FromBody] RevokePermReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Set<PermissionAssignmentEntity>()
            .FirstOrDefaultAsync(p => p.Resource == req.Resource && p.AssigneeType == req.AssigneeType && p.AssigneeId == req.AssigneeId, ct);

        if (existing != null)
        {
            db.Set<PermissionAssignmentEntity>().Remove(existing);
            await db.SaveChangesAsync(ct);
        }
        return Results.Ok();
    }
}
