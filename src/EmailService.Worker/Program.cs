using EmailService.Application;
using EmailService.Infrastructure;
using EmailService.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/emailworker-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Add Application and Infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Worker
builder.Services.AddHostedService<EmailWorker>();

var host = builder.Build();

try
{
    Log.Information("Starting Email Worker Service");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
