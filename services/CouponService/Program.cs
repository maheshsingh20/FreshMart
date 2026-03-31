using CouponService.Application.Commands;
using CouponService.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<CouponDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("CouponDb")));

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
builder.Services.AddScoped<ValidateCouponHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<CouponDbContext>();

builder.Services.AddCors(opt =>
    opt.AddPolicy("AllowGateway", p =>
        p.WithOrigins("http://localhost:4200", "http://api-gateway")
         .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CouponDbContext>();
    try { await db.Database.EnsureCreatedAsync(); } catch { /* DB already exists */ }
    if (!db.Coupons.Any())
    {
        db.Coupons.AddRange(
            new CouponService.Domain.Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 200, UsageLimit = 1000, IsActive = true },
            new CouponService.Domain.Coupon { Code = "SAVE50", DiscountType = "Fixed", DiscountValue = 50, MinOrderAmount = 500, UsageLimit = 500, IsActive = true },
            new CouponService.Domain.Coupon { Code = "FRESH20", DiscountType = "Percentage", DiscountValue = 20, MinOrderAmount = 300, UsageLimit = 200, IsActive = true }
        );
        await db.SaveChangesAsync();
    }
}

app.UseSerilogRequestLogging();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors("AllowGateway");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
