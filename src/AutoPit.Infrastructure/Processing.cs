using AutoPit.Core;
namespace AutoPit.Infrastructure;
public sealed class ServiceProcessor(IAutoStore store) : IServiceProcessor
{
    private static readonly string[] Techs = [ "Alex M", "Priya K", "Jordan S", "Sam R" ];
    private static readonly string[] Findings = [
        "Loose gas cap; cleared code P0457",
        "Brake pad wear at 3mm; recommend replacement",
        "12V battery weak; CCA below spec",
        "Misfire on cylinder 3; coil swapped and verified",
        "Software TSB applied; PCM updated"
    ];
    public async Task ProcessAsync(ServiceRequest req, CancellationToken ct)
    {
        await Task.Delay(Random.Shared.Next(60, 240), ct);
        var tech = Techs[Math.Abs(req.Vin.GetHashCode()) % Techs.Length];
        var finding = Findings[Math.Abs(req.Concern.GetHashCode()) % Findings.Length];
        var estimate = Math.Round((decimal)(95 + (Math.Abs(req.Vin.GetHashCode()) % 600)), 2);
        await store.SaveOrderAsync(new ServiceOrder(req.Id, tech, finding, estimate, DateTimeOffset.UtcNow), ct);
    }
}
