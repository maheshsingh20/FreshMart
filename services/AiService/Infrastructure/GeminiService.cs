using AiService.Application.Services;
using System.Text;
using System.Text.Json;

namespace AiService.Infrastructure;

public class GeminiService(IHttpClientFactory httpFactory, IConfiguration config) : IGeminiService
{
    private readonly string _apiKey = config["Gemini:ApiKey"] ?? "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_GEMINI_API_KEY_HERE";

    public async Task<string> GenerateAsync(object payload, CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
        var resp = await client.PostAsync(url,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) throw new Exception($"Gemini error: {body}");
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";
    }
}
