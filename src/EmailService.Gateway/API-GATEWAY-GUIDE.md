# 🌐 API Gateway Guide

## Overview

The API Gateway provides a **single entry point** for all Email Service APIs using Ocelot.

## 🚀 Quick Start

```bash
# Install dependencies
cd src/EmailService.Gateway
dotnet restore

# Run gateway
dotnet run
```

Gateway will start on: **http://localhost:8080**

## 📋 Configuration Files

### 1. `ocelot.json` (Default - Simple)
- Basic routing
- No rate limiting
- No QoS
- **Use for development/testing**

### 2. `ocelot.full-features.json` (Advanced)
- Rate limiting per route
- QoS with Polly (circuit breaker, timeout, retry)
- Response caching
- **Use for production**

### 3. `ocelot.production.json` (Production)
- Load balancing across multiple instances
- Advanced QoS settings
- Comprehensive rate limiting
- **Use for production with multiple backend servers**

## 🔄 Switch Configuration

Edit `Program.cs` line ~20:

```csharp
// Use simple config (default)
var ocelotConfigFile = "ocelot.json";

// Use full features
var ocelotConfigFile = "ocelot.full-features.json";

// Use production config
var ocelotConfigFile = "ocelot.production.json";
```

Or set environment variable:
```bash
# For production
export ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

## 🗺️ Route Mappings

### Email Service Routes

| Backend API | Gateway Route | Method | Description |
|-------------|---------------|--------|-------------|
| `POST /api/email/send` | `POST /email/send` | POST | Send single email |
| `POST /api/email/send/template` | `POST /email/send/template` | POST | Send template email |
| `POST /api/email/send/bulk` | `POST /email/send/bulk` | POST | Send bulk emails |
| `GET /api/email/status/{id}` | `GET /email/status/{id}` | GET | Get email status |

### Health Routes

| Backend API | Gateway Route | Method |
|-------------|---------------|--------|
| `GET /health` | `GET /health/api` | GET |
| `GET /api/health/detailed` | `GET /health/detailed` | GET |

### Documentation Routes

| Backend API | Gateway Route | Method |
|-------------|---------------|--------|
| `GET /swagger/*` | `GET /docs/*` | GET |

### Gateway-Only Routes

| Gateway Route | Description |
|---------------|-------------|
| `GET /` | Gateway info & available routes |
| `GET /gateway-health` | Gateway health check |
| `GET /gateway-stats` | Gateway statistics |

## 🧪 Testing the Gateway

### 1. Test Gateway is Running

```bash
curl http://localhost:8080/
```

Expected response:
```json
{
  "service": "Email Service API Gateway",
  "version": "1.0.0",
  "status": "running",
  "routes": { ... }
}
```

### 2. Test Health Check

```bash
# Gateway health
curl http://localhost:8080/gateway-health

# API health through gateway
curl http://localhost:8080/health/api
```

### 3. Test Email API Through Gateway

```bash
# Instead of http://localhost:5000/api/email/send
# Use gateway:
curl -X POST "http://localhost:8080/email/send?sendImmediately=true" \
     -H "X-API-Key: test-api-key-12345" \
     -H "Content-Type: application/json" \
     -d '{
       "toEmail": "test@example.com",
       "subject": "Test via Gateway",
       "body": "<h1>Sent through API Gateway!</h1>",
       "isHtml": true
     }'
```

### 4. Test Swagger Through Gateway

Open browser: http://localhost:8080/docs/index.html

## ✨ Features

### 1. Rate Limiting (Full Features Config)

**Limits per route:**
- `POST /email/send` - 100 requests/minute
- `POST /email/send/template` - 50 requests/minute
- `POST /email/send/bulk` - 5 requests/10 minutes
- `GET /email/status/{id}` - 300 requests/minute

**Test rate limiting:**
```bash
# Send 101 requests in 1 minute
for i in {1..101}; do
  curl http://localhost:8080/email/send -H "X-API-Key: test-api-key-12345"
done

# Should get 429 error on 101st request
```

### 2. QoS (Quality of Service) with Polly

**Circuit Breaker:**
- Opens after 3 consecutive failures
- Stays open for 10 seconds
- Prevents cascading failures

**Timeout:**
- `/email/send` - 30 seconds
- `/email/send/bulk` - 120 seconds

**Test circuit breaker:**
```bash
# Stop API service
# Make 4 requests through gateway
# 4th request should fail with circuit breaker open
```

### 3. Response Caching

**Cached routes:**
- `GET /health/api` - 5 seconds TTL
- `GET /email/status/{id}` - 15 seconds TTL

**Test caching:**
```bash
# First request hits backend
time curl http://localhost:8080/health/api

# Second request (within 5s) returns from cache (faster)
time curl http://localhost:8080/health/api
```

### 4. Load Balancing (Production Config)

Configure multiple backend instances:

```json
"DownstreamHostAndPorts": [
  { "Host": "localhost", "Port": 5000 },
  { "Host": "localhost", "Port": 5002 },
  { "Host": "localhost", "Port": 5003 }
]
```

**Algorithms:**
- `RoundRobin` - Distributes evenly
- `LeastConnection` - Routes to least busy
- `NoLoadBalancer` - No load balancing

### 5. Custom Middleware

**GatewayLoggingMiddleware:**
- Logs all requests/responses
- Adds request ID for tracing
- Masks sensitive headers

**ApiKeyValidationMiddleware:**
- Validates API key format
- Adds request ID header
- Blocks invalid requests early

**CircuitBreakerMiddleware:**
- Per-endpoint circuit breakers
- Auto-recovery after cooldown
- Prevents backend overload

## 📊 Monitoring

### View Logs

```bash
# Real-time logs
tail -f logs/gateway-*.txt

# Search logs
grep "ERROR" logs/gateway-*.txt
grep "Circuit breaker OPENED" logs/gateway-*.txt
```

### Check Statistics

```bash
curl http://localhost:8080/gateway-stats
```

## 🏗️ Architecture

```
Client
  │
  ↓
API Gateway (Port 8080)
  │
  ├─→ Email API (Port 5000)
  ├─→ Email API Instance 2 (Port 5002)  [Optional]
  └─→ Admin Dashboard (Port 5001)
```

## ⚙️ Configuration Examples

### Enable All Features

1. Use `ocelot.full-features.json`
2. Ensure Polly is added: `services.AddOcelot().AddPolly()`
3. Restart gateway

### Add Custom Route

Edit `ocelot.json`:

```json
{
  "DownstreamPathTemplate": "/api/your-endpoint",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    { "Host": "localhost", "Port": 5000 }
  ],
  "UpstreamPathTemplate": "/your-route",
  "UpstreamHttpMethod": [ "GET", "POST" ],
  "RateLimitOptions": {
    "EnableRateLimiting": true,
    "Period": "1m",
    "Limit": 100
  }
}
```

### Configure Different Rate Limits

```json
"RateLimitOptions": {
  "ClientWhitelist": [],
  "EnableRateLimiting": true,
  "Period": "5m",        // Period: 1s, 1m, 1h, 1d
  "PeriodTimespan": 300, // Seconds
  "Limit": 50            // Max requests
}
```

### Configure QoS

```json
"QoSOptions": {
  "ExceptionsAllowedBeforeBreaking": 3,  // Circuit breaker threshold
  "DurationOfBreak": 10000,               // ms to stay open
  "TimeoutValue": 30000                   // Request timeout (ms)
}
```

## 🐛 Troubleshooting

### Issue: Gateway won't start

**Error:** "Unable to start Ocelot... QosDelegatingHandlerDelegate"

**Solution:**
```bash
# Ensure Polly is installed
dotnet add package Ocelot.Provider.Polly

# Or use simple ocelot.json without QoS
```

### Issue: Routes not working

**Check:**
1. Backend service is running
2. Port numbers match in config
3. Check gateway logs: `tail -f logs/gateway-*.txt`
4. Test backend directly first

### Issue: Rate limit not working

**Solution:**
- Use `ocelot.full-features.json`
- Check `EnableRateLimiting: true`
- Add `X-Client-Id` header to identify clients

### Issue: Circuit breaker not triggering

**Verify:**
1. Using `ocelot.full-features.json`
2. Polly is registered: `.AddPolly()`
3. QoSOptions configured on route
4. Backend is actually failing (500+ errors)

## 🚦 Production Checklist

- [ ] Use `ocelot.production.json`
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure multiple backend instances
- [ ] Enable HTTPS
- [ ] Set appropriate rate limits
- [ ] Configure logging level
- [ ] Setup health monitoring
- [ ] Test failover scenarios
- [ ] Document custom routes
- [ ] Setup alerting on circuit breaker events

## 📈 Performance Tips

1. **Use caching** for read operations
2. **Set appropriate timeouts** per route complexity
3. **Configure load balancing** for high traffic
4. **Monitor circuit breaker** events
5. **Adjust rate limits** based on capacity
6. **Use connection pooling** (automatic with HTTP client)

## 🔗 Integration with Services

### Update Client Applications

**Before (Direct API):**
```javascript
POST http://localhost:5000/api/email/send
```

**After (Through Gateway):**
```javascript
POST http://localhost:8080/email/send
```

### Update Frontend Configuration

```javascript
// config.js
const API_BASE_URL = process.env.NODE_ENV === 'production' 
  ? 'https://gateway.yourcompany.com'
  : 'http://localhost:8080';
```

## 📚 Additional Resources

- Ocelot Documentation: https://ocelot.readthedocs.io/
- Polly Documentation: https://www.pollydocs.org/
- Rate Limiting: https://ocelot.readthedocs.io/en/latest/features/ratelimiting.html
- QoS: https://ocelot.readthedocs.io/en/latest/features/qualityofservice.html

---

**Your API Gateway is ready! 🚀**
