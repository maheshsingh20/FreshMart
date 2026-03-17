using Microsoft.EntityFrameworkCore;
using ProductService.Domain;

namespace ProductService.Infrastructure.Persistence;

public static class ProductDbSeeder
{
    public static async Task SeedAsync(ProductDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var categories = new[]
        {
            Category.Create("Fruits & Vegetables", "Fresh produce",         "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400"),
            Category.Create("Dairy & Eggs",         "Milk, cheese, eggs",   "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400"),
            Category.Create("Bakery",               "Bread and pastries",   "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400"),
            Category.Create("Beverages",            "Drinks and juices",    "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400"),
            Category.Create("Snacks",               "Chips and snacks",     "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400"),
            Category.Create("Meat & Seafood",       "Fresh meat and fish",  "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400"),
            Category.Create("Frozen Foods",         "Frozen meals and veg", "https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=400"),
            Category.Create("Pantry",               "Canned and dry goods", "https://images.unsplash.com/photo-1584473457406-6240486418e9?w=400"),
        };
        await db.Categories.AddRangeAsync(categories);
        await db.SaveChangesAsync();

        var fv = categories[0].Id; var de = categories[1].Id; var bk = categories[2].Id;
        var bv = categories[3].Id; var sn = categories[4].Id; var mt = categories[5].Id;
        var fz = categories[6].Id; var pa = categories[7].Id;

        var products = new[]
        {
            Product.Create("Banana",          "Fresh yellow bananas",          1.99m,  "FV001", "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", fv, 100, "Fresh Farm",  null, "bunch"),
            Product.Create("Apple",           "Crisp red apples",              2.49m,  "FV002", "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400",    fv, 80,  "Orchard",     null, "kg"),
            Product.Create("Tomatoes",        "Vine ripened tomatoes",         2.99m,  "FV003", "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400",    fv, 70,  "Fresh Farm",  null, "kg"),
            Product.Create("Broccoli",        "Fresh green broccoli",          1.79m,  "FV004", "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?w=400", fv, 50,  "Green Valley",null, "head"),
            Product.Create("Spinach",         "Baby spinach leaves 200g",      2.29m,  "FV005", "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400", fv, 60,  "Green Valley",null, "200g"),
            Product.Create("Whole Milk",      "Full cream whole milk 1L",      1.49m,  "DE001", "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400",    de, 60,  "DairyFresh",  null, "1L"),
            Product.Create("Eggs (12 pack)",  "Free range eggs",               3.99m,  "DE002", "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", de, 50,  "Happy Hens",  null, "pack"),
            Product.Create("Cheddar Cheese",  "Mature cheddar 400g",           5.49m,  "DE003", "https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400", de, 40,  "DairyFresh",  null, "400g"),
            Product.Create("Greek Yogurt",    "Thick creamy yogurt 500g",      3.29m,  "DE004", "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400", de, 45,  "Creamy Co",   null, "500g"),
            Product.Create("Sourdough Bread", "Artisan sourdough loaf",        4.50m,  "BK001", "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400", bk, 30,  "Artisan Bake",null, "loaf"),
            Product.Create("Croissant",       "Buttery French croissant",      1.99m,  "BK002", "https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400",    bk, 40,  "Artisan Bake",null, "each"),
            Product.Create("Orange Juice",    "Freshly squeezed OJ 1L",        3.29m,  "BV001", "https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400", bv, 45,  "SunPress",    null, "1L"),
            Product.Create("Sparkling Water", "Natural sparkling water 1.5L",  1.29m,  "BV002", "https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=400",    bv, 90,  "AquaBubble",  null, "1.5L"),
            Product.Create("Green Tea",       "Premium green tea 20 bags",     4.99m,  "BV003", "https://images.unsplash.com/photo-1556679343-c7306c1976bc?w=400",    bv, 55,  "TeaLeaf",     null, "20 bags"),
            Product.Create("Potato Chips",    "Classic salted chips 200g",     2.99m,  "SN001", "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400", sn, 120, "CrunchCo",    null, "200g"),
            Product.Create("Dark Chocolate",  "70% cocoa dark chocolate",      3.49m,  "SN002", "https://images.unsplash.com/photo-1606312619070-d48b4c652a52?w=400", sn, 80,  "ChocoBliss",  null, "100g"),
            Product.Create("Chicken Breast",  "Boneless skinless 500g",        7.99m,  "MT001", "https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400", mt, 35,  "FarmFresh",   null, "500g"),
            Product.Create("Salmon Fillet",   "Atlantic salmon 300g",          9.99m,  "MT002", "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=400", mt, 25,  "OceanCatch",  null, "300g"),
            Product.Create("Frozen Peas",     "Garden peas 500g",              2.49m,  "FZ001", "https://images.unsplash.com/photo-1584568694244-14fbdf83bd30?w=400", fz, 65,  "FrostFresh",  null, "500g"),
            Product.Create("Ice Cream",       "Vanilla bean ice cream 1L",     5.99m,  "FZ002", "https://images.unsplash.com/photo-1497034825429-c343d7c6a68f?w=400", fz, 40,  "CreamDream",  null, "1L"),
            Product.Create("Pasta",           "Spaghetti 500g",                1.99m,  "PA001", "https://images.unsplash.com/photo-1551462147-ff29053bfc14?w=400",    pa, 90,  "ItalFoods",   null, "500g"),
            Product.Create("Olive Oil",       "Extra virgin olive oil 500ml",  8.99m,  "PA002", "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400", pa, 50,  "MedGrove",    null, "500ml"),
        };
        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();
    }
}
