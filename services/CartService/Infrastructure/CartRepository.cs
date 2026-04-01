using CartService.Domain;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CartService.Infrastructure;

/// <summary>
/// Persistence contract for shopping carts.
/// Implementations must be thread-safe as multiple requests for the same customer can arrive concurrently.
/// </summary>
public interface ICartRepository
{
    /// <summary>Retrieves the cart for a customer, or <c>null</c> if none exists.</summary>
    Task<Cart?> GetAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Persists (creates or overwrites) the cart for a customer.</summary>
    Task SaveAsync(Cart cart, CancellationToken ct = default);

    /// <summary>Deletes the cart for a customer (called after a successful order).</summary>
    Task DeleteAsync(Guid customerId, CancellationToken ct = default);
}

/// <summary>
/// Redis-backed cart repository. Carts are stored as JSON strings with a 7-day TTL.
/// Falls back to an in-process dictionary when Redis is unavailable so the cart
/// still works during Redis restarts (data is lost on pod restart in that case).
/// </summary>
public class RedisCartRepository(IConnectionMultiplexer redis) : ICartRepository
{
    private static string Key(Guid id) => $"cart:{id}";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    /// <summary>In-memory fallback used when Redis is unavailable.</summary>
    private static readonly Dictionary<Guid, string> _memoryStore = new();

    /// <inheritdoc/>
    public async Task<Cart?> GetAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(Key(customerId));
            return value.HasValue ? JsonConvert.DeserializeObject<Cart>(value!) : null;
        }
        catch
        {
            return _memoryStore.TryGetValue(customerId, out var json)
                ? JsonConvert.DeserializeObject<Cart>(json) : null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Cart cart, CancellationToken ct = default)
    {
        var json = JsonConvert.SerializeObject(cart);
        try
        {
            var db = redis.GetDatabase();
            await db.StringSetAsync(Key(cart.CustomerId), json, Ttl);
        }
        catch
        {
            _memoryStore[cart.CustomerId] = json;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(Key(customerId));
        }
        catch { /* ignore */ }
        _memoryStore.Remove(customerId);
    }
}
