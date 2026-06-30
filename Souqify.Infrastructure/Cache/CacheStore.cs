
using Souqify.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Souqify.Infrastructure.Cache
{
    public class CacheStore : ICacheStore
    {

        private readonly IDatabase _database;

        public CacheStore(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<T> GetDataAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value);
        }

        public async Task<bool> RemoveDataAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public Task<bool> SetDataAsync<T>(string key, T value, TimeSpan expireDate)
        {
            var serialisedValue = JsonSerializer.Serialize(value);

            return _database.StringSetAsync(key, serialisedValue,expireDate);
        }
    }
}
