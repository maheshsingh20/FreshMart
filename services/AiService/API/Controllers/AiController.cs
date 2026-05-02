using AiService.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.API.Controllers;

/// <summary>
/// Exposes two AI-powered endpoints for the FreshMart shopping assistant.
///
/// ENDPOINTS:
///   POST /api/v1/ai/chat   — conversational grocery assistant
///   POST /api/v1/ai/recipe — recipe generator with ingredient-to-product matching
///
/// DEPENDENCIES:
///   IGeminiService         — calls Google Gemini 2.5 Flash API
///   IProductCatalogService — fetches all products from ProductService
///
/// HOW THE AI KNOWS ABOUT PRODUCTS:
///   On every request, we fetch all products and inject them into the system prompt.
///   Gemini is instructed to ONLY recommend products from this list.
///   This means the AI always has up-to-date catalog info without any retraining.
///
/// ERROR RESPONSES:
///   400 — missing required input (message or dish)
///   503 — Gemini API key not configured
///   502 — Gemini API call failed or returned invalid JSON
/// </summary>
[ApiController]
[Route("api/v1/ai")]
public class AiController(IGeminiService gemini, IProductCatalogService catalog) : ControllerBase
{
    /// <summary>
    /// Conversational grocery shopping assistant.
    ///
    /// FLOW:
    ///   1. Fetch all products from ProductService (to build context)
    ///   2. Build a system prompt that tells Gemini:
    ///      - It is FreshMart's assistant
    ///      - Here are all available products (name, price, category, brand)
    ///      - Rules: only recommend these products, include prices, mention discounts
    ///   3. Build conversation history (previous turns + new message)
    ///   4. Call Gemini with temperature=0.7 (creative but grounded)
    ///   5. Find which product names appear in the reply → suggestedProducts
    ///   6. Return { reply, suggestedProducts }
    ///
    /// CONVERSATION HISTORY:
    ///   The frontend sends all previous turns with each request.
    ///   This gives Gemini context so it can answer follow-up questions.
    ///   Example: "What about dairy?" after "Show me breakfast items" works correctly.
    ///
    /// TEMPERATURE 0.7:
    ///   Balanced between creative (1.0) and deterministic (0.0).
    ///   Good for conversational responses — varied but not random.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Message is required" });
        if (!gemini.IsConfigured)
            return StatusCode(503, new { error = "AI service not configured." });

        // Step 1: Get all products to inject into the AI prompt
        var products = await catalog.GetAllAsync(ct);

        // Step 2: Build catalog text — each product on one line
        // Format: "- Amul Milk | Category: Dairy | Price: ₹68 (10% off) | Brand: Amul | ID: abc123"
        var catalogText = string.Join("\n", products.Select(p =>
            $"- {p.Name} | Category: {p.Category} | Price: ₹{p.Price}" +
            $"{(p.DiscountPercent > 0 ? $" ({p.DiscountPercent}% off)" : "")}" +
            $" | Brand: {p.Brand ?? "N/A"} | ID: {p.Id}"));

        // Step 3: System prompt — defines the AI's persona, context, and rules
        // This is sent as system_instruction (not part of the conversation history)
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

        // Step 4: Build conversation contents array
        // Gemini expects: [{ role: "user", parts: [{text}] }, { role: "model", parts: [{text}] }, ...]
        var contents = new List<object>();
        foreach (var turn in req.History ?? [])
            contents.Add(new { role = turn.Role, parts = new[] { new { text = turn.Text } } });
        contents.Add(new { role = "user", parts = new[] { new { text = req.Message } } });

        try
        {
            // Step 5: Call Gemini
            var text = await gemini.GenerateAsync(new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
            }, ct);

            // Step 6: Find products mentioned in the reply
            // Simple string matching — if the reply contains "Amul Milk", include it in suggestedProducts
            var mentioned = products
                .Where(p => text.Contains(p.Name, StringComparison.OrdinalIgnoreCase))
                .Select(p => new { p.Id, p.Name, p.Price, p.DiscountPercent })
                .Take(6)  // max 6 suggestions to keep UI clean
                .ToList();

