using AuthService.Application.Services;
using AuthService.Domain;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SharedKernel.Messaging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AuthDb")));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddScoped<INotificationRelay, NotificationRelay>();
builder.Services.AddHttpClient();

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
builder.Services.AddHealthChecks().AddDbContextCheck<AuthDbContext>();

builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowGateway", p =>
        p.WithOrigins("http://localhost:4200", "http://api-gateway")
         .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.EnsureCreatedAsync();
    // Add OTP columns if they don't exist yet (idempotent schema update)
    await db.Database.ExecuteSqlRawAsync("""
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'OtpHash')
            ALTER TABLE Users ADD OtpHash nvarchar(512) NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'OtpExpiry')
            ALTER TABLE Users ADD OtpExpiry datetime2 NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'OtpPurpose')
            ALTER TABLE Users ADD OtpPurpose nvarchar(50) NULL;
    """);
    await AuthDbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.UseSerilogRequestLogging();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors("AllowGateway");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
