using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog auditLog);
        Task<AuditLogListResponse> GetPagedAsync(AuditLogFilterRequest filter);
        Task<AuditLogDetailDto?> GetByIdAsync(int id);
        Task<int> CountAsync();
    }
}
