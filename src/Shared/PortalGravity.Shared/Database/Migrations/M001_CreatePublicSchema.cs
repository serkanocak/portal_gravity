using FluentMigrator;

namespace PortalGravity.Shared.Database.Migrations;

/// <summary>
/// Creates the public (global) schema tables: tenants, users, translations.
/// </summary>
[Migration(001, "Create public schema base tables")]
public sealed class M001_CreatePublicSchema : Migration
{
    public override void Up()
    {
        // Enable UUID generation
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

        // tenants table
        Create.Table("tenants")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("slug").AsString(100).NotNullable().Unique()
            .WithColumn("schema_name").AsString(100).NotNullable()
            .WithColumn("parent_id").AsGuid().Nullable().ForeignKey("tenants", "id")
            .WithColumn("is_main").AsBoolean().WithDefaultValue(false)
            .WithColumn("settings").AsCustom("jsonb").WithDefaultValue("{}")
            .WithColumn("is_active").AsBoolean().WithDefaultValue(true)
            .WithColumn("created_at").AsCustom("timestamptz").WithDefault(SystemMethods.CurrentUTCDateTime);

        // users table (global user registry)
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("tenant_id").AsGuid().NotNullable().ForeignKey("tenants", "id")
            .WithColumn("email").AsString(320).NotNullable()
            .WithColumn("password_hash").AsString(500).Nullable()
            .WithColumn("company_id").AsGuid().Nullable()
            .WithColumn("department_id").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().WithDefaultValue(true)
            .WithColumn("created_at").AsCustom("timestamptz").WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.UniqueConstraint("uq_users_tenant_email")
            .OnTable("users")
            .Columns("tenant_id", "email");

        // refresh_tokens table
        Create.Table("refresh_tokens")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("user_id").AsGuid().NotNullable().ForeignKey("users", "id")
            .WithColumn("token").AsString(500).NotNullable().Unique()
            .WithColumn("expires_at").AsCustom("timestamptz").NotNullable()
            .WithColumn("revoked_at").AsCustom("timestamptz").Nullable()
            .WithColumn("created_at").AsCustom("timestamptz").WithDefault(SystemMethods.CurrentUTCDateTime);

        // translation_namespaces table
        Create.Table("translation_namespaces")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("name").AsString(100).NotNullable().Unique();

        // translations table
        Create.Table("translations")
            .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
            .WithColumn("namespace_id").AsGuid().NotNullable().ForeignKey("translation_namespaces", "id")
            .WithColumn("locale").AsString(10).NotNullable()
            .WithColumn("key").AsString(500).NotNullable()
            .WithColumn("value").AsString(2000).NotNullable();

        Create.UniqueConstraint("uq_translations_ns_locale_key")
            .OnTable("translations")
            .Columns("namespace_id", "locale", "key");
    }

    public override void Down()
    {
        Delete.Table("translations");
        Delete.Table("translation_namespaces");
        Delete.Table("refresh_tokens");
        Delete.Table("users");
        Delete.Table("tenants");
    }
}
