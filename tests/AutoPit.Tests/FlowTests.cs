using Bogus;
using FluentAssertions;
using AutoPit.Core;
using AutoPit.Infrastructure;
using System.Threading;
using System;
using Xunit;
using System.Threading.Tasks;
namespace AutoPit.Tests;
public class FlowTests
{
    [Fact] public async Task RoundTrip_Works()
    {
        var db = $"Data Source=test-{Guid.NewGuid():N}.db";
        var store = new SqliteStore(db);
        await store.InitAsync(CancellationToken.None);
        var options = new ProcessingOptions(workers:1, channelCapacity:8);
        var bus = new ChannelBus(options);
        var processor = new ServiceProcessor(store);
        var faker = new Faker();
        var car = new Car(faker.Vehicle.Vin(), faker.Vehicle.Manufacturer(), faker.Vehicle.Model(), faker.Random.Int(2010,2025), faker.Vehicle.Type());
        await store.UpsertCarAsync(car, CancellationToken.None);
        var req = new ServiceRequest(Guid.NewGuid(), car.Vin, "Check engine light", 3, DateTimeOffset.UtcNow);
        await store.UpsertServiceAsync(req, CancellationToken.None);
        (await bus.PublishAsync(req, CancellationToken.None)).Should().BeTrue();
        var read = await bus.Reader.ReadAsync(CancellationToken.None);
        await store.UpsertServiceAsync(read with { Status = ServiceStatus.Diagnosing }, CancellationToken.None);
        await processor.ProcessAsync(read, CancellationToken.None);
        await store.UpsertServiceAsync(read with { Status = ServiceStatus.Complete }, CancellationToken.None);
        (await store.GetOrderAsync(req.Id, CancellationToken.None)).Should().NotBeNull();
    }
}
