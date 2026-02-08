using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Petek.Server.Services;

/// <summary>
/// SignalR bağlantı yöneticisi (Redis destekli)
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections;
    private readonly ILogger<ConnectionManager> _logger;
    private const string RedisKeyPrefix = "Petek:connections:";

    public ConnectionManager(IConfiguration configuration, ILogger<ConnectionManager> logger)
    {
        _logger = logger;
        _connections = new ConcurrentDictionary<Guid, HashSet<string>>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(redisConnection);
                _logger.LogInformation("Redis connection established for connection management");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to Redis, using in-memory connection management");
            }
        }
    }

    public async Task AddConnectionAsync(Guid userId, string connectionId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.SetAddAsync($"{RedisKeyPrefix}{userId}", connectionId);
        }
        else
        {
            _connections.AddOrUpdate(
                userId,
                _ => new HashSet<string> { connectionId },
                (_, set) =>
                {
                    lock (set) { set.Add(connectionId); }
                    return set;
                });
        }
    }

    public async Task RemoveConnectionAsync(Guid userId, string connectionId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.SetRemoveAsync($"{RedisKeyPrefix}{userId}", connectionId);
        }
        else
        {
            if (_connections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                    }
                }
            }
        }
    }

    public async Task<bool> HasConnectionsAsync(Guid userId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var length = await db.SetLengthAsync($"{RedisKeyPrefix}{userId}");
            return length > 0;
        }
        else
        {
            return _connections.TryGetValue(userId, out var set) && set.Count > 0;
        }
    }

    public async Task<IEnumerable<string>> GetConnectionsAsync(Guid userId)
    {
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var values = await db.SetMembersAsync($"{RedisKeyPrefix}{userId}");
            return values.Select(v => v.ToString());
        }
        else
        {
            if (_connections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    return set.ToList();
                }
            }
            return Enumerable.Empty<string>();
        }
    }
}
