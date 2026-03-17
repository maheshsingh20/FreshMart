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

        // Seed categories — idempotent by name
        var allCategoryDefs = new List<(string Name, string Desc, string Img)>
        {
            ("Fruits & Vegetables",       "Fresh fruits and vegetables",           "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400"),
            ("Dairy, Bread & Eggs",       "Milk, cheese, bread and eggs",          "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400"),
            ("Chicken, Meat & Fish",      "Fresh chicken, meat and seafood",       "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400"),
            ("Snacks & Munchies",         "Chips, biscuits and snacks",            "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400"),
            ("Cold Drinks & Juices",      "Soft drinks, juices and water",         "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400"),
            ("Tea, Coffee & Milk Drinks", "Tea, coffee and hot beverages",         "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400"),
            ("Bakery & Biscuits",         "Bread, cakes and biscuits",             "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400"),
            ("Atta, Rice & Dal",          "Staple grains, pulses and flour",       "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400"),
            ("Oil & More",                "Cooking oils, ghee and vinegar",        "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400"),
            ("Sauces & Spreads",          "Ketchup, jams, pickles and spreads",    "https://images.unsplash.com/photo-1472476443507-c7a5948772fc?w=400"),
            ("Organic & Healthy Living",  "Organic and health food products",      "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=400"),
            ("Breakfast & Instant Food",  "Cereals, oats and instant noodles",     "https://images.unsplash.com/photo-1517093157656-b9eccef91cb1?w=400"),
            ("Sweet Tooth",               "Chocolates, candies and desserts",      "https://images.unsplash.com/photo-1606312619070-d48b4c652a52?w=400"),
            ("Paan Corner",               "Paan, mukhwas and mouth fresheners",    "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400"),
            ("Masala & Spices",           "Indian spices and masalas",             "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400"),
            ("Cleaning Essentials",       "Detergents, cleaners and disinfectants","https://images.unsplash.com/photo-1563453392212-326f5e854473?w=400"),
            ("Home & Office",             "Stationery, storage and home needs",    "https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=400"),
            ("Personal Care",             "Soaps, shampoos and grooming",          "https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400"),
            ("Baby Care",                 "Baby food, diapers and care products",  "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?w=400"),
            ("Pharma & Wellness",         "Vitamins, supplements and medicines",   "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400"),
            ("Pet Care",                  "Pet food and accessories",              "https://images.unsplash.com/photo-1601758124510-52d02ddb7cbd?w=400"),
        };

        var existingCatNames = await db.Categories.Select(c => c.Name).ToListAsync();
        var newCats = allCategoryDefs
            .Where(d => !existingCatNames.Contains(d.Name))
            .Select(d => new Category { Name = d.Name, Description = d.Desc, ImageUrl = d.Img })
            .ToList();
        if (newCats.Any()) { db.Categories.AddRange(newCats); await db.SaveChangesAsync(); }

        if (await db.Products.AnyAsync())
        {
            // Seed coupons if missing and return
            if (!await db.Coupons.AnyAsync()) await SeedCouponsAsync(db);
            return;
        }

        var cats = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        Guid C(string name) => cats.TryGetValue(name, out var id) ? id : cats.Values.First();

        db.Products.AddRange(
            // Fruits & Vegetables
            new Product { Name = "Banana",               Description = "Fresh yellow bananas",            Price = 40m,  Sku = "FV001", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 100, Unit = "dozen",    Brand = "Fresh Farm",   AverageRating = 4.5 },
            new Product { Name = "Apple",                Description = "Crisp red apples",                Price = 180m, Sku = "FV002", ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 80,  Unit = "kg",       Brand = "Orchard",      AverageRating = 4.7 },
            new Product { Name = "Tomato",               Description = "Vine ripened tomatoes",           Price = 40m,  Sku = "FV003", ImageUrl = "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 70,  Unit = "kg",       Brand = "Fresh Farm",   AverageRating = 4.3 },
            new Product { Name = "Onion",                Description = "Fresh red onions",                Price = 35m,  Sku = "FV004", ImageUrl = "https://images.unsplash.com/photo-1508747703725-719777637510?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 120, Unit = "kg",       Brand = "Fresh Farm",   AverageRating = 4.2 },
            new Product { Name = "Spinach",              Description = "Baby spinach leaves 200g",        Price = 30m,  Sku = "FV005", ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 60,  Unit = "200g",     Brand = "Green Valley", AverageRating = 4.6 },
            new Product { Name = "Potato",               Description = "Fresh potatoes",                  Price = 30m,  Sku = "FV006", ImageUrl = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400", CategoryId = C("Fruits & Vegetables"),       StockQuantity = 150, Unit = "kg",       Brand = "Fresh Farm",   AverageRating = 4.4 },
            // Dairy, Bread & Eggs
            new Product { Name = "Amul Milk",            Description = "Full cream milk 1L",              Price = 68m,  Sku = "DE001", ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400", CategoryId = C("Dairy, Bread & Eggs"),       StockQuantity = 60,  Unit = "1L",       Brand = "Amul",         AverageRating = 4.4 },
            new Product { Name = "Eggs",                 Description = "Farm fresh eggs pack of 12",      Price = 90m,  Sku = "DE002", ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", CategoryId = C("Dairy, Bread & Eggs"),       StockQuantity = 50,  Unit = "12 pack",  Brand = "Country Eggs", AverageRating = 4.8 },
            new Product { Name = "Amul Butter",          Description = "Salted butter 500g",              Price = 260m, Sku = "DE003", ImageUrl = "https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400", CategoryId = C("Dairy, Bread & Eggs"),       StockQuantity = 40,  Unit = "500g",     Brand = "Amul",         AverageRating = 4.5 },
            new Product { Name = "Bread",                Description = "Whole wheat sandwich bread",      Price = 45m,  Sku = "DE004", ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400", CategoryId = C("Dairy, Bread & Eggs"),       StockQuantity = 45,  Unit = "400g",     Brand = "Britannia",    AverageRating = 4.3 },
            new Product { Name = "Paneer",               Description = "Fresh cottage cheese 200g",       Price = 90m,  Sku = "DE005", ImageUrl = "https://images.unsplash.com/photo-1631452180519-c014fe946bc7?w=400", CategoryId = C("Dairy, Bread & Eggs"),       StockQuantity = 35,  Unit = "200g",     Brand = "Amul",         AverageRating = 4.6 },
            // Chicken, Meat & Fish
            new Product { Name = "Chicken Breast",       Description = "Boneless skinless 500g",          Price = 220m, Sku = "MT001", ImageUrl = "https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400", CategoryId = C("Chicken, Meat & Fish"),      StockQuantity = 35,  Unit = "500g",     Brand = "FreshMeat",    AverageRating = 4.5 },
            new Product { Name = "Mutton",               Description = "Fresh mutton curry cut 500g",     Price = 380m, Sku = "MT002", ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400", CategoryId = C("Chicken, Meat & Fish"),      StockQuantity = 25,  Unit = "500g",     Brand = "FreshMeat",    AverageRating = 4.4 },
            new Product { Name = "Rohu Fish",            Description = "Fresh rohu fish 500g",            Price = 180m, Sku = "MT003", ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=400", CategoryId = C("Chicken, Meat & Fish"),      StockQuantity = 20,  Unit = "500g",     Brand = "OceanFresh",   AverageRating = 4.3 },
            // Snacks & Munchies
            new Product { Name = "Lays Classic",         Description = "Classic salted chips 26g",        Price = 20m,  Sku = "SN001", ImageUrl = "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400", CategoryId = C("Snacks & Munchies"),         StockQuantity = 120, Unit = "26g",      Brand = "Lays",         AverageRating = 4.2 },
            new Product { Name = "Kurkure",              Description = "Masala munch 90g",                Price = 30m,  Sku = "SN002", ImageUrl = "https://images.unsplash.com/photo-1599490659213-e2b9527bd087?w=400", CategoryId = C("Snacks & Munchies"),         StockQuantity = 100, Unit = "90g",      Brand = "Kurkure",      AverageRating = 4.4 },
            new Product { Name = "Haldiram Bhujia",      Description = "Aloo bhujia 400g",                Price = 120m, Sku = "SN003", ImageUrl = "https://images.unsplash.com/photo-1601050690597-df0568f70950?w=400", CategoryId = C("Snacks & Munchies"),         StockQuantity = 80,  Unit = "400g",     Brand = "Haldiram",     AverageRating = 4.7 },
            // Cold Drinks & Juices
            new Product { Name = "Coca Cola",            Description = "Refreshing cola 750ml",           Price = 45m,  Sku = "CD001", ImageUrl = "https://images.unsplash.com/photo-1554866585-cd94860890b7?w=400", CategoryId = C("Cold Drinks & Juices"),      StockQuantity = 90,  Unit = "750ml",    Brand = "Coca Cola",    AverageRating = 4.3 },
            new Product { Name = "Real Orange Juice",    Description = "100% orange juice 1L",            Price = 120m, Sku = "CD002", ImageUrl = "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400", CategoryId = C("Cold Drinks & Juices"),      StockQuantity = 55,  Unit = "1L",       Brand = "Real",         AverageRating = 4.5 },
            new Product { Name = "Sprite",               Description = "Lemon lime drink 750ml",          Price = 45m,  Sku = "CD003", ImageUrl = "https://images.unsplash.com/photo-1625772299848-391b6a87d7b3?w=400", CategoryId = C("Cold Drinks & Juices"),      StockQuantity = 85,  Unit = "750ml",    Brand = "Sprite",       AverageRating = 4.2 },
            // Tea, Coffee & Milk Drinks
            new Product { Name = "Tata Chai",            Description = "Premium tea 250g",                Price = 130m, Sku = "TC001", ImageUrl = "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400", CategoryId = C("Tea, Coffee & Milk Drinks"), StockQuantity = 70,  Unit = "250g",     Brand = "Tata Tea",     AverageRating = 4.6 },
            new Product { Name = "Nescafe Classic",      Description = "Instant coffee 100g",             Price = 280m, Sku = "TC002", ImageUrl = "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400", CategoryId = C("Tea, Coffee & Milk Drinks"), StockQuantity = 50,  Unit = "100g",     Brand = "Nescafe",      AverageRating = 4.5 },
            new Product { Name = "Horlicks",             Description = "Health drink 500g",               Price = 290m, Sku = "TC003", ImageUrl = "https://images.unsplash.com/photo-1517093157656-b9eccef91cb1?w=400", CategoryId = C("Tea, Coffee & Milk Drinks"), StockQuantity = 40,  Unit = "500g",     Brand = "Horlicks",     AverageRating = 4.4 },
            // Bakery & Biscuits
            new Product { Name = "Parle-G",              Description = "Glucose biscuits 800g",           Price = 60m,  Sku = "BB001", ImageUrl = "https://images.unsplash.com/photo-1558961363-fa8fdf82db35?w=400", CategoryId = C("Bakery & Biscuits"),         StockQuantity = 100, Unit = "800g",     Brand = "Parle",        AverageRating = 4.8 },
            new Product { Name = "Britannia Good Day",   Description = "Butter cookies 200g",             Price = 35m,  Sku = "BB002", ImageUrl = "https://images.unsplash.com/photo-1499636136210-6f4ee915583e?w=400", CategoryId = C("Bakery & Biscuits"),         StockQuantity = 90,  Unit = "200g",     Brand = "Britannia",    AverageRating = 4.6 },
            // Atta, Rice & Dal
            new Product { Name = "Aashirvaad Atta",      Description = "Whole wheat flour 5kg",           Price = 280m, Sku = "AR001", ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400", CategoryId = C("Atta, Rice & Dal"),          StockQuantity = 60,  Unit = "5kg",      Brand = "Aashirvaad",   AverageRating = 4.7 },
            new Product { Name = "Basmati Rice",         Description = "Long grain basmati 5kg",          Price = 450m, Sku = "AR002", ImageUrl = "https://images.unsplash.com/photo-1536304993881-ff86e0c9b589?w=400", CategoryId = C("Atta, Rice & Dal"),          StockQuantity = 50,  Unit = "5kg",      Brand = "India Gate",   AverageRating = 4.8 },
            new Product { Name = "Toor Dal",             Description = "Split pigeon peas 1kg",           Price = 140m, Sku = "AR003", ImageUrl = "https://images.unsplash.com/photo-1515543237350-b3ecd2612a25?w=400", CategoryId = C("Atta, Rice & Dal"),          StockQuantity = 80,  Unit = "1kg",      Brand = "Tata Sampann", AverageRating = 4.5 },
            // Oil & More
            new Product { Name = "Fortune Sunflower Oil",Description = "Refined sunflower oil 1L",        Price = 160m, Sku = "OL001", ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400", CategoryId = C("Oil & More"),                StockQuantity = 55,  Unit = "1L",       Brand = "Fortune",      AverageRating = 4.4 },
            new Product { Name = "Amul Ghee",            Description = "Pure cow ghee 500ml",             Price = 320m, Sku = "OL002", ImageUrl = "https://images.unsplash.com/photo-1631452180519-c014fe946bc7?w=400", CategoryId = C("Oil & More"),                StockQuantity = 40,  Unit = "500ml",    Brand = "Amul",         AverageRating = 4.9 },
            // Sauces & Spreads
            new Product { Name = "Maggi Ketchup",        Description = "Tomato ketchup 1kg",              Price = 130m, Sku = "SS001", ImageUrl = "https://images.unsplash.com/photo-1472476443507-c7a5948772fc?w=400", CategoryId = C("Sauces & Spreads"),          StockQuantity = 65,  Unit = "1kg",      Brand = "Maggi",        AverageRating = 4.5 },
            new Product { Name = "Kissan Jam",           Description = "Mixed fruit jam 500g",            Price = 120m, Sku = "SS002", ImageUrl = "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400", CategoryId = C("Sauces & Spreads"),          StockQuantity = 50,  Unit = "500g",     Brand = "Kissan",       AverageRating = 4.4 },
            // Masala & Spices
            new Product { Name = "MDH Garam Masala",     Description = "Aromatic spice blend 100g",       Price = 85m,  Sku = "MS001", ImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400", CategoryId = C("Masala & Spices"),           StockQuantity = 90,  Unit = "100g",     Brand = "MDH",          AverageRating = 4.7 },
            new Product { Name = "Turmeric Powder",      Description = "Pure haldi powder 200g",          Price = 55m,  Sku = "MS002", ImageUrl = "https://images.unsplash.com/photo-1615485500704-8e990f9900f7?w=400", CategoryId = C("Masala & Spices"),           StockQuantity = 100, Unit = "200g",     Brand = "Everest",      AverageRating = 4.6 },
            new Product { Name = "Red Chilli Powder",    Description = "Hot red chilli 200g",             Price = 60m,  Sku = "MS003", ImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400", CategoryId = C("Masala & Spices"),           StockQuantity = 85,  Unit = "200g",     Brand = "Everest",      AverageRating = 4.5 },
            // Organic & Healthy Living
            new Product { Name = "Organic Honey",        Description = "Pure natural honey 500g",         Price = 350m, Sku = "OH001", ImageUrl = "https://images.unsplash.com/photo-1587049352846-4a222e784d38?w=400", CategoryId = C("Organic & Healthy Living"),  StockQuantity = 40,  Unit = "500g",     Brand = "Dabur",        AverageRating = 4.8 },
            new Product { Name = "Oats",                 Description = "Rolled oats 1kg",                 Price = 180m, Sku = "OH002", ImageUrl = "https://images.unsplash.com/photo-1517093157656-b9eccef91cb1?w=400", CategoryId = C("Organic & Healthy Living"),  StockQuantity = 60,  Unit = "1kg",      Brand = "Quaker",       AverageRating = 4.6 },
            // Breakfast & Instant Food
            new Product { Name = "Maggi Noodles",        Description = "Masala instant noodles 70g",      Price = 14m,  Sku = "BI001", ImageUrl = "https://images.unsplash.com/photo-1569718212165-3a8278d5f624?w=400", CategoryId = C("Breakfast & Instant Food"),  StockQuantity = 200, Unit = "70g",      Brand = "Maggi",        AverageRating = 4.7 },
            new Product { Name = "Cornflakes",           Description = "Kellogg's cornflakes 875g",       Price = 320m, Sku = "BI002", ImageUrl = "https://images.unsplash.com/photo-1517093157656-b9eccef91cb1?w=400", CategoryId = C("Breakfast & Instant Food"),  StockQuantity = 45,  Unit = "875g",     Brand = "Kellogg's",    AverageRating = 4.5 },
            // Sweet Tooth
            new Product { Name = "Dairy Milk",           Description = "Milk chocolate 150g",             Price = 120m, Sku = "SW001", ImageUrl = "https://images.unsplash.com/photo-1606312619070-d48b4c652a52?w=400", CategoryId = C("Sweet Tooth"),               StockQuantity = 80,  Unit = "150g",     Brand = "Cadbury",      AverageRating = 4.9 },
            new Product { Name = "KitKat",               Description = "Wafer chocolate 4 finger",        Price = 50m,  Sku = "SW002", ImageUrl = "https://images.unsplash.com/photo-1621939514649-280e2ee25f60?w=400", CategoryId = C("Sweet Tooth"),               StockQuantity = 100, Unit = "41.5g",    Brand = "Nestle",       AverageRating = 4.7 },
            // Cleaning Essentials
            new Product { Name = "Surf Excel",           Description = "Washing powder 1kg",              Price = 180m, Sku = "CL001", ImageUrl = "https://images.unsplash.com/photo-1563453392212-326f5e854473?w=400", CategoryId = C("Cleaning Essentials"),       StockQuantity = 70,  Unit = "1kg",      Brand = "Surf Excel",   AverageRating = 4.6 },
            new Product { Name = "Colin Glass Cleaner",  Description = "Glass and surface cleaner 500ml", Price = 120m, Sku = "CL002", ImageUrl = "https://images.unsplash.com/photo-1563453392212-326f5e854473?w=400", CategoryId = C("Cleaning Essentials"),       StockQuantity = 55,  Unit = "500ml",    Brand = "Colin",        AverageRating = 4.4 },
            // Personal Care
            new Product { Name = "Dove Soap",            Description = "Moisturising beauty bar 100g",    Price = 55m,  Sku = "PC001", ImageUrl = "https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400", CategoryId = C("Personal Care"),             StockQuantity = 90,  Unit = "100g",     Brand = "Dove",         AverageRating = 4.7 },
            new Product { Name = "Head and Shoulders",   Description = "Anti-dandruff shampoo 340ml",     Price = 280m, Sku = "PC002", ImageUrl = "https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400", CategoryId = C("Personal Care"),             StockQuantity = 50,  Unit = "340ml",    Brand = "H&S",          AverageRating = 4.5 },
            // Baby Care
            new Product { Name = "Pampers Diapers",      Description = "Baby dry diapers M 56 count",     Price = 850m, Sku = "BC001", ImageUrl = "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?w=400", CategoryId = C("Baby Care"),                 StockQuantity = 30,  Unit = "56 count", Brand = "Pampers",      AverageRating = 4.8 },
            new Product { Name = "Cerelac",              Description = "Baby wheat cereal 300g",          Price = 220m, Sku = "BC002", ImageUrl = "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?w=400", CategoryId = C("Baby Care"),                 StockQuantity = 35,  Unit = "300g",     Brand = "Nestle",       AverageRating = 4.7 },
            // Pharma & Wellness
            new Product { Name = "Vitamin C 1000mg",     Description = "Immunity booster 60 tablets",     Price = 450m, Sku = "PW001", ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400", CategoryId = C("Pharma & Wellness"),          StockQuantity = 45,  Unit = "60 tabs",  Brand = "HealthVit",    AverageRating = 4.6 },
            new Product { Name = "Dettol Sanitizer",     Description = "Hand sanitizer 500ml",            Price = 180m, Sku = "PW002", ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=400", CategoryId = C("Pharma & Wellness"),          StockQuantity = 60,  Unit = "500ml",    Brand = "Dettol",       AverageRating = 4.7 },
            // Pet Care
            new Product { Name = "Pedigree Dog Food",    Description = "Adult dog food chicken 3kg",      Price = 780m, Sku = "PT001", ImageUrl = "https://images.unsplash.com/photo-1601758124510-52d02ddb7cbd?w=400", CategoryId = C("Pet Care"),                  StockQuantity = 25,  Unit = "3kg",      Brand = "Pedigree",     AverageRating = 4.7 },
            // Paan Corner
            new Product { Name = "Rajnigandha",          Description = "Silver coated paan masala 4.4g",  Price = 10m,  Sku = "PN001", ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400", CategoryId = C("Paan Corner"),               StockQuantity = 200, Unit = "4.4g",     Brand = "Rajnigandha",  AverageRating = 4.3 },
            // Home & Office
            new Product { Name = "Classmate Notebook",   Description = "Single line notebook 172 pages",  Price = 55m,  Sku = "HO001", ImageUrl = "https://images.unsplash.com/photo-1484101403633-562f891dc89a?w=400", CategoryId = C("Home & Office"),             StockQuantity = 80,  Unit = "each",     Brand = "Classmate",    AverageRating = 4.5 }
        );
        await db.SaveChangesAsync();

        await SeedCouponsAsync(db);
    }

    private static async Task SeedCouponsAsync(AppDbContext db)
    {
        if (await db.Coupons.AnyAsync()) return;
        db.Coupons.AddRange(
            new Coupon { Code = "WELCOME10", DiscountType = "Percentage", DiscountValue = 10, MinOrderAmount = 200, UsageLimit = 1000, IsActive = true },
            new Coupon { Code = "FLAT50",    DiscountType = "Fixed",      DiscountValue = 50, MinOrderAmount = 500, UsageLimit = 500,  IsActive = true },
            new Coupon { Code = "FRESH20",   DiscountType = "Percentage", DiscountValue = 20, MinOrderAmount = 300, UsageLimit = 200,  IsActive = true, ExpiresAt = DateTime.UtcNow.AddMonths(3) }
        );
        await db.SaveChangesAsync();
    }
}
