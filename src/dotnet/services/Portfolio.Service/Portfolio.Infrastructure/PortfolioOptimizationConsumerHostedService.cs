using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Portfolio.Infrastructure;

public sealed class PortfolioOptimizationConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionHolder _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<PortfolioOptimizationConsumerHostedService> _logger;
    private const string QueueName = "portfolio.optimization.in";

    public PortfolioOptimizationConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionHolder rabbit,
        IOptions<RabbitMqOptions> options,
        ILogger<PortfolioOptimizationConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => Run(stoppingToken), stoppingToken);

    private void Run(CancellationToken stoppingToken)
    {
        var channel = _rabbit.Connection.CreateModel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(QueueName, _options.ExchangeName, Lending.Contracts.Events.EventTypes.PortfolioOptimizationCompleted);
        channel.QueueBind(QueueName, _options.ExchangeName, Lending.Contracts.Events.EventTypes.PortfolioOptimizationFailed);
        channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<PortfolioOptimizationReplyHandler>();
                await handler.HandleAsync(json, stoppingToken).ConfigureAwait(false);
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Portfolio optimization reply handling");
                channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(QueueName, false, consumer);
        while (!stoppingToken.IsCancellationRequested)
            Thread.Sleep(500);
    }
}
