using FluentMigrator;

namespace PortalGravity.Shared.Database.Migrations;

[Migration(003, "Create global outbox_messages table for processor")]
public sealed class M003_CreateGlobalOutbox : Migration
{
    public override void Up()
    {
        if (!Schema.Table("outbox_messages").Exists())
        {
            Create.Table("outbox_messages")
                .WithColumn("id").AsGuid().PrimaryKey().WithDefault(SystemMethods.NewGuid)
                .WithColumn("event_type").AsString(500).NotNullable()
                .WithColumn("payload").AsCustom("jsonb").NotNullable()
                .WithColumn("created_at").AsCustom("timestamptz").WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("processed_at").AsCustom("timestamptz").Nullable()
                .WithColumn("error").AsString(2000).Nullable()
                .WithColumn("retry_count").AsInt32().WithDefaultValue(0);
        }
    }

    public override void Down()
    {
        // Don't drop it just in case.
    }
}
