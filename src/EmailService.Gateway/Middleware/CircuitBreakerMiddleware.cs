using System.Collections.Concurrent;

namespace EmailService.Gateway.Middleware;

public class CircuitBreakerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CircuitBreakerMiddleware> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers;
    private readonly int _failureThreshold = 5;
    private readonly TimeSpan _openDuration = TimeSpan.FromSeconds(30);

    public CircuitBreakerMiddleware(RequestDelegate next, ILogger<CircuitBreakerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerState>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value ?? "/";
        var state = _circuitBreakers.GetOrAdd(endpoint, _ => new CircuitBreakerState());

        // Check if circuit is open
        if (state.IsOpen())
        {
            if (DateTime.UtcNow - state.OpenedAt > _openDuration)
            {
                // Try half-open state
                state.Reset();
                _logger.LogInformation("Circuit breaker for {Endpoint} entering half-open state", endpoint);
            }
            else
            {
                _logger.LogWarning("Circuit breaker OPEN for {Endpoint} - rejecting request", endpoint);
                
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Service Temporarily Unavailable",
                    message = $"Circuit breaker is open for this endpoint. Please retry after {(_openDuration - (DateTime.UtcNow - state.OpenedAt)).TotalSeconds:F0} seconds.",
                    endpoint = endpoint,
                    timestamp = DateTime.UtcNow
                });
                return;
            }
        }

        try
        {
            await _next(context);

            // Check response status
            if (context.Response.StatusCode >= 500)
            {
                state.RecordFailure();
                
                if (state.FailureCount >= _failureThreshold)
                {
                    state.Open();
                    _logger.LogError(
                        "Circuit breaker OPENED for {Endpoint} after {FailureCount} failures",
                        endpoint,
                        state.FailureCount);
                }
            }
            else
            {
                state.RecordSuccess();
            }
        }
        catch (Exception ex)
        {
            state.RecordFailure();
            
            if (state.FailureCount >= _failureThreshold)
            {
                state.Open();
                _logger.LogError(ex,
                    "Circuit breaker OPENED for {Endpoint} after {FailureCount} failures",
                    endpoint,
                    state.FailureCount);
            }
            
            throw;
        }
    }

    private class CircuitBreakerState
    {
        private int _failureCount;
        private bool _isOpen;
        public DateTime OpenedAt { get; private set; }
        public int FailureCount => _failureCount;

        public bool IsOpen() => _isOpen;

        public void RecordFailure()
        {
            Interlocked.Increment(ref _failureCount);
        }

        public void RecordSuccess()
        {
            Interlocked.Exchange(ref _failureCount, 0);
            _isOpen = false;
        }

        public void Open()
        {
            _isOpen = true;
            OpenedAt = DateTime.UtcNow;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _failureCount, 0);
            _isOpen = false;
        }
    }
}
