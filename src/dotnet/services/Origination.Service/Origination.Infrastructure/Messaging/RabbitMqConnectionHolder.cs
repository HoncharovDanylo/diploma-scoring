using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Origination.Infrastructure.Messaging;

public sealed class RabbitMqConnectionHolder : IDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnectionHolder(IOptions<RabbitMqOptions> options)
    {
        var o = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
    }

    public IConnection Connection => _connection;

    public void Dispose() => _connection.Dispose();
}
