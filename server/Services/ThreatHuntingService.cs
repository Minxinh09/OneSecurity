using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public class ThreatHuntingService : IThreatHuntingService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<Type> _providerTypes = new()
        {
            typeof(HuntingProviders.AgentThreatSearchProvider),
            typeof(HuntingProviders.AlertThreatSearchProvider),
            typeof(HuntingProviders.IncidentThreatSearchProvider),
            typeof(HuntingProviders.AuditLogThreatSearchProvider),
            typeof(HuntingProviders.SecurityEventThreatSearchProvider)
        };

        public ThreatHuntingService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<ThreatSearchResultDto> SearchAsync(ThreatSearchRequest request)
        {
            var tasks = _providerTypes.Select(providerType => Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var provider = (IThreatSearchProvider)scope.ServiceProvider.GetRequiredService(providerType);
                return await provider.SearchAsync(request);
            })).ToList();

            var resultsArray = await Task.WhenAll(tasks);

            var merged = resultsArray
                .SelectMany(x => x)
                .OrderByDescending(x => x.Timestamp)
                .Take(100) // Capped total returned results
                .ToList();

            return new ThreatSearchResultDto { Items = merged };
        }
    }
}
