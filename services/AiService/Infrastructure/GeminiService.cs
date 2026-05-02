using AiService.Application.Services;
using System.Text;
using System.Text.Json;

namespace AiService.Infrastructure;

/// <summary>
/// Concrete implementation of <see cref="IGeminiService"/>.
/// Makes HTTP calls to the Google Gemini 2.5 Flash REST API.
///
/// HOW IT WORKS:
///   1. Reads the API key from configuration (Gemini:ApiKey)
///   2. Serializes the payload (system prompt + conversation) to JSON
///   3. POSTs to the Gemini generateContent endpoint
///   4. Parses the nested JSON response to extract the generated text
///
/// GEMINI RESPONSE STRUCTURE:
///   {
///     "candidates": [{
///       "content": {
///         "parts": [{ "text": "the generated text here" }]
///       }
///     }]
///   }
///   We navigate: candidates[0] → content → parts[0] → text
///
/// ERROR HANDLING:
///   Throws Exception with the raw error body if Gemini returns non-2xx.
///   The controller catches this and returns 502 Bad Gateway.
/// </summary>
public class GeminiService(IHttpClientFactory httpFactory, IConfiguration config) : IGeminiService
{
    private readonly string _apiKey = config["Gemini:ApiKey"] ?? "";

    /// <summary>
    /// Returns true only when a real API key is configured.
    /// Prevents crashes when the key is missing or still set to the placeholder.
    /// The controller checks this and returns 503 instead of attempting a doomed API call.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_GEMINI_API_KEY_HERE";

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(object payload, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        // Gemini 2.5 Flash — fast and cost-effective for conversational AI
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
        var resp = await client.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) throw new Exception($"Gemini error: {body}");
        // Navigate the nested response structure to extract the generated text
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";
    }
}
