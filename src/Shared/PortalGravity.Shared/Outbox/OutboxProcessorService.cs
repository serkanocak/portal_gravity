using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PortalGravity.Shared.Outbox;

public sealed class OutboxProcessorService : BackgroundService
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
    private const int MaxRetries = 5;

    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceProvider services,
        ILogger<OutboxProcessorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessor encountered an error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await repository.GetUnprocessedAsync(50, ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType is null)
                {
                    _logger.LogWarning("Unknown event type: {Type}", message.EventType);
                    await repository.MarkFailedAsync(message.Id, "Unknown event type", ct);
                    continue;
                }

                var @event = (INotification)JsonSerializer.Deserialize(message.Payload, eventType)!;
                await publisher.Publish(@event, ct);
                await repository.MarkProcessedAsync(message.Id, ct);

                _logger.LogDebug("Outbox message processed: {Id} ({Type})", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);

                if (message.RetryCount >= MaxRetries)
                    await repository.MarkFailedAsync(message.Id, ex.Message, ct);
                // else: will be retried in next batch
            }
        }
    }
}
