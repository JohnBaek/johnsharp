using System.Collections.Concurrent;
using JohnIsDev.Core.Cache.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JohnIsDev.Core.Cache.Implements;

/// <summary>
/// InMemory Cache Manager (Should replace integrated server)
/// </summary>
public class CacheManagerForInMemoryProvider(
    IMemoryCache memoryCache 
    ) : ICacheManager
{
    /// <summary>
    /// a Locking
    /// </summary>
    private readonly AsyncKeyedLock<string> _asyncLock = new ();

    /// <summary>
    /// Get Cached Data
    /// </summary>
    /// <param name="key">Key</param>
    /// <typeparam name="T">Type</typeparam>
    /// <returns>T</returns>
    public T? Get<T>(string key) => memoryCache.Get<T>(key);

    /// <summary>
    /// Get Cached Data Async
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T?> GetAsync<T>(string key) where T : class 
        => await Task.FromResult(memoryCache.Get<T>(key));
    

    /// <summary>
    /// Set Cached Data
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="value">T value</param>
    /// <param name="expirationTime">expiredTime</param>
    public void Set<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        // Has Expirations?
        MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
        if (expirationTime.HasValue)
            options.SetAbsoluteExpiration(expirationTime.Value);

        // Store Cached
        memoryCache.Set(key, value, options);
    }

    /// <summary>
    /// Set Cached Data Async
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expirationTime"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        // Has Expirations?
        MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
        if (expirationTime.HasValue)
            options.SetAbsoluteExpiration(expirationTime.Value);

        // Store Cached
        memoryCache.Set(key, value, options);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Remove Cached Data
    /// </summary>
    /// <param name="key">key</param>
    public void Remove(string key)
    {
        memoryCache.Remove(key);
    }

    /// <summary>
    /// Remove Cached Data Async
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public async Task RemoveAsync(string key)
    {
        memoryCache.Remove(key);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Contains Key?
    /// </summary>
    /// <param name="key">key</param>
    /// <returns></returns>
    public bool ContainsKey(string key)
    {
        return memoryCache.TryGetValue(key, out _);
    }

    /// <summary>
    /// GetOrSetCache Async
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <param name="cacheDuration"></param>
    /// <param name="callback"></param>
    public async Task<List<T>> GetOrSetCacheAsync<T>(string cacheKey, TimeSpan cacheDuration, Func<Task<List<T>>> callback)
        => await GetOrSetCacheWithAsync(cacheKey, cacheDuration, callback);

    /// <summary>
    /// Retrieves a cached item by its key. If the item is not in the cache, it executes the provided callback to load the data,
    /// adds the data to the cache for a specified duration, and then returns the data.
    /// </summary>
    /// <param name="cacheKey">The unique key used for identifying the cached item.</param>
    /// <param name="cacheDuration">The time duration for which the item will be stored in the cache.</param>
    /// <param name="callback">The asynchronous function to execute if the item does not exist in the cache.</param>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <returns>The cached item, either retrieved from the cache or obtained by executing the callback.</returns>
    public async Task<T> GetOrSetCacheWithAsync<T>(string cacheKey, TimeSpan cacheDuration, Func<Task<T>> callback)
    {
        // Get a data from Cache
        var cachedData = Get<T>(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        // Locking entry point for Prevent a Race problem
        using (await _asyncLock.LockAsync(cacheKey))
        {
            // 이중 확인 잠금 패턴 (Double-Check Locking)
            cachedData = Get<T>(cacheKey);
            if (cachedData != null)
            {
                return cachedData;
            }

            // 데이터 로드
            var data = await callback();

            // 데이터 캐시 설정 (빈 리스트도 캐시)
            Set(cacheKey, data, cacheDuration);

            return data;
        }
    }

    /// <summary>
    /// Retrieves the cached data associated with the given cache key or sets it using the specified callback if not present.
    /// </summary>
    /// <param name="cacheKey">The key associated with the cache entry.</param>
    /// <param name="cacheDuration">The duration for which the data should be cached.</param>
    /// <param name="callback">The function to execute to generate the data if it is not already cached.</param>
    /// <typeparam name="T">The type of the cached data.</typeparam>
    /// <returns>The cached data of type T.</returns>
    public T GetOrSetCacheWith<T>(string cacheKey, TimeSpan cacheDuration, Func<T> callback)
    {
        // Get a data from Cache
        var cachedData = Get<T>(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        if (cachedData != null)
        {
            return cachedData;
        }

        // 데이터 로드
        var data = callback();

        // 데이터 캐시 설정 (빈 리스트도 캐시)
        Set(cacheKey, data, cacheDuration);

        return data;
    }
}

/// <summary>
///
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class AsyncKeyedLock<TKey> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> LockAsync(TKey key)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        return new AsyncLockReleaser<TKey>(semaphore, key, _locks);
    }

    private class AsyncLockReleaser<T>(SemaphoreSlim semaphore, T key, ConcurrentDictionary<T, SemaphoreSlim> locks)
        : IDisposable where T : notnull
    {
        public void Dispose()
        {
            semaphore.Release();
            locks.TryRemove(key, out _);
        }
    }
}