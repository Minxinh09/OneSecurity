using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class RuleCacheService : IRuleCacheService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RuleCacheService> _logger;
        private List<AlertRule> _cache = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _isInitialized = false;

        public RuleCacheService(IServiceScopeFactory scopeFactory, ILogger<RuleCacheService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<List<AlertRule>> GetActiveRulesAsync()
        {
            if (!_isInitialized)
            {
                await EnsureInitializedAsync();
            }

            await _lock.WaitAsync();
            try
            {
                return _cache.ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ReloadAsync()
        {
            _logger.LogInformation("Reloading Alert Rules Cache...");
            await _lock.WaitAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ruleRepository = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
                var activeRules = await ruleRepository.GetActiveRulesAsync();
                
                _cache = activeRules
                    .OrderBy(r => r.Priority)
                    .ToList();

                _isInitialized = true;
                _logger.LogInformation("Successfully cached {Count} active rules sorted by priority.", _cache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reloading Alert Rules Cache.");
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_isInitialized) return;
                
                using var scope = _scopeFactory.CreateScope();
                var ruleRepository = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
                var activeRules = await ruleRepository.GetActiveRulesAsync();
                
                _cache = activeRules
                    .OrderBy(r => r.Priority)
                    .ToList();

                _isInitialized = true;
                _logger.LogInformation("Alert Rules Cache initialized with {Count} rules.", _cache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial loading of Alert Rules Cache.");
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
