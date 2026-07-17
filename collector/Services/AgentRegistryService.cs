using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OneSecurity.Collector.Services
{
    public class AgentRegistryEntry
    {
        public required string AgentId { get; set; }
        public required string Hostname { get; set; }
        public required string IpAddress { get; set; }
        public required string OsInfo { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string Status { get; set; } = "online";
    }

    public interface IAgentRegistryService
    {
        void RegisterOrUpdate(string agentId, string hostname, string ipAddress, string osInfo);
        void UpdateLastSeen(string agentId);
        IEnumerable<AgentRegistryEntry> GetAllEntries();
        int GetActiveCount();
    }

    public class AgentRegistryService : IAgentRegistryService
    {
        private readonly ConcurrentDictionary<string, AgentRegistryEntry> _registry = new ConcurrentDictionary<string, AgentRegistryEntry>();

        public void RegisterOrUpdate(string agentId, string hostname, string ipAddress, string osInfo)
        {
            var now = DateTime.UtcNow;
            _registry.AddOrUpdate(agentId,
                new AgentRegistryEntry
                {
                    AgentId = agentId,
                    Hostname = hostname,
                    IpAddress = ipAddress,
                    OsInfo = osInfo,
                    RegisteredAt = now,
                    LastSeenAt = now,
                    Status = "online"
                },
                (key, existing) =>
                {
                    existing.Hostname = hostname;
                    existing.IpAddress = ipAddress;
                    existing.OsInfo = osInfo;
                    existing.LastSeenAt = now;
                    existing.Status = "online";
                    return existing;
                });
        }

        public void UpdateLastSeen(string agentId)
        {
            var now = DateTime.UtcNow;
            if (_registry.TryGetValue(agentId, out var entry))
            {
                entry.LastSeenAt = now;
                entry.Status = "online";
            }
            else
            {
                _registry.TryAdd(agentId, new AgentRegistryEntry
                {
                    AgentId = agentId,
                    Hostname = "Unknown",
                    IpAddress = "Unknown",
                    OsInfo = "Unknown",
                    RegisteredAt = now,
                    LastSeenAt = now,
                    Status = "online"
                });
            }
        }

        public IEnumerable<AgentRegistryEntry> GetAllEntries()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _registry)
            {
                if ((now - kvp.Value.LastSeenAt).TotalSeconds > 30)
                {
                    kvp.Value.Status = "offline";
                }
            }
            return _registry.Values.ToList();
        }

        public int GetActiveCount()
        {
            var now = DateTime.UtcNow;
            return _registry.Values.Count(v => (now - v.LastSeenAt).TotalSeconds <= 30);
        }
    }
}
