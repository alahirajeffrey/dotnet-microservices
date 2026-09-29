using System.Text.Json;
using StackExchange.Redis;

namespace ProductService.Services;

public class RedisService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;

    public RedisService(IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        _connection = ConnectionMultiplexer.Connect(connectionString);
        _database = _connection.GetDatabase();
    }

    public IConnectionMultiplexer Connection => _connection;

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);

        if (!value.HasValue)
        {
            return default;
        }

        var json = value.ToString();
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);

        if (expiry.HasValue)
        {
            await _database.StringSetAsync(key, json, expiry.Value);
            return;
        }

        await _database.StringSetAsync(key, json);
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }
}