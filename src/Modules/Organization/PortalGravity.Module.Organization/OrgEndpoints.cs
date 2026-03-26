using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.User;

namespace PortalGravity.Module.Organization;

public static class OrgEndpoints
{
    public static async Task<IResult> ListCompanies([FromServices] AppDbContext db, CancellationToken ct)
    {
        var companies = await db.Set<CompanyEntity>().ToListAsync(ct);
        return Results.Ok(companies);
    }

    public record CreateCompanyReq(string Name, string? TaxNumber);

    public static async Task<IResult> CreateCompany([FromBody] CreateCompanyReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var company = new CompanyEntity
        {
            Name = req.Name,
            TaxNumber = req.TaxNumber
        };
        db.Set<CompanyEntity>().Add(company);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/org/companies/{company.Id}", company);
    }

    public static async Task<IResult> ListDepartments([FromServices] AppDbContext db, [FromQuery] Guid? companyId, CancellationToken ct)
    {
        var query = db.Set<DepartmentEntity>().AsQueryable();
        if (companyId.HasValue)
            query = query.Where(d => d.CompanyId == companyId.Value);

        var orgs = await query.ToListAsync(ct);
        return Results.Ok(orgs);
    }

    public record CreateDeptReq(Guid CompanyId, string Name, Guid? ParentId);

    public static async Task<IResult> CreateDepartment([FromBody] CreateDeptReq req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var department = new DepartmentEntity
        {
            CompanyId = req.CompanyId,
            Name = req.Name,
            ParentId = req.ParentId
        };
        db.Set<DepartmentEntity>().Add(department);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/org/departments/{department.Id}", department);
    }

    public static async Task<IResult> ListUsers([FromServices] AppDbContext db, [FromServices] ICurrentUser currentUser, [FromQuery] Guid? companyId, CancellationToken ct)
    {
        // Users tablosu Tenant public table'ı, currentUser ile scope et
        var query = db.Users.Where(u => u.TenantId == currentUser.TenantId);

        if (companyId.HasValue)
            query = query.Where(u => u.CompanyId == companyId);

        var users = await query.Select(u => new
        {
            u.Id,
            u.Email,
            u.CompanyId,
            u.DepartmentId,
            u.IsActive
        }).ToListAsync(ct);

        return Results.Ok(users);
    }

    public record InviteUserReq(string Email, Guid? CompanyId, Guid? DepartmentId);

    public static async Task<IResult> InviteUser([FromBody] InviteUserReq req, [FromServices] AppDbContext db, [FromServices] ICurrentUser currentUser, CancellationToken ct)
    {
        var existing = await db.Users.AnyAsync(u => u.TenantId == currentUser.TenantId && u.Email == req.Email, ct);
        if (existing) return Results.BadRequest("Kullanıcı zaten bu tenant'ta davetli.");

        var user = new UserEntity
        {
            TenantId = currentUser.TenantId,
            Email = req.Email,
            CompanyId = req.CompanyId,
            DepartmentId = req.DepartmentId,
            // İlk davette geçici şifre ile aktive edilebilir veya davet linki gönderilir (biz test için şimdilik boş bırakıp IsActive=true diyelim)
            IsActive = true,
            PasswordHash = "123456" // Gerçek sistemlerde random geçici şifre ve mail atılır!
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { message = "Kullanıcı başarıyla oluşturuldu.", userId = user.Id });
    }
}
