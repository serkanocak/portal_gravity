namespace PortalGravity.Shared.Database.Entities;

public class HelpGuideEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = default!; // Örn: "supplier-create-guide"
    public string Title { get; set; } = default!;
    public string ContentHtml { get; set; } = default!; // Tiptap WYSIWYG çıktısı
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
