using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new AppUser { Email = "admin@grocery.com",    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),    FirstName = "Admin",   LastName = "User",    Role = "Admin"          },
                new AppUser { Email = "manager@grocery.com",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),  FirstName = "Store",   LastName = "Manager", Role = "StoreManager"   },
                new AppUser { Email = "driver@grocery.com",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123"),   FirstName = "Delivery",LastName = "Driver",  Role = "DeliveryDriver" },
                new AppUser { Email = "customer@grocery.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"), FirstName = "John",    LastName = "Doe",     Role = "Customer"       }
            );
            await db.SaveChangesAsync();
        }

        if (await db.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { Name = "Fruits & Vegetables", Description = "Fresh produce",          ImageUrl = "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400" },
            new() { Name = "Dairy & Eggs",         Description = "Milk, cheese, eggs",     ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400" },
            new() { Name = "Bakery",               Description = "Bread and pastries",     ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400" },
            new() { Name = "Beverages",            Description = "Drinks and juices",      ImageUrl = "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400" },
            new() { Name = "Snacks",               Description = "Chips and snacks",       ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400" },
            new() { Name = "Meat & Seafood",       Description = "Fresh meat and fish",    ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400" },
            new() { Name = "Frozen Foods",         Description = "Frozen meals and veg",   ImageUrl = "https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=400" },
            new() { Name = "Pantry",               Description = "Canned and dry goods",   ImageUrl = "https://images.unsplash.com/photo-1584473457406-6240486418e9?w=400" },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var fv = categories[0].Id; var de = categories[1].Id; var bk = categories[2].Id;
        var bv = categories[3].Id; var sn = categories[4].Id; var mt = categories[5].Id;
        var fz = categories[6].Id; var pa = categories[7].Id;

        db.Products.AddRange(
            // Fruits & Veg
            new Product { Name = "Banana",           Description = "Fresh yellow bananas",        Price = 1.99m,  Sku = "FV001", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", CategoryId = fv, StockQuantity = 100, Unit = "bunch",  Brand = "Fresh Farm",   AverageRating = 4.5 },
            new Product { Name = "Apple",            Description = "Crisp red apples",            Price = 2.49m,  Sku = "FV002", ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400", CategoryId = fv, StockQuantity = 80,  Unit = "kg",     Brand = "Orchard",      AverageRating = 4.7 },
            new Product { Name = "Tomatoes",         Description = "Vine ripened tomatoes",       Price = 2.99m,  Sku = "FV003", ImageUrl = "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400", CategoryId = fv, StockQuantity = 70,  Unit = "kg",     Brand = "Fresh Farm",   AverageRating = 4.3 },
            new Product { Name = "Broccoli",         Description = "Fresh green broccoli",        Price = 1.79m,  Sku = "FV004", ImageUrl = "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?w=400", CategoryId = fv, StockQuantity = 50,  Unit = "head",   Brand = "Green Valley", AverageRating = 4.2 },
            new Product { Name = "Spinach",          Description = "Baby spinach leaves 200g",    Price = 2.29m,  Sku = "FV005", ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400", CategoryId = fv, StockQuantity = 60,  Unit = "200g",   Brand = "Green Valley", AverageRating = 4.6 },
            // Dairy
            new Product { Name = "Whole Milk",       Description = "Full cream whole milk 1L",    Price = 1.49m,  Sku = "DE001", ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400", CategoryId = de, StockQuantity = 60,  Unit = "1L",     Brand = "DairyFresh",   AverageRating = 4.4 },
            new Product { Name = "Eggs (12 pack)",   Description = "Free range eggs",             Price = 3.99m,  Sku = "DE002", ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", CategoryId = de, StockQuantity = 50,  Unit = "pack",   Brand = "Happy Hens",   AverageRating = 4.8 },
            new Product { Name = "Cheddar Cheese",   Description = "Mature cheddar 400g",         Price = 5.49m,  Sku = "DE003", ImageUrl = "https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400", CategoryId = de, StockQuantity = 40,  Unit = "400g",   Brand = "DairyFresh",   AverageRating = 4.5 },
            new Product { Name = "Greek Yogurt",     Description = "Thick creamy yogurt 500g",    Price = 3.29m,  Sku = "DE004", ImageUrl = "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400", CategoryId = de, StockQuantity = 45,  Unit = "500g",   Brand = "Creamy Co",    AverageRating = 4.6 },
            // Bakery
            new Product { Name = "Sourdough Bread",  Description = "Artisan sourdough loaf",      Price = 4.50m,  Sku = "BK001", ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400", CategoryId = bk, StockQuantity = 30,  Unit = "loaf",   Brand = "Artisan Bake", AverageRating = 4.9 },
            new Product { Name = "Croissant",        Description = "Buttery French croissant",    Price = 1.99m,  Sku = "BK002", ImageUrl = "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400", CategoryId = bk, StockQuantity = 40,  Unit = "each",   Brand = "Artisan Bake", AverageRating = 4.7 },
            // Beverages
            new Product { Name = "Orange Juice",     Description = "Freshly squeezed OJ 1L",      Price = 3.29m,  Sku = "BV001", ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400", CategoryId = bv, StockQuantity = 45,  Unit = "1L",     Brand = "SunPress",     AverageRating = 4.5 },
            new Product { Name = "Sparkling Water",  Description = "Natural sparkling water 1.5L", Price = 1.29m, Sku = "BV002", ImageUrl = "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=400", CategoryId = bv, StockQuantity = 90,  Unit = "1.5L",   Brand = "AquaBubble",   AverageRating = 4.3 },
            new Product { Name = "Green Tea",        Description = "Premium green tea 20 bags",   Price = 4.99m,  Sku = "BV003", ImageUrl = "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400", CategoryId = bv, StockQuantity = 55,  Unit = "20 bags",Brand = "TeaLeaf",      AverageRating = 4.6 },
            // Snacks
            new Product { Name = "Potato Chips",     Description = "Classic salted chips 200g",   Price = 2.99m,  Sku = "SN001", ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400", CategoryId = sn, StockQuantity = 120, Unit = "200g",   Brand = "CrunchCo",     AverageRating = 4.2 },
            new Product { Name = "Dark Chocolate",   Description = "70% cocoa dark chocolate",    Price = 3.49m,  Sku = "SN002", ImageUrl = "https://images.unsplash.com/photo-1606312619070-d48b4c652a52?w=400", CategoryId = sn, StockQuantity = 80,  Unit = "100g",   Brand = "ChocoBliss",   AverageRating = 4.8 },
            // Meat
            new Product { Name = "Chicken Breast",   Description = "Boneless skinless 500g",      Price = 7.99m,  Sku = "MT001", ImageUrl = "https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400", CategoryId = mt, StockQuantity = 35,  Unit = "500g",   Brand = "FarmFresh",    AverageRating = 4.5 },
            new Product { Name = "Salmon Fillet",    Description = "Atlantic salmon 300g",         Price = 9.99m,  Sku = "MT002", ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=400", CategoryId = mt, StockQuantity = 25,  Unit = "300g",   Brand = "OceanCatch",   AverageRating = 4.7 },
            // Frozen
            new Product { Name = "Frozen Peas",      Description = "Garden peas 500g",            Price = 2.49m,  Sku = "FZ001", ImageUrl = "https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=400", CategoryId = fz, StockQuantity = 65,  Unit = "500g",   Brand = "FrostFresh",   AverageRating = 4.1 },
            new Product { Name = "Ice Cream",        Description = "Vanilla bean ice cream 1L",   Price = 5.99m,  Sku = "FZ002", ImageUrl = "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?w=400", CategoryId = fz, StockQuantity = 40,  Unit = "1L",     Brand = "CreamDream",   AverageRating = 4.9 },
            // Pantry
            new Product { Name = "Pasta",            Description = "Spaghetti 500g",              Price = 1.99m,  Sku = "PA001", ImageUrl = "https://images.unsplash.com/photo-1551462147-ff29053bfc14?w=400", CategoryId = pa, StockQuantity = 90,  Unit = "500g",   Brand = "ItalFoods",    AverageRating = 4.4 },
            new Product { Name = "Olive Oil",        Description = "Extra virgin olive oil 500ml",Price = 8.99m,  Sku = "PA002", ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400", CategoryId = pa, StockQuantity = 50,  Unit = "500ml",  Brand = "MedGrove",     AverageRating = 4.7 }
        );
        await db.SaveChangesAsync();

        // Seed demo coupons
        if (!await db.Coupons.AnyAsync())
        {
            db.Coupons.AddRange(
                new Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 200, UsageLimit = 1000, IsActive = true },
                new Coupon { Code = "FLAT50",    DiscountType = "Fixed",      DiscountValue = 50, MinOrderAmount = 500, UsageLimit = 500,  IsActive = true },
                new Coupon { Code = "FRESH20",   DiscountType = "Percentage", DiscountValue = 20, MinOrderAmount = 300, UsageLimit = 200,  IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(3) }
            );
            await db.SaveChangesAsync();
        }
    }
}
