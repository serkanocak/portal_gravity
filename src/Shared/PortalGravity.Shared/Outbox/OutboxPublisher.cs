using System.Text.Json;
using MediatR;

namespace PortalGravity.Shared.Outbox;

public interface IOutboxPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : INotification;
}

public sealed class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxRepository _repository;

    public OutboxPublisher(IOutboxRepository repository)
        => _repository = repository;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : INotification
    {
        var message = new OutboxMessage
        {
            EventType = typeof(TEvent).FullName!,
            Payload = JsonSerializer.Serialize(@event)
        };

        await _repository.AddAsync(message, ct);
    }
}

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize = 50, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid id, CancellationToken ct = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct = default);
}
