using AutoPit.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace AutoPit.Worker;
public sealed class ServiceWorker(IMessageBus bus, IAutoStore store, IServiceProcessor processor, ILogger<ServiceWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("AutoPit worker online");
        await foreach (var req in bus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await store.UpsertServiceAsync(req with { Status = ServiceStatus.Diagnosing, FailureReason = null }, stoppingToken);
                await processor.ProcessAsync(req, stoppingToken);
                await store.UpsertServiceAsync(req with { Status = ServiceStatus.Complete, FailureReason = null }, stoppingToken);
            }
            catch (Exception ex)
            {
                await store.UpsertServiceAsync(req with { Status = ServiceStatus.Failed, FailureReason = ex.Message }, stoppingToken);
            }
        }
    }
}
