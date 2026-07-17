using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace OneSecurity.Server.Services
{
    public class RegexCache
    {
        private readonly ConcurrentDictionary<string, Regex> _cache = new();

        public Regex GetOrAdd(string pattern)
        {
            return _cache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
