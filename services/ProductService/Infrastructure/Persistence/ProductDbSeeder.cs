using ProductService.Domain;

namespace ProductService.Infrastructure.Persistence;

public static class ProductDbSeeder
{
    public static async Task SeedAsync(ProductDbContext db)
    {
        if (db.Categories.Any()) return;

        var cats = new[]
        {
            ("Fruits & Vegetables", "Fresh fruits and vegetables", "https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400"),
            ("Dairy, Bread & Eggs", "Milk, cheese, bread and eggs", "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400"),
            ("Snacks & Munchies", "Chips, biscuits and snacks", "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400"),
            ("Cold Drinks & Juices", "Soft drinks, juices and water", "https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400"),
            ("Atta, Rice & Dal", "Staple grains, pulses and flour", "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400"),
            ("Masala & Spices", "Indian spices and masalas", "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400"),
            ("Personal Care", "Soaps, shampoos and grooming", "https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400"),
            ("Cleaning Essentials", "Detergents, cleaners and disinfectants", "https://images.unsplash.com/photo-1563453392212-326f5e854473?w=400"),
        };

        var categories = cats.Select(c => Category.Create(c.Item1, c.Item2, c.Item3)).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        Guid C(string name) => categories.First(c => c.Name == name).Id;

        var products = new[]
        {
            Product.Create("Banana", "Fresh yellow bananas", 40m, "FV001", "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400", C("Fruits & Vegetables"), 100, "Fresh Farm", null, "dozen"),
            Product.Create("Apple", "Crisp red apples", 180m, "FV002", "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400", C("Fruits & Vegetables"), 80, "Orchard", null, "kg"),
            Product.Create("Tomato", "Vine ripened tomatoes", 40m, "FV003", "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400", C("Fruits & Vegetables"), 70, "Fresh Farm", null, "kg"),
            Product.Create("Amul Milk", "Full cream milk 1L", 68m, "DE001", "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400", C("Dairy, Bread & Eggs"), 60, "Amul", null, "1L"),
            Product.Create("Eggs", "Farm fresh eggs pack of 12", 90m, "DE002", "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400", C("Dairy, Bread & Eggs"), 50, "Country Eggs", null, "12 pack"),
            Product.Create("Lays Classic", "Classic salted chips 26g", 20m, "SN001", "https://images.unsplash.com/photo-1566478989037-eec170784d0b?w=400", C("Snacks & Munchies"), 120, "Lays", null, "26g"),
            Product.Create("Kurkure", "Masala munch 90g", 30m, "SN002", "https://images.unsplash.com/photo-1599490659213-e2b9527bd087?w=400", C("Snacks & Munchies"), 100, "Kurkure", null, "90g"),
            Product.Create("Coca Cola", "Refreshing cola 750ml", 45m, "CD001", "https://images.unsplash.com/photo-1554866585-cd94860890b7?w=400", C("Cold Drinks & Juices"), 90, "Coca Cola", null, "750ml"),
            Product.Create("Aashirvaad Atta", "Whole wheat flour 5kg", 280m, "AR001", "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400", C("Atta, Rice & Dal"), 60, "Aashirvaad", null, "5kg"),
            Product.Create("Basmati Rice", "Long grain basmati 5kg", 450m, "AR002", "https://images.unsplash.com/photo-1536304993881-ff86e0c9b589?w=400", C("Atta, Rice & Dal"), 50, "India Gate", null, "5kg"),
            Product.Create("MDH Garam Masala", "Aromatic spice blend 100g", 85m, "MS001", "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?w=400", C("Masala & Spices"), 90, "MDH", null, "100g"),
            Product.Create("Dove Soap", "Moisturising beauty bar 100g", 55m, "PC001", "https://images.unsplash.com/photo-1556228578-8c89e6adf883?w=400", C("Personal Care"), 90, "Dove", null, "100g"),
            Product.Create("Surf Excel", "Washing powder 1kg", 180m, "CL001", "https://images.unsplash.com/photo-1563453392212-326f5e854473?w=400", C("Cleaning Essentials"), 70, "Surf Excel", null, "1kg"),
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
