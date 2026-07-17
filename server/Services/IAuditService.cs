using System.Threading.Tasks;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string action,
            AuditResourceType resourceType, // Use enum
            string? entityId = null,
            string? description = null,
            bool success = true,
            int statusCode = 200,
            AuditSeverity severity = AuditSeverity.Information,
            string? userNameOverride = null,
            string? roleOverride = null); // Keep override parameters
    }
}
