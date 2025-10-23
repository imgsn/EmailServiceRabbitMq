namespace EmailService.Gateway.Middleware;

public class ApiKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyValidationMiddleware> _logger;
    private readonly HashSet<string> _publicPaths;

    public ApiKeyValidationMiddleware(RequestDelegate next, ILogger<ApiKeyValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        // Paths that don't require API key
        _publicPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/",
            "/gateway-health",
            "/health/api",
            "/health/all",
            "/docs"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip validation for public paths
        if (_publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
            path.Contains("/swagger") ||
            path.Contains("/docs"))
        {
            await _next(context);
            return;
        }

        // Check for API key header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey) || 
            string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "Request to {Path} from {IpAddress} missing API key",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API Key is required. Please provide X-API-Key header.",
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            });
            return;
        }

        // Basic API key format validation (you can enhance this)
        var apiKeyValue = apiKey.ToString();
        if (apiKeyValue.Length < 10)
        {
            _logger.LogWarning(
                "Invalid API key format from {IpAddress}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid API Key format",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // Add request ID for tracking
        context.Request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());

        await _next(context);
    }
}
