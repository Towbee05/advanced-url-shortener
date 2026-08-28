using StackExchange.Redis;

namespace UrlShortener.Services;

public interface ICacheService
{
    public Task<string?> GetStringAsync(string key);
    public Task<bool> SetStringAsync(string key, string value, TimeSpan? exp = null);
    public Task<string?> GetAndDeleteAsync(string key);
    public Task<bool> DeleteStringAsync(string key);
}

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        var cache = this._redis.GetDatabase();
        return await cache.StringGetAsync(key);
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? exp = null)
    {
        var cache = this._redis.GetDatabase();
        if (exp.HasValue)
        {
            return await cache.StringSetAsync(key, value, exp.Value);
        }
        return await cache.StringSetAsync(key, value);
    }

    public async Task<string?> GetAndDeleteAsync(string key)
    {
        var cache = this._redis.GetDatabase();
        return await cache.StringGetDeleteAsync(key);
    }

    public async Task<bool> DeleteStringAsync(string key)
    {
        var cache = this._redis.GetDatabase();
        return await cache.KeyDeleteAsync(key);
    }
}
