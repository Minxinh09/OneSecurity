using System;
using System.Collections.Generic;

namespace OneSecurity.Collector.DTOs
{
    public class CollectorSyncData
    {
        public bool IsUpToDate { get; set; } = false;
        public int ConfigurationVersion { get; set; }
        public int RulesVersion { get; set; }
        
        public List<string> RegisteredAssetHostnames { get; set; } = new();
        public Dictionary<string, string> AgentIdToHostnameMap { get; set; } = new();
        public Dictionary<string, long> AssetCollectorMap { get; set; } = new();
        
        public List<CachedTokenDto> ValidTokens { get; set; } = new();
        public List<CachedPolicyDto> Policies { get; set; } = new();
    }

    public class CachedTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public long AssetId { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public long CollectorId { get; set; }
        public long PolicyId { get; set; }
        public DateTime ExpireAt { get; set; }
        public bool Used { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
    }

    public class CachedPolicyDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int HeartbeatInterval { get; set; }
        public int MetricsInterval { get; set; }
        public string EnabledLogs { get; set; } = string.Empty;
        public bool ResponseEnabled { get; set; }
    }
}
