using Microsoft.EntityFrameworkCore;
using PortalGravity.Shared.Database.Entities;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.Database;

namespace PortalGravity.Api.Database;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ITenantRepository tenantRepo)
    {
        if (!await db.Tenants.AnyAsync())
        {
            // 1. Create Main Tenant
            var mainTenant = new TenantEntity
            {
                Id = Guid.NewGuid(),
                Name = "Portal Gravity HQ",
                Slug = "main",
                IsMain = true,
                IsActive = true,
                SchemaName = "tenant_main"
            };
            db.Tenants.Add(mainTenant);

            // 2. Create Admin User
            var admin = new UserEntity
            {
                Id = Guid.NewGuid(),
                TenantId = mainTenant.Id,
                Email = "admin@portal-gravity.com",
                PasswordHash = "Admin123!", // NOT: Gerçek projede hashlenmeli
                IsActive = true
            };
            db.Users.Add(admin);

            await db.SaveChangesAsync();

            // 3. Provision the Tenant Schema
            await db.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {mainTenant.SchemaName};");
        }

        // 4. Seed i18n translations (always check independently)
        await SeedTranslationsAsync(db);
    }

    private static async Task SeedTranslationsAsync(AppDbContext db)
    {
        if (await db.Set<TranslationNamespaceEntity>().AnyAsync()) return;

        var authNs = new TranslationNamespaceEntity { Name = "auth" };
        db.Set<TranslationNamespaceEntity>().Add(authNs);
        await db.SaveChangesAsync();

        var translations = new List<TranslationEntity>
        {
            // English
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.title", Value = "Welcome Back" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.subtitle", Value = "Sign in to your workspace" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.tenantId", Value = "Workspace URL" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.email", Value = "Email Address" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.password", Value = "Password" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.forgot_password", Value = "Forgot password?" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.submit", Value = "Sign In" },
            new() { NamespaceId = authNs.Id, Locale = "en", Key = "login.error", Value = "Login failed" },
            // Turkish
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.title", Value = "Tekrar Hoşgeldiniz" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.subtitle", Value = "Çalışma alanınıza giriş yapın" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.tenantId", Value = "Çalışma Alanı URL" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.email", Value = "E-posta Adresi" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.password", Value = "Şifre" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.forgot_password", Value = "Şifrenizi mi unuttunuz?" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.submit", Value = "Giriş Yap" },
            new() { NamespaceId = authNs.Id, Locale = "tr", Key = "login.error", Value = "Giriş başarısız" },
        };

        db.Set<TranslationEntity>().AddRange(translations);
        await db.SaveChangesAsync();
    }
}
