using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiController(AppDbContext db, IConfiguration config, IHttpClientFactory httpFactory) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ── Shared helper ────────────────────────────────────────────────────────
    private async Task<(string apiKey, List<CatalogProduct> products)> GetContextAsync()
    {
        var apiKey = config["Gemini:ApiKey"] ?? "";
        var products = await db.Products
            .Where(p => p.IsActive && p.StockQuantity > 0)
            .Include(p => p.Category)
            .Select(p => new CatalogProduct
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Price = p.DiscountPercent > 0 ? Math.Round(p.Price * (1 - p.DiscountPercent / 100m), 2) : p.Price,
                OriginalPrice = p.Price,
                DiscountPercent = p.DiscountPercent,
                Category = p.Category.Name,
                Brand = p.Brand ?? "",
                Unit = p.Unit ?? "",
                AverageRating = p.AverageRating
            })
            .ToListAsync();
        return (apiKey, products);
    }

    private async Task<string> CallGeminiAsync(object payload)
    {
        var apiKey = config["Gemini:ApiKey"]!;
        var client = httpFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
        var json = JsonSerializer.Serialize(payload);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, httpContent);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Gemini error: {body}");
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";
    }

    private IActionResult CheckApiKey(string key) =>
        string.IsNullOrWhiteSpace(key) || key == "YOUR_GEMINI_API_KEY_HERE"
            ? StatusCode(503, new { error = "AI service not configured. Please set Gemini:ApiKey in appsettings.json" })
            : null!;

    // ── POST /api/v1/ai/chat  (general assistant) ────────────────────────────
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Message is required" });

        var (apiKey, products) = await GetContextAsync();
        if (CheckApiKey(apiKey) is { } err) return err;

        var catalog = string.Join("\n", products.Select(p =>
            $"- {p.Name} | Category: {p.Category} | Price: \u20b9{p.Price}" +
            $"{(p.DiscountPercent > 0 ? $" ({p.DiscountPercent}% off, was \u20b9{p.OriginalPrice})" : "")}" +
            $" | Brand: {(string.IsNullOrEmpty(p.Brand) ? "N/A" : p.Brand)} | Unit: {(string.IsNullOrEmpty(p.Unit) ? "N/A" : p.Unit)} | Rating: {p.AverageRating:F1}/5 | ID: {p.Id}"));

        var systemPrompt = $"""
            You are FreshMart's friendly AI shopping assistant. FreshMart is an online grocery store in India.
            Your job is to help customers find products, suggest items for recipes, and answer grocery-related questions.

            AVAILABLE PRODUCTS IN OUR STORE:
            {catalog}

            RULES:
            - Only recommend products that exist in the catalog above.
            - When suggesting products, always include the product name and price in Indian Rupees (\u20b9).
            - If asked for a recipe or meal, list the ingredients needed AND match them to available products.
            - If a product isn't available, say so honestly.
            - Keep responses concise, friendly, and helpful.
            - Always mention if a product is on sale/discounted.
            - Respond in the same language the user writes in.
            - Do NOT make up products that aren't in the catalog.
            """;

        var geminiContents = new List<object>();
        foreach (var turn in req.History ?? [])
            geminiContents.Add(new { role = turn.Role, parts = new[] { new { text = turn.Text } } });
        geminiContents.Add(new { role = "user", parts = new[] { new { text = req.Message } } });

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = geminiContents,
            generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
        };

        try
        {
            var text = await CallGeminiAsync(payload);
            var mentioned = products
                .Where(p => text.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
                .Select(p => new { p.Id, p.Name, p.Price, p.DiscountPercent })
                .Take(6).ToList();
            return Ok(new { reply = text, suggestedProducts = mentioned });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "AI service error", detail = ex.Message });
        }
    }

    // ── POST /api/v1/ai/recipe  (structured recipe + matched products) ────────
    [HttpPost("recipe")]
    public async Task<IActionResult> Recipe([FromBody] RecipeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Dish))
            return BadRequest(new { error = "Dish is required" });

        var (apiKey, products) = await GetContextAsync();
        if (CheckApiKey(apiKey) is { } err) return err;

        var servings = req.Servings > 0 ? req.Servings : 2;

        var jsonSchema = """{"recipe":{"name":"string","servings":0,"prep_time_minutes":0,"cook_time_minutes":0},"ingredients":[{"name":"string","quantity":0,"unit":"g|ml|pieces|tbsp|tsp","search_keyword":"string","category":"vegetable|dairy|grain|spice|meat|other"}],"steps":["step1"],"meta":{"difficulty":"easy|medium|hard","tags":["veg"]}}""";

        var structuredPrompt = $"""
            You are an AI cooking and grocery assistant integrated with an online grocery store.
            User request: "{req.Dish}"
            Context:
            - Number of servings: {servings}
            - User location: India
            - Goal: Convert cooking intent into a grocery-ready structured response

            Instructions:
            1. Identify the dish (or suggest the most relevant one).
            2. Generate a realistic recipe for the given servings.
            3. Extract a COMPLETE ingredient list with exact quantities and standard units (g, ml, pieces, tbsp, tsp).
            4. Avoid vague terms like "some", "as needed", "to taste" — use exact numbers.
            5. Expand generic ingredients: "spices" → turmeric, red chili powder, garam masala, etc.
            6. Use ingredient names that match grocery store products (simple + common).
            7. Ensure quantities are practical and usable for shopping.
            8. Keep ingredient names lowercase and singular (e.g., "onion", not "onions").
            9. Do NOT include optional ingredients unless essential.
            10. For each ingredient, provide a "search_keyword" (simple word to match grocery DB).
            11. Estimate "category" for each ingredient.

            Output STRICT JSON ONLY — no markdown, no explanation, no code fences:
            {jsonSchema}
            Replace the placeholder values with real data. The "servings" field must be {servings}.
            """;

        var payload = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = structuredPrompt } } } },
            generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
        };

        try
        {
            var raw = await CallGeminiAsync(payload);

            // Strip markdown fences if Gemini wraps in ```json
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned[(cleaned.IndexOf('\n') + 1)..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..cleaned.LastIndexOf("```")].TrimEnd();

            RecipeResponse? recipe;
            try { recipe = JsonSerializer.Deserialize<RecipeResponse>(cleaned, JsonOpts); }
            catch { return StatusCode(502, new { error = "Failed to parse recipe JSON", raw }); }

            if (recipe?.Ingredients == null) return StatusCode(502, new { error = "Invalid recipe structure", raw });

            // Match each ingredient to real products in DB
            var matched = recipe.Ingredients.Select(ing =>
            {
                var keyword = ing.SearchKeyword?.ToLower() ?? ing.Name.ToLower();
                var match = products.FirstOrDefault(p =>
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Contains(ing.Name, StringComparison.OrdinalIgnoreCase) ||
                    keyword.Contains(p.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

                return new MatchedIngredient
                {
                    Name = ing.Name,
                    Quantity = ing.Quantity,
                    Unit = ing.Unit,
                    Category = ing.Category,
                    Product = match == null ? null : new MatchedProduct
                    {
                        Id = match.Id,
                        Name = match.Name,
                        Price = match.Price,
                        DiscountPercent = match.DiscountPercent,
                        Unit = match.Unit
                    }
                };
            }).ToList();

            return Ok(new { recipe = recipe.Recipe, ingredients = matched, steps = recipe.Steps, meta = recipe.Meta });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "AI service error", detail = ex.Message });
        }
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record ChatRequest(string Message, List<ChatTurn>? History);
public record ChatTurn(string Role, string Text);
public record RecipeRequest(string Dish, int Servings = 2);