            return Ok(new { reply = text, suggestedProducts = mentioned });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = "AI service error", detail = ex.Message });
        }
    }

    /// <summary>
    /// Recipe generator that matches ingredients to available store products.
    ///
    /// FLOW:
    ///   1. Fetch all products from ProductService
    ///   2. Build a strict JSON prompt asking Gemini for a structured recipe
    ///   3. Call Gemini with temperature=0.3 (low = more deterministic = valid JSON)
    ///   4. Clean the response (remove markdown code blocks if present)
    ///   5. Parse JSON into RecipeResponse object
    ///   6. For each ingredient, fuzzy-match against product catalog
    ///   7. Return recipe + matched ingredients + steps + metadata
    ///
    /// WHY TEMPERATURE 0.3 (lower than chat):
    ///   Recipe mode needs strict JSON output. Higher temperature makes Gemini
    ///   more creative but also more likely to add markdown, comments, or deviate
    ///   from the JSON schema. Lower temperature = more predictable, structured output.
    ///
    /// INGREDIENT MATCHING LOGIC:
    ///   For each ingredient (e.g. "butter"), we search products where:
    ///   - product.Name contains the search_keyword, OR
    ///   - product.Name contains the ingredient name, OR
    ///   - search_keyword contains the first word of product.Name
    ///   First match wins. If no match, product = null (ingredient not in our store).
    ///
    /// JSON CLEANING:
    ///   Gemini sometimes wraps JSON in markdown code blocks (```json ... ```).
    ///   We strip these before parsing to avoid JsonException.
    /// </summary>
    [HttpPost("recipe")]
    public async Task<IActionResult> Recipe([FromBody] RecipeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Dish))
            return BadRequest(new { error = "Dish is required" });
        if (!gemini.IsConfigured)
            return StatusCode(503, new { error = "AI service not configured." });

        var products = await catalog.GetAllAsync(ct);
        var servings = req.Servings > 0 ? req.Servings : 2;

        // JSON schema shown to Gemini as an example of the expected output format
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
            // Low temperature for structured JSON output
            var raw = await gemini.GenerateAsync(new
            {
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
            }, ct);

            // Clean markdown code blocks if Gemini wrapped the JSON
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```")) cleaned = cleaned[(cleaned.IndexOf('\n') + 1)..];
            if (cleaned.EndsWith("```")) cleaned = cleaned[..cleaned.LastIndexOf("```")].TrimEnd();

            RecipeResponse? recipe;
            try { recipe = JsonSerializer.Deserialize<RecipeResponse>(cleaned, JsonOpts); }
            catch { return StatusCode(502, new { error = "Failed to parse recipe JSON", raw }); }

            if (recipe?.Ingredients == null) return StatusCode(502, new { error = "Invalid recipe structure" });

            // Match each ingredient to a product in our catalog
            var matched = recipe.Ingredients.Select(ing =>
            {
                var keyword = ing.SearchKeyword?.ToLower() ?? ing.Name.ToLower();
                // Try three matching strategies in order:
                // 1. Product name contains the search keyword (e.g. "butter" → "Amul Butter")
                // 2. Product name contains the ingredient name
                // 3. Search keyword contains the first word of product name (e.g. "tomato sauce" → "Tomato")
                var match = products.FirstOrDefault(p =>
                    p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Contains(ing.Name, StringComparison.OrdinalIgnoreCase) ||
                    keyword.Contains(p.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));

                return new
                {
                    ing.Name, ing.Quantity, ing.Unit, ing.Category,
                    // null if ingredient not available in our store
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

/// <summary>Request body for the chat endpoint.</summary>
/// <param name="Message">The user's current message.</param>
/// <param name="History">All previous conversation turns. Null for first message.</param>
public record ChatRequest(string Message, List<ChatTurn>? History);

/// <summary>A single turn in the conversation history.</summary>
/// <param name="Role">"user" or "model" — matches Gemini's expected role values.</param>
/// <param name="Text">The message text for this turn.</param>
public record ChatTurn(string Role, string Text);

/// <summary>Request body for the recipe endpoint.</summary>
/// <param name="Dish">Name of the dish to generate a recipe for (e.g. "Butter Chicken").</param>
/// <param name="Servings">Number of servings. Defaults to 2 if not provided or invalid.</param>
public record RecipeRequest(string Dish, int Servings = 2);

/// <summary>Deserialized structure of Gemini's JSON recipe response.</summary>
public class RecipeResponse
{
    [JsonPropertyName("recipe")] public RecipeInfo? Recipe { get; set; }
    [JsonPropertyName("ingredients")] public List<RecipeIngredient>? Ingredients { get; set; }
    [JsonPropertyName("steps")] public List<string>? Steps { get; set; }
    [JsonPropertyName("meta")] public RecipeMeta? Meta { get; set; }
}

/// <summary>Basic recipe metadata returned by Gemini.</summary>
public class RecipeInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("servings")] public int Servings { get; set; }
    [JsonPropertyName("prep_time_minutes")] public int PrepTimeMinutes { get; set; }
    [JsonPropertyName("cook_time_minutes")] public int CookTimeMinutes { get; set; }
}

/// <summary>
/// A single ingredient in the recipe.
/// SearchKeyword is used for fuzzy-matching against the product catalog.
/// </summary>
public class RecipeIngredient
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("quantity")] public double Quantity { get; set; }
    [JsonPropertyName("unit")] public string Unit { get; set; } = "";
    /// <summary>Simple keyword for matching against product names (e.g. "butter" for "Amul Butter").</summary>
    [JsonPropertyName("search_keyword")] public string SearchKeyword { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
}

/// <summary>Recipe difficulty and dietary tags.</summary>
public class RecipeMeta
{
    [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}
