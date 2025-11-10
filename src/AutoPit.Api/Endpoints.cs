using AutoPit.Core;
namespace AutoPit.Api;
public static class Endpoints
{
    public static void MapAutoPit(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api").RequireRateLimiting("burst");
        grp.MapPost("/cars", async (IAutoStore store, CarDto dto, CancellationToken ct) => {
            var car = new Car(dto.Vin.Trim(), dto.Make.Trim(), dto.Model.Trim(), dto.Year, dto.Trim?.Trim());
            var (ok, error) = Validation.Validate(car);
            if (!ok) return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = [error!] });
            await store.UpsertCarAsync(car, ct);
            return Results.Created($"/api/cars/{car.Vin}", car);
        });
        grp.MapGet("/cars/{vin}", async (string vin, IAutoStore store, CancellationToken ct) => {
            var car = await store.GetCarAsync(vin, ct);
            return car is null ? Results.NotFound() : Results.Ok(car);
        });
        grp.MapPost("/service", async (IAutoStore store, IMessageBus bus, ServiceDto dto, CancellationToken ct) => {
            var req = new ServiceRequest(Guid.NewGuid(), dto.Vin.Trim(), dto.Concern.Trim(), dto.Priority, DateTimeOffset.UtcNow);
            var (ok, error) = Validation.Validate(req);
            if (!ok) return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = [error!] });
            await store.UpsertServiceAsync(req, ct);
            var enq = await bus.PublishAsync(req, ct);
            if (!enq) return Results.StatusCode(503);
            return Results.Accepted($"/api/service/{req.Id}", new { requestId = req.Id, status = "Queued" });
        });
        grp.MapGet("/service/{id:guid}", async (Guid id, IAutoStore store, CancellationToken ct) => {
            var req = await store.GetServiceAsync(id, ct);
            if (req is null) return Results.NotFound();
            var order = await store.GetOrderAsync(id, ct);
            return Results.Ok(new { request = req, order, status = order is null ? req.Status.ToString() : "Complete" });
        });
        grp.MapGet("/service/inqueue", async (IAutoStore store, CancellationToken ct) => {
            var list = new List<ServiceRequest>(); await foreach (var r in store.GetQueuedAsync(ct)) list.Add(r); return Results.Ok(list);
        });
    }
    public record CarDto(string Vin, string Make, string Model, int Year, string? Trim);
    public record ServiceDto(string Vin, string Concern, int Priority);
}
