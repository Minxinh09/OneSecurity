using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public class CollectorCacheService : ICollectorCacheService
    {
        private readonly ConcurrentDictionary<string, CachedTokenDto> _tokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<long, CachedPolicyDto> _policies = new();
        private readonly ConcurrentDictionary<string, string> _agentIdToHostname = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _hostnameToCollector = new(StringComparer.OrdinalIgnoreCase);
        private int _configVersion = 0;
        private int _rulesVersion = 0;

        public bool IsTokenValid(string token, string hostname, long collectorId, out string error)
        {
            error = string.Empty;
            if (!_tokens.TryGetValue(token, out var cachedToken))
            {
                error = "Enrollment token not found.";
                return false;
            }

            if (cachedToken.Used || cachedToken.UsedCount >= cachedToken.MaxUses)
            {
                error = "Enrollment token has already been used.";
                return false;
            }

            if (DateTime.UtcNow > cachedToken.ExpireAt)
            {
                error = "Enrollment token has expired.";
                return false;
            }

            if (cachedToken.CollectorId != collectorId)
            {
                error = "Collector mismatch for this enrollment token.";
                return false;
            }

            if (cachedToken.Hostname.ToLower() != hostname.ToLower())
            {
                error = $"Token is locked to hostname '{cachedToken.Hostname}', but received '{hostname}'.";
                return false;
            }

            return true;
        }

        public bool HasAgent(string agentId)
        {
            return _agentIdToHostname.ContainsKey(agentId);
        }

        public string? GetHostname(string agentId)
        {
            if (_agentIdToHostname.TryGetValue(agentId, out var hostname)) return hostname;
            return null;
        }

        public bool IsHostnameAssigned(string hostname, long collectorId)
        {
            if (_hostnameToCollector.TryGetValue(hostname, out var assignedId))
            {
                return assignedId == collectorId;
            }
            return false;
        }

        public CachedPolicyDto? GetPolicy(long policyId)
        {
            if (_policies.TryGetValue(policyId, out var policy)) return policy;
            return null;
        }

        public void UpdateCache(CollectorSyncData syncData)
        {
            _configVersion = syncData.ConfigurationVersion;
            _rulesVersion = syncData.RulesVersion;

            // Update Tokens
            _tokens.Clear();
            foreach (var t in syncData.ValidTokens)
            {
                _tokens[t.Token] = t;
            }

            // Update Policies
            _policies.Clear();
            foreach (var p in syncData.Policies)
            {
                _policies[p.Id] = p;
            }

            // Update AgentId to Hostname map
            _agentIdToHostname.Clear();
            foreach (var kvp in syncData.AgentIdToHostnameMap)
            {
                _agentIdToHostname[kvp.Key] = kvp.Value;
            }

            // Update Hostname to Collector map
            _hostnameToCollector.Clear();
            foreach (var kvp in syncData.AssetCollectorMap)
            {
                _hostnameToCollector[kvp.Key] = kvp.Value;
            }
        }

        public int GetConfigVersion() => _configVersion;
        public int GetRulesVersion() => _rulesVersion;
    }
}
