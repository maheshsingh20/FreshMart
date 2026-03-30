using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// ═══════════════════════════════════════════════════════════════════════════════
// API Gateway — Single Entry Point for All Microservices
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE:
//   This is the only service exposed to the outside world (port 5000).
//   All frontend requests go through here. The gateway:
//     1. Validates JWT tokens (rejects unauthorized requests before forwarding)
//     2. Rate limits by IP to prevent abuse
//     3. Routes requests to the correct microservice via YARP reverse proxy
//     4. Sets CORS and security headers
//
// ROUTING (defined in appsettings.json ReverseProxy section):
//   /api/v1/auth/*         → auth-service:8080
//   /api/v1/products/*     → product-service:8080
//   /api/v1/orders/*       → order-service:8080
//   /api/v1/payments/*     → payment-service:8080
//   /api/v1/notifications/* → notification-service:8080
//   /hubs/notifications/*  → notification-service:8080 (SignalR WebSocket)
//   /hubs/support/*        → support-service:8080 (SignalR WebSocket)
//   ... and more
//
// SECURITY:
//   - JWT validation happens HERE before any request reaches a microservice
//   - Routes marked AuthorizationPolicy="default" require a valid JWT
//   - Public routes (login, register, products) have no auth requirement
//   - Rate limiting: 100 req/min globally, 10 req/5min for login
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

// JWT Auth (validates tokens before forwarding)
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate limiting — configured in appsettings.json IpRateLimiting section
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// YARP Reverse Proxy — routes loaded from appsettings.json ReverseProxy section
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

// CORS — allow the Angular frontend and direct API calls
builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowFrontend", p =>
        p.WithOrigins("http://localhost:4200", "http://localhost:5000", "http://frontend")
         .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");

// Allow Google OAuth popup postMessage (COOP default blocks it)
// Without this, the Google Sign-In popup cannot send the token back to the page
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["Cross-Origin-Opener-Policy"] = "unsafe-none";
    ctx.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none";
    await next();
});

// WebSockets must be enabled before YARP for SignalR hub proxying to work
app.UseWebSockets();
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
