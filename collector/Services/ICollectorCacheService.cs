using System.Collections.Generic;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public interface ICollectorCacheService
    {
        bool IsTokenValid(string token, string hostname, long collectorId, out string error);
        bool HasAgent(string agentId);
        string? GetHostname(string agentId);
        bool IsHostnameAssigned(string hostname, long collectorId);
        CachedPolicyDto? GetPolicy(long policyId);
        void UpdateCache(CollectorSyncData syncData);
        int GetConfigVersion();
        int GetRulesVersion();
    }
}
