using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Origination.Infrastructure.Messaging;

public sealed class ScoringReplyConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionHolder _rabbit;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ScoringReplyConsumerHostedService> _logger;
    private const string QueueName = "origination.scoring.in";

    public ScoringReplyConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionHolder rabbit,
        IOptions<RabbitMqOptions> options,
        ILogger<ScoringReplyConsumerHostedService> logger)
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
        channel.QueueBind(QueueName, _options.ExchangeName, Lending.Contracts.Events.EventTypes.ScoringCompleted);
        channel.QueueBind(QueueName, _options.ExchangeName, Lending.Contracts.Events.EventTypes.ScoringFailed);
        channel.BasicQos(0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ScoringReplyHandler>();
                await handler.HandleAsync(json, stoppingToken).ConfigureAwait(false);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed handling scoring reply");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(QueueName, autoAck: false, consumer);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
                Thread.Sleep(500);
        }
        finally
        {
            channel.Close();
            channel.Dispose();
        }
    }
}
