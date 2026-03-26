using FluentMigrator;

namespace PortalGravity.Shared.Database.Migrations;

/// <summary>
/// Creates tenant-specific schema tables using a stored procedure approach.
/// Call ProvisionTenantSchema('tenant_acme') after creating a new tenant.
/// </summary>
[Migration(002, "Create tenant schema provisioning function")]
public sealed class M002_TenantSchemaProvisioner : Migration
{
    public override void Up()
    {
        // PostgreSQL function to provision a new tenant schema
        Execute.Sql(@"
            CREATE OR REPLACE FUNCTION provision_tenant_schema(schema_name TEXT)
            RETURNS VOID AS $$
            BEGIN
                -- Create schema if not exists
                EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);

                -- roles
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.roles (
                        id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id   UUID NOT NULL,
                        name        VARCHAR(100) NOT NULL,
                        description TEXT,
                        UNIQUE (name)
                    )', schema_name);

                -- companies
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.companies (
                        id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id   UUID NOT NULL,
                        name        VARCHAR(200) NOT NULL,
                        tax_number  VARCHAR(50),
                        is_active   BOOLEAN DEFAULT TRUE,
                        created_at  TIMESTAMPTZ DEFAULT NOW()
                    )', schema_name);

                -- departments
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.departments (
                        id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id   UUID NOT NULL,
                        company_id  UUID NOT NULL,
                        name        VARCHAR(200) NOT NULL,
                        parent_id   UUID,
                        created_at  TIMESTAMPTZ DEFAULT NOW()
                    )', schema_name);

                -- permission_assignments
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.permission_assignments (
                        id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id       UUID NOT NULL,
                        resource        VARCHAR(200) NOT NULL,
                        assignee_type   VARCHAR(20) NOT NULL CHECK (assignee_type IN (''user'',''department'',''company'',''role'')),
                        assignee_id     UUID NOT NULL,
                        can_read        BOOLEAN DEFAULT FALSE,
                        can_write       BOOLEAN DEFAULT FALSE,
                        can_delete      BOOLEAN DEFAULT FALSE,
                        can_manage      BOOLEAN DEFAULT FALSE,
                        created_at      TIMESTAMPTZ DEFAULT NOW()
                    )', schema_name);

                -- outbox_messages
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.outbox_messages (
                        id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        event_type   VARCHAR(500) NOT NULL,
                        payload      JSONB NOT NULL,
                        created_at   TIMESTAMPTZ DEFAULT NOW(),
                        processed_at TIMESTAMPTZ,
                        error        TEXT,
                        retry_count  INTEGER DEFAULT 0
                    )', schema_name);

                -- audit_logs (partitioned by month)
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.audit_logs (
                        id           UUID DEFAULT gen_random_uuid(),
                        tenant_id    UUID NOT NULL,
                        user_id      UUID,
                        action       VARCHAR(500) NOT NULL,
                        resource     VARCHAR(200),
                        result       VARCHAR(20) NOT NULL,
                        metadata     JSONB,
                        created_at   TIMESTAMPTZ DEFAULT NOW()
                    ) PARTITION BY RANGE (created_at)', schema_name);

                -- Create initial partition for current month
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.audit_logs_%s PARTITION OF %I.audit_logs
                    FOR VALUES FROM (date_trunc(''month'', NOW())) TO (date_trunc(''month'', NOW()) + INTERVAL ''1 month'')',
                    schema_name,
                    to_char(NOW(), 'YYYY_MM'),
                    schema_name);

                -- help_guides
                EXECUTE format('
                    CREATE TABLE IF NOT EXISTS %I.help_guides (
                        id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id    UUID NOT NULL,
                        slug         VARCHAR(255) NOT NULL UNIQUE,
                        title        VARCHAR(500) NOT NULL,
                        content_html TEXT NOT NULL,
                        updated_at   TIMESTAMPTZ DEFAULT NOW()
                    )', schema_name);

                RAISE NOTICE 'Tenant schema provisioned: %', schema_name;
            END;
            $$ LANGUAGE plpgsql;
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP FUNCTION IF EXISTS provision_tenant_schema(TEXT);");
    }
}
