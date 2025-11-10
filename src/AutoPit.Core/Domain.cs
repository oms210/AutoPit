namespace AutoPit.Core;

public enum ServiceStatus { Queued, Diagnosing, Complete, Failed }

public record Car(string Vin, string Make, string Model, int Year, string? Trim);

public record ServiceRequest(Guid Id, string Vin, string Concern, int Priority, DateTimeOffset CreatedUtc, ServiceStatus Status = ServiceStatus.Queued, string? FailureReason = null);

public record ServiceOrder(Guid RequestId, string Technician, string Findings, decimal EstimatedCost, DateTimeOffset CompletedUtc);

public sealed class ProcessingOptions(int workers = 2, int channelCapacity = 1024)
{
    public int Workers { get; init; } = workers;
    public int ChannelCapacity { get; init; } = channelCapacity;
}
