using System;
using Microsoft.Extensions.Caching.Memory;

namespace OneSecurity.Collector.Services
{
    public interface IDeduplicationService
    {
        bool IsDuplicate(string? messageId, string agentId);
    }

    public class DeduplicationService : IDeduplicationService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(30);

        public DeduplicationService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsDuplicate(string? messageId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return false;
            }

            string cacheKey = $"dup:{agentId}:{messageId}";

            if (_cache.TryGetValue(cacheKey, out _))
            {
                return true;
            }

            _cache.Set(cacheKey, true, CacheExpiry);
            return false;
        }
    }
}
