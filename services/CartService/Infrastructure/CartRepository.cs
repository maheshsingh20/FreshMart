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

    public async Task<Cart?> GetAsync(Guid customerId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(Key(customerId));
        return value.HasValue ? JsonConvert.DeserializeObject<Cart>(value!) : null;
    }

    public async Task SaveAsync(Cart cart, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(Key(cart.CustomerId),
            JsonConvert.SerializeObject(cart), Ttl);
    }

    public async Task DeleteAsync(Guid customerId, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(Key(customerId));
    }
}
