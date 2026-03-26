namespace PortalGravity.Shared.Database.Entities;

public class TranslationNamespaceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
}

public class TranslationEntity
{
    public Guid Id { get; set; }
    public Guid NamespaceId { get; set; }
    public string Locale { get; set; } = default!; // "tr", "en" vs.
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}
