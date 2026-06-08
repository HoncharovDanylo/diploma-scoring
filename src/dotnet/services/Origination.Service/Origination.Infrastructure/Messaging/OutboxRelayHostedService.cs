using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Origination.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Origination.Infrastructure.Messaging;

public sealed class OutboxRelayHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionHolder _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxRelayHostedService> _logger;

    public OutboxRelayHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionHolder rabbit,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxRelayHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RelayBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Origination outbox relay");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task RelayBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OriginationDbContext>();
        var pending = await db.IntegrationOutbox
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.OutboxId)
            .Take(100)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            await Task.Delay(400, ct);
            return;
        }

        using var channel = _rabbit.Connection.CreateModel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);

        foreach (var row in pending)
        {
            var body = Encoding.UTF8.GetBytes(row.EnvelopeJson);
            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: row.EventType,
                basicProperties: null,
                body: body);
            row.PublishedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
