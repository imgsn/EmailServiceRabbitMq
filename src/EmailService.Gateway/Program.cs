using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;
using Serilog;
using EmailService.Gateway.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Ocelot", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EmailService.Gateway")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Application}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/gateway-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Application}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("Starting Email Service API Gateway...");

// Determine which ocelot config to use based on environment
var environment = builder.Environment.EnvironmentName;
var ocelotConfigFile = environment == "Production"
    ? "ocelot.production.json"
    : "ocelot.full-features.json";

Log.Information("Loading Ocelot configuration: {ConfigFile}", ocelotConfigFile);

// Add Ocelot configuration
builder.Configuration.AddJsonFile(ocelotConfigFile, optional: false, reloadOnChange: true);

// Add services
builder.Services.AddOcelot()
    .AddCacheManager(x =>
    {
        x.WithDictionaryHandle();
    })
    .AddPolly();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsBuilder =>
    {
        corsBuilder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Configure pipeline
app.UseCors("AllowAll");

app.UseResponseCompression();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "[Gateway] {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
});

// Custom middleware
app.UseMiddleware<GatewayLoggingMiddleware>();
app.UseMiddleware<ApiKeyValidationMiddleware>();
app.UseMiddleware<CircuitBreakerMiddleware>();

// Map health check endpoint
app.MapHealthChecks("/gateway-health");

// Gateway info endpoint
app.MapGet("/", () =>
{
    var routes = new
    {
        email_send = "POST /email/send",
        email_send_template = "POST /email/send/template",
        email_send_bulk = "POST /email/send/bulk",
        email_status = "GET /email/status/{id}",
        health_api = "GET /health/api",
        health_detailed = "GET /health/api/detailed",
        swagger_docs = "GET /docs/*",
        gateway_health = "GET /gateway-health"
    };

    var features = new
    {
        rate_limiting = "Enabled (per route)",
        load_balancing = "Round Robin / Least Connection",
        circuit_breaker = "Enabled (5 failures, 30s cooldown)",
        caching = "Enabled for GET endpoints",
        qos = "Quality of Service with timeout & retry",
        logging = "Serilog with file & console",
        cors = "Enabled for all origins"
    };

    return Results.Ok(new
    {
        service = "Email Service API Gateway",
        version = "1.0.0",
        status = "running",
        environment = environment,
        timestamp = DateTime.UtcNow,
        gateway_url = "http://localhost:8080",
        routes,
        features,
        documentation = new
        {
            swagger = "http://localhost:8080/docs/index.html",
            readme = "https://github.com/yourusername/EmailService"
        },
        usage = new
        {
            authentication = "Required: X-API-Key header",
            example = "curl -H \"X-API-Key: your-key\" http://localhost:8080/email/send"
        }
    });
});

// Statistics endpoint
app.MapGet("/gateway-stats", () =>
{
    return Results.Ok(new
    {
        uptime = DateTime.UtcNow.ToString("O"),
        health = "healthy",
        backend_services = new
        {
            email_api = new
            {
                url = "http://localhost:5000",
                status = "up"
            },
            admin_dashboard = new
            {
                url = "http://localhost:5001",
                status = "up"
            }
        }
    });
});

// Use Ocelot
await app.UseOcelot();

Log.Information("╔════════════════════════════════════════════╗");
Log.Information("║  Email Service API Gateway Started        ║");
Log.Information("║  URL: http://localhost:8080               ║");
Log.Information("║  Environment: {Environment,-27} ║", environment);
Log.Information("║  Health: http://localhost:8080/gateway-health ║");
Log.Information("╚════════════════════════════════════════════╝");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
