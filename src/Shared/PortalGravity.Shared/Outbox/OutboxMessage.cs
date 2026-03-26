namespace PortalGravity.Shared.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string EventType { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
