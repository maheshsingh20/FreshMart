using CartService.Domain;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CartService.Infrastructure;

public interface ICartRepository
{
    Task<Cart?> GetAsync(Guid customerId, CancellationToken ct = default);
    Task SaveAsync(Cart cart, CancellationToken ct = default);
    Task DeleteAsync(Guid customerId, CancellationToken ct = default);
}

public class RedisCartRepository(IConnectionMultiplexer redis) : ICartRepository
{
    private static string Key(Guid id) => $"cart:{id}";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);
    // In-memory fallback when Redis is unavailable
    private static readonly Dictionary<Guid, string> _memoryStore = new();

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
