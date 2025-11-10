using System.Threading.Channels;
namespace AutoPit.Core;

public interface IAutoStore
{
    Task InitAsync(CancellationToken ct);
    Task UpsertCarAsync(Car car, CancellationToken ct);
    Task<Car?> GetCarAsync(string vin, CancellationToken ct);
    Task UpsertServiceAsync(ServiceRequest req, CancellationToken ct);
    Task<ServiceRequest?> GetServiceAsync(Guid id, CancellationToken ct);
    Task SaveOrderAsync(ServiceOrder order, CancellationToken ct);
    Task<ServiceOrder?> GetOrderAsync(Guid requestId, CancellationToken ct);
    IAsyncEnumerable<ServiceRequest> GetQueuedAsync(CancellationToken ct);
}

public interface IMessageBus
{
    ValueTask<bool> PublishAsync(ServiceRequest req, CancellationToken ct);
    ChannelReader<ServiceRequest> Reader { get; }
}

public interface IServiceProcessor
{
    Task ProcessAsync(ServiceRequest req, CancellationToken ct);
}
