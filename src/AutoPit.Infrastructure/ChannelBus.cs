using System.Threading.Channels;
using AutoPit.Core;
namespace AutoPit.Infrastructure;
public sealed class ChannelBus(ProcessingOptions options) : IMessageBus
{
    private readonly Channel<ServiceRequest> _channel = Channel.CreateBounded<ServiceRequest>(new BoundedChannelOptions(options.ChannelCapacity){ FullMode = BoundedChannelFullMode.Wait });
    public ChannelReader<ServiceRequest> Reader => _channel.Reader;
    public async ValueTask<bool> PublishAsync(ServiceRequest req, CancellationToken ct)
        => await _channel.Writer.WaitToWriteAsync(ct) && _channel.Writer.TryWrite(req);
}
