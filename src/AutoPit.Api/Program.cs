using AutoPit.Api;
using AutoPit.Core;
using AutoPit.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.AddApiInfra();
builder.Services.AddCors(o => o.AddPolicy("auto", p => p.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true)));
var cs = builder.Configuration.GetConnectionString("Sqlite")!;
var proc = builder.Configuration.GetSection("Processing").Get<ProcessingOptions>() ?? new();
builder.Services.AddAutoPit(cs, proc);
var useRabbit = builder.Configuration.GetValue<bool>("USE_RABBITMQ");
if (useRabbit)
{
    var host = builder.Configuration.GetValue<string>("RabbitMQ:HostName") ?? "rabbitmq";
    var port = builder.Configuration.GetValue<int?>("RabbitMQ:Port") ?? 5672;
    var user = builder.Configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest";
    var pass = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
    builder.Services.AddSingleton<IMessageBus>(_ => new RabbitMqBus(host, port, user, pass));
}
var app = builder.Build();
app.UseApiInfra();
app.UseCors("auto");
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<IAutoStore>();
    await store.InitAsync(app.Lifetime.ApplicationStopping);
}
app.MapAutoPit();
app.Run("http://localhost:5190");
