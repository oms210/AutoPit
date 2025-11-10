using AutoPit.Core;
using Microsoft.Extensions.DependencyInjection;
namespace AutoPit.Infrastructure;
public static class Bootstrap
{
    public static IServiceCollection AddAutoPit(this IServiceCollection services, string sqliteConnStr, ProcessingOptions? opt = null)
    {
        var options = opt ?? new ProcessingOptions();
        services.AddSingleton(options);
        services.AddSingleton<IAutoStore>(_ => new SqliteStore(sqliteConnStr));
        services.AddSingleton<IMessageBus, ChannelBus>();
        services.AddSingleton<IServiceProcessor, ServiceProcessor>();
        return services;
    }
}
