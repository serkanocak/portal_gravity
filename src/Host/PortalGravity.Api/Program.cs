using System.Text;
using FluentMigrator.Runner;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortalGravity.Shared.Database;
using PortalGravity.Shared.Modules;
using PortalGravity.Shared.Outbox;
using PortalGravity.Shared.Tenant;
using PortalGravity.Shared.User;
using PortalGravity.Shared.Database.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ─────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ── Config ───────────────────────────────────────────────────
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection")!;
var jwtSettings = configuration.GetSection("Jwt");

// ── Database ─────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    opts.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(sp.GetRequiredService<TenantSchemaInterceptor>());
});

// ── FluentMigrator ───────────────────────────────────────────
builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(PortalGravity.Shared.Database.Migrations.M001_CreatePublicSchema).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// ── Authentication (JWT) ─────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ── Tenant Context ───────────────────────────────────────────
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ICurrentTenant>(sp =>
    sp.GetRequiredService<ITenantContext>().Current
    ?? throw new InvalidOperationException("No tenant context set."));

// ── Tenant Resolvers ─────────────────────────────────────────
builder.Services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
builder.Services.AddScoped<ITenantResolver, HeaderTenantResolver>();
builder.Services.AddScoped<ITenantResolver, JwtClaimTenantResolver>();
builder.Services.AddScoped<CompositeTenantResolver>();
builder.Services.AddScoped<ITenantRepository, PostgresTenantRepository>();

// ── Current User ─────────────────────────────────────────────
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// ── Interceptors ─────────────────────────────────────────────
builder.Services.AddScoped<TenantSchemaInterceptor>();

// ── MediatR ──────────────────────────────────────────────────
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
    cfg.AddOpenBehavior(typeof(PortalGravity.Module.Audit.AuditBehavior<,>));
});

// ── Outbox ───────────────────────────────────────────────────
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
builder.Services.AddScoped<IOutboxRepository, PostgresOutboxRepository>();
builder.Services.AddHostedService<OutboxProcessorService>();

// ── Redis ────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = configuration.GetConnectionString("Redis"));

// ── Modules ──────────────────────────────────────────────────
builder.Services.AddModules(configuration,
    typeof(PortalGravity.Module.Identity.IdentityModule).Assembly,
    typeof(PortalGravity.Module.RBAC.RBACModule).Assembly,
    typeof(PortalGravity.Module.Organization.OrganizationModule).Assembly,
    typeof(PortalGravity.Module.Localization.LocalizationModule).Assembly,
    typeof(PortalGravity.Module.Audit.AuditModule).Assembly,
    typeof(PortalGravity.Module.HelpGuide.HelpGuideModule).Assembly);

// ── Health Checks ─────────────────────────────────────────────
// builder.Services.AddHealthChecks()
//     .AddNpgSql(connectionString, name: "postgresql")
//     .AddRedis(configuration.GetConnectionString("Redis")!, name: "redis");
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        b => b.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Run Migrations ───────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
    await PortalGravity.Api.Database.DbSeeder.SeedAsync(db, tenantRepo);
}

// ── Middleware Pipeline ──────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseExceptionHandler("/error");
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Tenant middleware — after auth so JWT claims are available
app.UseMiddleware<TenantMiddleware>();

app.UseModules();

// ── Endpoints ─────────────────────────────────────────────────
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapModuleEndpoints();

app.Run();
