using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.Outbox;

namespace PortalGravity.Shared.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -- Global Tables (public schema, ama EF varsayılan olduğu için ayrıca schema belirtmiyoruz)

        modelBuilder.Entity<TenantEntity>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            // user için (tenant_id, email) eşsiz (unique)
            b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        });

        modelBuilder.Entity<RefreshTokenEntity>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(rt => rt.Id);
            b.HasIndex(rt => rt.Token).IsUnique();
        });

        // -- Tenant-specific Tables (Şemasını çalışma zamanında Interceptor atayacak)
        // Ancak tablo isimleri veritabanındaki gibi olmalı
        
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            // Bu tablo, Tenant schema'sında
            b.ToTable("outbox_messages");
            b.HasKey(o => o.Id);
        });

        modelBuilder.Entity<RoleEntity>(b =>
        {
            b.ToTable("roles");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<PermissionAssignmentEntity>(b =>
        {
            b.ToTable("permission_assignments");
            b.HasKey(p => p.Id);
        });

        modelBuilder.Entity<CompanyEntity>(b =>
        {
            b.ToTable("companies");
            b.HasKey(c => c.Id);
        });

        modelBuilder.Entity<DepartmentEntity>(b =>
        {
            b.ToTable("departments");
            b.HasKey(d => d.Id);
        });

        // -- Localization
        modelBuilder.Entity<TranslationNamespaceEntity>(b =>
        {
            b.ToTable("translation_namespaces");
            b.HasKey(n => n.Id);
            b.HasIndex(n => n.Name).IsUnique();
        });

        modelBuilder.Entity<TranslationEntity>(b =>
        {
            b.ToTable("translations");
            b.HasKey(t => t.Id);
            b.HasIndex(t => new { t.NamespaceId, t.Locale, t.Key }).IsUnique();
        });

        // -- Audit
        modelBuilder.Entity<AuditLogEntity>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(a => a.Id);
        });

        // -- Help Guide
        modelBuilder.Entity<HelpGuideEntity>(b =>
        {
            b.ToTable("help_guides");
            b.HasKey(h => h.Id);
            b.HasIndex(h => h.Slug).IsUnique();
        });
    }
}
