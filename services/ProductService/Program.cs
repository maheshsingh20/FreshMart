using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Domain;
using ProductService.Infrastructure.Persistence;
using Serilog;
using SharedKernel.Messaging;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ProductDb")));

// Redis cache — lazy connect so startup doesn't fail if Redis is unavailable
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false";
if (!redisConn.Contains("abortConnect")) redisConn += ",abortConnect=false";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    try { return ConnectionMultiplexer.Connect(redisConn); }
    catch { return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false"); }
});

// RabbitMQ event bus
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.MapInboundClaims = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<ProductDbContext>();

builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowGateway", p =>
        p.WithOrigins("http://localhost:4200", "http://api-gateway")
         .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();
    await ProductDbSeeder.SeedAsync(db);
}

app.UseSerilogRequestLogging();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors("AllowGateway");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
