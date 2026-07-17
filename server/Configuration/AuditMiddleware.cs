using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Configuration
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditService auditService)
        {
            var method = context.Request.Method;

            // Skip OPTIONS and HEAD requests
            if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
                method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;

            // Skip routes: /swagger, /health, /hubs, /favicon.ico, and static files (files with extensions)
            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                Path.HasExtension(path))
            {
                await _next(context);
                return;
            }

            await _next(context);

            // Check for 401 or 403 response codes after pipeline execution
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized ||
                context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                string action = context.Response.StatusCode == StatusCodes.Status401Unauthorized 
                    ? "Unauthorized Access" 
                    : "Forbidden Access";
                
                string description = $"Attempted to {method} {path}";
                var severity = AuditSeverity.Warning;

                await auditService.LogAsync(
                    action: action,
                    resourceType: AuditResourceType.System,
                    description: description,
                    success: false,
                    statusCode: context.Response.StatusCode,
                    severity: severity
                );
            }
        }
    }
}
