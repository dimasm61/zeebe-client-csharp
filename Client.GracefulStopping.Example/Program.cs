using System.Reflection;
using Client.GracefulStopping.Example;
using Client.GracefulStopping.Example.Workers;
using Grpc.Net.Client;
using Zeebe.Client;
using Zeebe.Client.Api.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddZeebeBuilders();

// extend timeout before abort background services
builder.Services.Configure<HostOptions>(
    options => options.ShutdownTimeout = TimeSpan.FromSeconds(StaticSettings.ShutdownTimeoutSec));

builder.Services.AddScoped<IZeebeClient>(
    serviceProvider => serviceProvider.GetRequiredService<IZeebeClientBuilder>()
        .UseGatewayAddress(StaticSettings.ZeebeUrl).UsePlainText().Build());

builder.Services.AddSingleton<WorkerHandlerCounter>();

builder.Services.AddSingleton<WorkerManager>();

builder.Services.AddHostedService<Worker1>();
//builder.Services.AddHostedService<Worker2>();


var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();