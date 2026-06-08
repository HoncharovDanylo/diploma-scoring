using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Portfolio.Infrastructure;

public sealed class PortfolioOutboxRelayHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionHolder _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<PortfolioOutboxRelayHostedService> _logger;

    public PortfolioOutboxRelayHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionHolder rabbit,
        IOptions<RabbitMqOptions> options,
        ILogger<PortfolioOutboxRelayHostedService> logger)
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
                var pending = await db.PortfolioIntegrationOutbox
                    .Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OutboxId)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(400, stoppingToken);
                    continue;
                }

                using var channel = _rabbit.Connection.CreateModel();
                channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);
                foreach (var row in pending)
                {
                    var body = Encoding.UTF8.GetBytes(row.EnvelopeJson);
                    channel.BasicPublish(_options.ExchangeName, row.EventType, null, body);
                    row.PublishedAtUtc = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portfolio outbox relay");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
