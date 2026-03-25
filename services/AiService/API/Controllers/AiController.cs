using AiService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.API.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiController(IGeminiService gemini, IProductCatalogService catalog) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Message is required" });
        if (!gemini.IsConfigured)
            return StatusCode(503, new { error = "AI service not configured." });

        var products = await catalog.GetAllAsync(ct);
        var catalogText = string.Join("\n", products.Select(p =>
            $"- {p.Name} | Category: {p.Category} | Price: ₹{p.Price}" +
            $"{(p.DiscountPercent > 0 ? $" ({p.DiscountPercent}% off)" : "")}" +
            $" | Brand: {p.Brand ?? "N/A"} | ID: {p.Id}"));

        var systemPrompt = $"""
            You are FreshMart's friendly AI shopping assistant. FreshMart is an online grocery store in India.
            Help customers find products, suggest items for recipes, and answer grocery-related questions.

            AVAILABLE PRODUCTS:
            {catalogText}

            RULES:
            - Only recommend products from the catalog above.
            - Always include product name and price in Indian Rupees (₹).
            - If asked for a recipe, list ingredients AND match them to available products.
            - Keep responses concise and friendly.
            - Mention discounts when applicable.
            - Respond in the same language the user writes in.
            """;

        var contents = new List<object>();
        foreach (var turn in req.History ?? [])
            contents.Add(new { role = turn.Role, parts = new[] { new { text = turn.Text } } });
        contents.Add(new { role = "user", parts = new[] { new { text = req.Message } } });

        try
        {
            var text = await gemini.GenerateAsync(new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
            }, ct);

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

    [HttpPost("recipe")]
    public async Task<IActionResult> Recipe([FromBody] RecipeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Dish))
            return BadRequest(new { error = "Dish is required" });
        if (!gemini.IsConfigured)
            return StatusCode(503, new { error = "AI service not configured." });

        var products = await catalog.GetAllAsync(ct);
        var servings = req.Servings > 0 ? req.Servings : 2;

        var jsonSchema = """{"recipe":{"name":"string","servings":0,"prep_time_minutes":0,"cook_time_minutes":0},"ingredients":[{"name":"string","quantity":0,"unit":"g|ml|pieces|tbsp|tsp","search_keyword":"string","category":"vegetable|dairy|grain|spice|meat|other"}],"steps":["step1"],"meta":{"difficulty":"easy|medium|hard","tags":["veg"]}}""";

        var prompt = $"""
            You are an AI cooking and grocery assistant for an Indian online grocery store.
            User request: "{req.Dish}" for {servings} servings.
            Generate a realistic recipe. Extract a COMPLETE ingredient list with exact quantities.
            Use ingredient names that match grocery store products (simple, common, lowercase singular).
            Provide a "search_keyword" for each ingredient to match grocery DB.
            Output STRICT JSON ONLY — no markdown, no explanation:
            {jsonSchema}
            """;

        try
        {
            var raw = await gemini.GenerateAsync(new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
            }, ct);

            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned[(cleaned.IndexOf('\n') + 1)..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..cleaned.LastIndexOf("```")].TrimEnd();

            RecipeResponse? recipe;
            try { recipe = JsonSerializer.Deserialize<RecipeResponse>(cleaned, JsonOpts); }
            catch { return StatusCode(502, new { error = "Failed to parse recipe JSON", raw }); }

            if (recipe?.Ingredients == null) return StatusCode(502, new { error = "Invalid recipe structure" });

            var matched = recipe.Ingredients.Select(ing =>
            {
                var keyword = ing.SearchKeyword?.ToLower() ?? ing.Name.ToLower();
                var match = products.FirstOrDefault(p =>
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Contains(ing.Name, StringComparison.OrdinalIgnoreCase) ||
                    keyword.Contains(p.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

                return new
                {
                    ing.Name, ing.Quantity, ing.Unit, ing.Category,
                    product = match is null ? null : new { match.Id, match.Name, match.Price, match.DiscountPercent, match.Unit }
                };
            });

            return Ok(new { recipe = recipe.Recipe, ingredients = matched, steps = recipe.Steps, meta = recipe.Meta });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "AI service error", detail = ex.Message });
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
}

public record ChatRequest(string Message, List<ChatTurn>? History);
public record ChatTurn(string Role, string Text);
public record RecipeRequest(string Dish, int Servings = 2);

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
