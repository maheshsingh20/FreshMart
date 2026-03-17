using AuthService.Application.Services;
using AuthService.Domain;

namespace AuthService.Infrastructure.Persistence;

public static class AuthDbSeeder
{
    public static async Task SeedAsync(AuthDbContext db, IPasswordHasher hasher)
    {
        if (db.Users.Any()) return;

        var users = new[]
        {
            User.Create("admin@grocery.com",    hasher.Hash("Admin@123"),    "Admin",   "User",    UserRole.Admin),
            User.Create("manager@grocery.com",  hasher.Hash("Manager@123"),  "Store",   "Manager", UserRole.StoreManager),
            User.Create("driver@grocery.com",   hasher.Hash("Driver@123"),   "Delivery","Driver",  UserRole.DeliveryDriver),
            User.Create("customer@grocery.com", hasher.Hash("Customer@123"), "John",    "Customer",UserRole.Customer),
        };

        await db.Users.AddRangeAsync(users);
        await db.SaveChangesAsync();
    }
}
