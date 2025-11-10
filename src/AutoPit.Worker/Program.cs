using AutoPit.Core;
using AutoPit.Infrastructure;
using AutoPit.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
var builder = Host.CreateApplicationBuilder(args);
var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=autopit.db";
var proc = builder.Configuration.GetSection("Processing").Get<ProcessingOptions>() ?? new();
builder.Services.AddAutoPit(cs, proc);
builder.Services.AddHostedService<ServiceWorker>();
var useRabbit = builder.Configuration.GetValue<bool>("USE_RABBITMQ");
if (useRabbit)
{
    var host = builder.Configuration.GetValue<string>("RabbitMQ:HostName") ?? "rabbitmq";
    var port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
    var user = builder.Configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest";
    var pass = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
    builder.Services.AddSingleton<IMessageBus>(_ => new RabbitMqBus(host, port, user, pass));
}
var sp = builder.Services.BuildServiceProvider();
var store = sp.GetRequiredService<IAutoStore>();
await store.InitAsync(CancellationToken.None);
var hostApp = builder.Build();
await hostApp.RunAsync();
