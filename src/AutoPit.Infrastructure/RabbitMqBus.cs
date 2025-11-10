using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AutoPit.Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AutoPit.Infrastructure;

public sealed class RabbitMqBus : IMessageBus, IAsyncDisposable
{
    private readonly Channel<ServiceRequest> _channel;
    private readonly IConnection _conn;
    private readonly IModel _pub;
    private readonly IModel _sub;
    private readonly string _queueName;
    private readonly string _exchangeName = "autopit.exchange";

    public ChannelReader<ServiceRequest> Reader => _channel.Reader;

    public RabbitMqBus(
        string hostName = "localhost",
        int port = 5672,
        string user = "guest",
        string pass = "guest",
        string queueName = "autopit.service")
    {
        _queueName = queueName;

        // Reader channel for the worker to consume
        _channel = Channel.CreateBounded<ServiceRequest>(
            new BoundedChannelOptions(2048) { FullMode = BoundedChannelFullMode.Wait });

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = user,
            Password = pass,
            // 6.x still supports this flag and AsyncEventingBasicConsumer
            DispatchConsumersAsync = true,

            // These help when RabbitMQ restarts
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true
        };

        _conn = factory.CreateConnection();
        _pub = _conn.CreateModel();
        _sub = _conn.CreateModel();

        // Simple direct exchange + single queue
        _pub.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: true);
        _pub.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _pub.QueueBind(_queueName, _exchangeName, routingKey: _queueName);

        // Set a reasonable prefetch
        _sub.BasicQos(prefetchSize: 0, prefetchCount: 16, global: false);

        var consumer = new AsyncEventingBasicConsumer(_sub);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var req = JsonSerializer.Deserialize<ServiceRequest>(json);
                if (req is not null)
                {
                    await _channel.Writer.WriteAsync(req);
                }
                _sub.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                // Requeue so another consumer can try later
                _sub.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _sub.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
    }

    public ValueTask<bool> PublishAsync(ServiceRequest req, CancellationToken ct)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req));
        var props = _pub.CreateBasicProperties();
        props.DeliveryMode = 2; // persistent

        _pub.BasicPublish(
            exchange: _exchangeName,
            routingKey: _queueName,
            basicProperties: props,
            body: body);

        return ValueTask.FromResult(true);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        try { _pub?.Close(); } catch { }
        try { _sub?.Close(); } catch { }
        try { _conn?.Close(); } catch { }
        _pub?.Dispose();
        _sub?.Dispose();
        _conn?.Dispose();
        _channel.Writer.TryComplete();
    }
}
