namespace AiService.Application.Services;

public interface IGeminiService
{
    Task<string> GenerateAsync(object payload, CancellationToken ct = default);
    bool IsConfigured { get; }
}