public class CatalogProduct
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Unit { get; set; } = "";
    public double AverageRating { get; set; }
}

public class RecipeResponse
{
    [JsonPropertyName("recipe")] public RecipeInfo? Recipe { get; set; }
    [JsonPropertyName("ingredients")] public List<RecipeIngredient>? Ingredients { get; set; }
    [JsonPropertyName("steps")] public List<string>? Steps { get; set; }
    [JsonPropertyName("meta")] public RecipeMeta? Meta { get; set; }
}

public class RecipeInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("servings")] public int Servings { get; set; }
    [JsonPropertyName("prep_time_minutes")] public int PrepTimeMinutes { get; set; }
    [JsonPropertyName("cook_time_minutes")] public int CookTimeMinutes { get; set; }
}

public class RecipeIngredient
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("quantity")] public double Quantity { get; set; }
    [JsonPropertyName("unit")] public string Unit { get; set; } = "";
    [JsonPropertyName("search_keyword")] public string SearchKeyword { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
}

public class RecipeMeta
{
    [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}

public class MatchedIngredient
{
    public string Name { get; set; } = "";
    public double Quantity { get; set; }
    public string Unit { get; set; } = "";
    public string Category { get; set; } = "";
    public MatchedProduct? Product { get; set; }
}

public class MatchedProduct
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Unit { get; set; } = "";
}
