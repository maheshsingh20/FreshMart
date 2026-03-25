using AuthService.Application.Services;
using AuthService.Domain;

namespace AuthService.Infrastructure.Persistence;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(AuthDbContext db, IPasswordHasher hasher)
    {
        if (db.Users.Any()) return;

        var seeds = new[]
        {
            ("ankitkumarkunwar8@gmail.com", "Admin@123",    "Admin",    "User",     UserRole.Admin),
            ("manager@freshmart.in",        "Manager@123",  "Store",    "Manager",  UserRole.StoreManager),
            ("driver@freshmart.in",         "Driver@123",   "Delivery", "Driver",   UserRole.DeliveryDriver),
            ("customer@freshmart.in",       "Customer@123", "John",     "Customer", UserRole.Customer),
        };

        foreach (var (email, password, first, last, role) in seeds)
        {
            var user = User.Create(email, hasher.Hash(password), first, last, role);
            user.VerifyEmail();
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }
}
