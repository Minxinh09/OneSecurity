using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class AuditService : IAuditService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            ILogger<AuditService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            AuditResourceType resourceType,
            string? entityId = null,
            string? description = null,
            bool success = true,
            int statusCode = 200,
            AuditSeverity severity = AuditSeverity.Information,
            string? userNameOverride = null,
            string? roleOverride = null)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                
                string? userId = null;
                string? userName = userNameOverride;
                string? roles = roleOverride;
                string? ipAddress = null;
                string? userAgent = null;
                string? correlationId = null;

                if (context != null)
                {
                    userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    
                    if (string.IsNullOrEmpty(userName))
                    {
                        userName = context.User?.FindFirstValue(ClaimTypes.Name) ?? context.User?.Identity?.Name;
                    }

                    if (string.IsNullOrEmpty(roles))
                    {
                        var rolesList = context.User?.FindAll(ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();
                        
                        if (rolesList != null && rolesList.Count > 0)
                        {
                            roles = string.Join(",", rolesList);
                        }
                    }

                    ipAddress = context.Connection?.RemoteIpAddress?.ToString();
                    userAgent = context.Request?.Headers["User-Agent"].ToString();

                    // Correlation ID priorities:
                    // 1. X-Correlation-ID
                    // 2. X-Request-ID
                    // 3. TraceIdentifier
                    if (context.Request?.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader) == true)
                    {
                        correlationId = correlationHeader.ToString();
                    }
                    else if (context.Request?.Headers.TryGetValue("X-Request-ID", out var requestHeader) == true)
                    {
                        correlationId = requestHeader.ToString();
                    }
                    else
                    {
                        correlationId = context.TraceIdentifier;
                    }
                }

                // Apply defaults
                userName ??= "System/Anonymous";
                roles ??= "Unknown";

                // Mask sensitive information in description
                string? maskedDescription = MaskSensitiveData(description);

                var auditLog = new AuditLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    UserId = userId,
                    UserName = userName,
                    Roles = roles,
                    Action = action,
                    ResourceType = resourceType,
                    EntityId = entityId,
                    Description = maskedDescription,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Success = success,
                    StatusCode = statusCode,
                    Severity = severity,
                    CorrelationId = correlationId
                };

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
                await repository.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                // Fail silently, but log locally
                _logger.LogWarning(ex, "Failed to write audit log for action {Action} on resource {ResourceType}", action, resourceType);
            }
        }

        private string? MaskSensitiveData(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var result = input;

            // Regex 1: Matches key-value assignments (e.g. password=abc, token: xyz, etc.)
            // Keys: password, secret, token, jwt, telegramchatid, api key, connection string, clientsecret, accesskey, refreshtoken
            var keyValuePattern = @"(?i)(password|secret|token|jwt|telegramchatid|api\s*key|connection\s*string|clientsecret|accesskey|refreshtoken)\s*[:=]\s*([^\s,;}}]+)";
            result = Regex.Replace(result, keyValuePattern, "$1 = ********", RegexOptions.IgnoreCase);

            // Regex 2: Matches header-style authentication (e.g. Bearer xyz, Authorization abc)
            var authPattern = @"(?i)(bearer|authorization)\s+([^\s,;}}]+)";
            result = Regex.Replace(result, authPattern, "$1 ********", RegexOptions.IgnoreCase);

            if (result.Length > 500)
            {
                result = result.Substring(0, 497) + "...";
            }

            return result;
        }
    }
}
