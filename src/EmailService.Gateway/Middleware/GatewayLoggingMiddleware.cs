namespace EmailService.Gateway.Middleware;

public class GatewayLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;

    public GatewayLoggingMiddleware(RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation(
            "[{RequestId}] Gateway Request: {Method} {Path} from {IpAddress}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        // Log headers (excluding sensitive ones)
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            var maskedKey = apiKey.ToString().Length > 10 
                ? apiKey.ToString().Substring(0, 10) + "..." 
                : "***";
            _logger.LogDebug("[{RequestId}] API Key: {ApiKey}", requestId, maskedKey);
        }

        try
        {
            await _next(context);

            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation(
                "[{RequestId}] Gateway Response: {StatusCode} in {Duration}ms",
                requestId,
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogError(ex,
                "[{RequestId}] Gateway Error: {Method} {Path} failed after {Duration}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                duration.TotalMilliseconds);
            
            throw;
        }
    }
}
