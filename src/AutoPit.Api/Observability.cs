using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
namespace AutoPit.Api;
public static class Observability
{
    public static void AddApiInfra(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("burst", opt => { opt.PermitLimit = 20; opt.Window = TimeSpan.FromSeconds(2); opt.QueueLimit = 40; opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; }));
    }
    public static void UseApiInfra(this WebApplication app)
    {
        app.UseRateLimiter();
        app.MapHealthChecks("/health");
        app.UseSwagger(); app.UseSwaggerUI();
    }
}
